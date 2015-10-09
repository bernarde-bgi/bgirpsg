using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

namespace Cashplay
{
	public static class IosBuildPostProcessorExtensions
	{
		public static void Set(this HashSet<string> container, string newValue)
		{
			if (!container.Contains(newValue))
				container.Add(newValue);
		}
	}
	
	public class IosBuildPostProcessor
	{
		private static string GetAbsolutePath(string target)
		{
			return Path.Combine(Application.dataPath, target);
		}
		
		private static string GetRelativePath(string root, string target)
		{
			var rootUri = new Uri(root + "/.");
			var targetUri = new Uri(Path.Combine(Application.dataPath, target));
			return rootUri.MakeRelativeUri(targetUri).ToString();
		}
		
		private static bool InjectCode(string [] fileNames, string anchor, string injection, bool before = false)
		{
			bool injected = false;
			foreach(var fileName in fileNames)
			{
				if (File.Exists(fileName))
					injected |= InjectCode(fileName, anchor, injection, before);
			}
			return injected;
		}
		
		private static bool InjectCode(string fileName, string anchor, string injection, bool before)
		{
			if (!File.Exists(fileName))
				return false;
	
			string [] afterParts = anchor.Split(' ');
	
			var fileContent = File.ReadAllText(fileName);
	
			var beforePos = 0;
			var afterPos = 0;
			if (afterParts.Length > 0) while(true)
			{
				var start = true;
				var match = true;
				foreach (var part in afterParts)
				{
					var nextPos = fileContent.IndexOf(part, afterPos);
	
					if (nextPos < 0)
						return false;
	
					if (!start)
					{
						var whitespace = fileContent.Substring(afterPos, nextPos - afterPos);
	
						if (whitespace.Trim().Length > 0)
						{
							match = false;
							break;
						}
					}
					else
					{
						beforePos = nextPos;	
					}
					start = false;
					afterPos = nextPos + part.Length;
				}
				if (match)
					break;
			}
	
			bool injected = false;
			if (before)
			{
				if (beforePos >= 0 && !fileContent.Substring(0, beforePos).EndsWith(injection))
				{
					fileContent = fileContent.Substring(0, beforePos) + injection + fileContent.Substring(beforePos);
					injected = true;
				}
			}
			else
			{
				if (afterPos >= 0 && !fileContent.Substring(afterPos).StartsWith(injection))
				{
					fileContent = fileContent.Substring(0, afterPos) + injection + fileContent.Substring(afterPos);
					injected = true;
				}
			}
			File.WriteAllText(fileName, fileContent);
			return injected;
		}
	
		[PostProcessBuild(2037)]
		public static void OnPostProcessBuild(BuildTarget target, string path)
		{
	#if UNITY_IPHONE
			Debug.Log("Post-processing XCode project " + path + " from " + Application.dataPath);
			XCProject currentProject = new XCProject(path);
			PBXGroup frameworkGroup = currentProject.GetGroup( "Frameworks" );
			PBXGroup resourcesGroup = currentProject.GetGroup( "Resources" );
	
			var sdkRootFrameworks = new HashSet<string>();
			
			// Base SDK
			{
				currentProject.AddFile("Assets/Plugins/iOS/CashplayClient.bundle", resourcesGroup, "SOURCE_ROOT", true, false);
				currentProject.AddFile("Assets/Plugins/iOS/CashplayClient.framework", frameworkGroup, "SOURCE_ROOT", true, false);

				sdkRootFrameworks.Set("System/Library/Frameworks/MobileCoreServices.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/QuartzCore.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/MessageUI.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/StoreKit.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/AdSupport.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/WebKit.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/libxml2.dylib");
				sdkRootFrameworks.Set("System/Library/Frameworks/libc++.dylib");
				
				// XMPP Framework
				//currentProject.AddFile("Assets/Plugins/iOS/libidn.a", frameworkGroup, "SOURCE_ROOT", true, false);
				sdkRootFrameworks.Set("System/Library/Frameworks/CoreData.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/SystemConfiguration.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/libresolv.dylib");
				sdkRootFrameworks.Set("System/Library/Frameworks/libiconv.dylib");
				sdkRootFrameworks.Set("System/Library/Frameworks/Security.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/Social.framework");
				sdkRootFrameworks.Set("System/Library/Frameworks/CFNetwork.framework");
			}
			
			foreach(var f in sdkRootFrameworks)
			{
				currentProject.AddFile(f, frameworkGroup, "SDKROOT", true, false);
			}
			
			var frameworkSearchPath = GetRelativePath(path, "Plugins/iOS");
			currentProject.AddFrameworkSearchPaths(frameworkSearchPath);
			currentProject.Save();
			
			Debug.Log("Post-processing AppController");

			var appController = new []
			{ 
				Path.Combine(path, "Classes/AppController.mm"), 
				Path.Combine(path, "Classes/UnityAppController.mm"),
			};
			
			//InjectCode(appController, "", "#import \"../Libraries/CashplayUnityAdapter.h\"\n");
			InjectCode(appController, "", "#import \"CashplayUnityAdapter.h\"\n");
			InjectCode(appController, "- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions {",
				           "\n\t[Cashplay application:application didFinishLaunchingWithOptions:launchOptions];\n\n");
			InjectCode(appController, "- (void)application:(UIApplication*)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData*)deviceToken {",
				           "\n\t[Cashplay application:application didRegisterForRemoteNotificationsWithDeviceToken:deviceToken];\n\n");
			InjectCode(appController, "- (void)application:(UIApplication*)application didReceiveRemoteNotification:(NSDictionary*)userInfo {",
				           "\n\t[Cashplay application:application didReceiveRemoteNotification:userInfo];\n\n");
			InjectCode(appController, "- (void)application:(UIApplication*)application didReceiveLocalNotification:(UILocalNotification*)notification {",
				           "\n\t[Cashplay application:application didReceiveLocalNotification:notification];\n\n");
				
			if (CashplaySetup.IsDeepLinkingEnabled())
			{	
				if (!InjectCode(appController, "- (BOOL)application:(UIApplication*)application openURL:(NSURL*)url sourceApplication:(NSString*)sourceApplication annotation:(id)annotation {",
				                "\n\tif ([Cashplay application:application openURL:url sourceApplication:sourceApplication annotation:annotation])\n\t\treturn YES;\n\n"))
				{
					InjectCode(appController, "- (void)application:(UIApplication*)application didReceiveLocalNotification:(UILocalNotification*)notification {",
				    	       "\n\n-(BOOL)application:(UIApplication*)application openURL:(NSURL *)url sourceApplication:(NSString *)sourceApplication annotation:(id)annotation \n{\n\tif ([Cashplay application:application openURL:url sourceApplication:sourceApplication annotation:annotation])\n\t\treturn YES;\n\n\treturn NO;\n}\n\n", true);
				}
				
				var infoPlistPath = Path.Combine(path, "Info.plist");
				Debug.Log("Post-processing Info plist " + infoPlistPath + " from " + Application.dataPath);

				var infoPlist = new ProjectInfoPlist(infoPlistPath);
				var gameId = CashplaySetup.GetGameId();
				var separator = gameId.LastIndexOfAny(new [] { ':', '@' });
				if (separator >= 0)
					gameId = gameId.Substring(separator + 1);
				infoPlist.AddUrlType(PlayerSettings.bundleIdentifier, "Viewer", new [] { "cashplay-" + gameId });
				infoPlist.Save();
			}
	#endif
		}
	}
}