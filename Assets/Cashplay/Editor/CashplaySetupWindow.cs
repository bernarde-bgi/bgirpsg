#if UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5
#define UNITY_4_3_PLUS
#endif

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cashplay;

public class CashplaySetup
{
	#region Utils
	private static string FullPathTo(string asset)
	{
		return Path.GetFullPath(Path.Combine(Application.dataPath, asset));
	}
	#endregion
	
	#region Fields
	public abstract class Field
	{
		public string label = "";
		public string metaDataName = "";
		public string constantName = "";
		public bool defaulted = false;
		public bool quotedInConfig = false;
		
		public abstract void OnGUI();
		public abstract void SetRequirement(AndroidManifest.Requirements requirements);
		public abstract void SetRequirement(IosConfig.Requirements requirements);
		public abstract void Refresh(AndroidManifest manifest, IosConfig config);
	}
	
	public class StringField : Field
	{
		public string defaultValue = "";
		public string userInput = "";
		public string manifestValue = "";
		public string configValue = "";
		
		public string [] possibleValues = null;
		public int optionIndex = -1;
		
		public override void OnGUI()
		{
			if (possibleValues == null)
			{
				userInput = EditorGUILayout.TextField(label, userInput);
			}
			else
			{
				for (var i = 0; i < possibleValues.Length; ++i)
				{
					if (possibleValues[i] == userInput)
					{
						optionIndex = i;
						break;
					}
				}
				optionIndex = EditorGUILayout.Popup(label, optionIndex, possibleValues);
				if (optionIndex >= 0)
					userInput = possibleValues[optionIndex];
			}
		}
		
		public override void SetRequirement(AndroidManifest.Requirements requirements)
		{
			if (string.IsNullOrEmpty(metaDataName))
				return;
			
			if (requirements.metaDatas.ContainsKey(metaDataName))
				requirements.metaDatas[metaDataName] = userInput;
			else
				requirements.metaDatas.Add (metaDataName, userInput);
		}
		
		public override void SetRequirement(IosConfig.Requirements requirements)
		{
			if (string.IsNullOrEmpty(constantName))
				return;
			
			if (requirements.constants.ContainsKey(constantName))
				requirements.constants[constantName] = new IosConfig.Constant(userInput, quotedInConfig);
			else
				requirements.constants.Add (constantName, new IosConfig.Constant(userInput, quotedInConfig));
		}
		
		public override void Refresh(AndroidManifest manifest, IosConfig config)
		{
			var definedInManifest = metaDataName.Length > 0;
			var hasCustomManifestValue = false;
			var customManifestValue = "";
			
			var definedInConfig = constantName.Length > 0;
			var hasCustomConfigValue = false;
			var customConfigValue = "";
			
			if (definedInManifest)
			{
				if (manifest.HasMetaData(metaDataName))
				{
					hasCustomManifestValue = true;
					customManifestValue = manifest.GetMetaData(metaDataName);
				}
			}
			if (definedInConfig)
			{
				if (config.HasConstant(constantName))
				{
					hasCustomConfigValue = true;
					customConfigValue = config.GetConstant(constantName);
				}
			}
			
			if (hasCustomConfigValue || hasCustomManifestValue)
			{
				if (hasCustomManifestValue)
				{
					if (manifestValue == userInput)
						userInput = customManifestValue;
					manifestValue = customManifestValue;
				}
				if (hasCustomConfigValue)
				{
					if (configValue == userInput)
						userInput = customConfigValue;
					configValue = customConfigValue;
				}
			}
			else if (!defaulted)
			{
				defaulted = true;
				userInput = defaultValue;
				return;
			}
		}
	}
	
	public class BooleanField : Field
	{
		public string trueManifestValue = "true";
		public string falseManifestValue = "false";
		public string trueConfigValue = "1";
		public string falseConfigValue = "0";
		
		public bool defaultValue;
		public bool userInput;
		public bool manifestValue;
		public bool configValue;
		
		public BooleanField() { quotedInConfig = false; }
		
		public override void OnGUI()
		{
			userInput = EditorGUILayout.Toggle(label, userInput);
		}
		
		public override void SetRequirement(AndroidManifest.Requirements requirements)
		{
			if (string.IsNullOrEmpty(metaDataName))
				return;
			
			var value = userInput ? trueManifestValue : falseManifestValue;
			if (requirements.metaDatas.ContainsKey(metaDataName))
				requirements.metaDatas[metaDataName] = value;
			else
				requirements.metaDatas.Add(metaDataName, value);
		}
		
		public override void SetRequirement(IosConfig.Requirements requirements)
		{
			if (string.IsNullOrEmpty(constantName))
				return;
			
			var value = userInput ? trueConfigValue : falseConfigValue;
			if (requirements.constants.ContainsKey(constantName))
				requirements.constants[constantName] = new IosConfig.Constant(value, quotedInConfig);
			else
				requirements.constants.Add(constantName, new IosConfig.Constant(value, quotedInConfig));
		}
		
		public override void Refresh(AndroidManifest manifest, IosConfig config)
		{
			var definedInManifest = metaDataName.Length > 0;
			var hasCustomManifestValue = false;
			var customManifestValue = false;
			
			var definedInConfig = constantName.Length > 0;
			var hasCustomConfigValue = false;
			var customConfigValue = false;
			
			if (definedInManifest)
			{
				if (manifest.HasMetaData(metaDataName))
				{
					hasCustomManifestValue = true;
					customManifestValue = manifest.GetMetaData(metaDataName) == trueManifestValue;
				}
			}
			if (definedInConfig)
			{
				if (config.HasConstant(constantName))
				{
					hasCustomConfigValue = true;
					customConfigValue = config.GetConstant(constantName) == trueConfigValue;
				}
			}
			
			if (hasCustomConfigValue || hasCustomManifestValue)
			{
				if (hasCustomManifestValue)
				{
					if (manifestValue == userInput)
						userInput = customManifestValue;

					manifestValue = customManifestValue;
				}
				if (hasCustomConfigValue)
				{
					if (configValue == userInput)
						userInput = customConfigValue;

					configValue = customConfigValue;
				}
			}
			else if (!defaulted)
			{
				defaulted = true;
				userInput = defaultValue;
				return;
			}
		}
	}
	#endregion
	
	private const string CashExtensionSuffix = ".cashextension";
	
	private const string IosConfigPath = "Plugins/iOS/CashplayConfig.h";
	private const string WPClient = "Cashplay/Plugins/CashplayClient.cs";
	private const string AndroidManifestPath = "Plugins/Android/AndroidManifest.xml";
	#if UNITY_4_3_PLUS
	private const string AndroidManifestDefaultPath = "Cashplay/Editor/DefaultAndroidManifest_4_3_plus.xml";
	#else
	private const string AndroidManifestDefaultPath = "Cashplay/Editor/DefaultAndroidManifest.xml";
	#endif

	public static int toolbarInt = 0;
	public static string[] toolbarStrings = new string[] {"Test Mode", "Production"};
	
	public List<string> errors = new List<string>();
	public List<string> warnings = new List<string>();
	
	public void Check(bool fix)
	{
		ApplyHighLevelFields();
		List<Field> allFields = UpdateRequirements();
		
		if (fix)
		{
			foreach (var f in allFields)
			{
				f.SetRequirement(manifestRequirements);
				f.SetRequirement(configRequirements);
			}
		}
		
		var manifestCheckResult = AndroidManifest.Check(manifestRequirements, fix);
		var configCheckResult = IosConfig.Check(configRequirements, fix);
		
		errors.Clear();
		errors.AddRange(manifestCheckResult.errors);
		errors.AddRange(configCheckResult.errors);
		
		warnings.Clear();
		warnings.AddRange(manifestCheckResult.warnings);
		warnings.AddRange(configCheckResult.warnings);
		
		alternativeResultsDeliveryEnabled = 
			manifestCheckResult.manifest == null ||
				manifestCheckResult.manifest.UnityActivitiesAreCustomizable;
		
		foreach (var f in allFields)
		{
			f.Refresh(manifestCheckResult.manifest, configCheckResult.config);
		}

		RecoverHighLevelFields();
	}
	
	private AndroidManifest.Requirements manifestRequirements = new AndroidManifest.Requirements();
	private IosConfig.Requirements configRequirements = new IosConfig.Requirements();
	private const string GameIdMetaData = "co.cashplay.GAME_ID";
	private const string GameIdConstant = "CASHPLAY_GAME_ID";
	
	private const string SecretMetaData = "co.cashplay.SECRET";
	private const string SecretConstant = "CASHPLAY_SECRET";
	
	private const string StoreIdMetaData = "co.cashplay.STORE_ID";
	private const string StoreIdConstant = "CASHPLAY_STORE_ID";
	
	private const string AdjustIdMetaData = "co.cashplay.ADJUST_ID";
	private const string AdjustIdConstant = "CASHPLAY_ADJUST_ID";
	
	private const string TestModeMetaData = "co.cashplay.TEST_MODE";
	private const string TestModeConstant = "CASHPLAY_TEST_MODE";
	
	private const string LogEnabledMetaData = "co.cashplay.LOG_ENABLED";
	private const string LogEnabledConstant = "CASHPLAY_LOG_ENABLED";
	
	private const string NoCashMetaData = "co.cashplay.NO_CASH";
	
	private const string AlternativeResultsDeliveryMetaData = "co.cashplay.ALTERNATIVE_RESULTS_DELIVERY";
	
	private const string DeepLinkingEnabledConstant = "CASHPLAY_DEEP_LINKING_ENABLED";

	public StringField gameIdAndroidTest = new StringField 
	{
		label = "Game ID",
		defaultValue = "",
		
		metaDataName = GameIdMetaData,
		quotedInConfig = true,
	};

	public StringField gameIdAndroidProduction = new StringField 
	{
		label = "Game ID",
		defaultValue = "",
		
		metaDataName = GameIdMetaData,
		quotedInConfig = true,
	};

	public StringField gameIdIOSTest = new StringField 
	{
		label = "Game ID",
		defaultValue = "",
		
		constantName = GameIdConstant,
		quotedInConfig = true,
	};

	public StringField gameIdIOSProduction = new StringField 
	{
		label = "Game ID",
		defaultValue = "",
		
		constantName = GameIdConstant,
		quotedInConfig = true,
	};
	
	public StringField secretAndroidTest = new StringField 
	{
		label = "Secret",
		defaultValue = "",
		
		metaDataName = SecretMetaData,
		quotedInConfig = true,
	};

	public StringField secretAndroidProduction = new StringField 
	{
		label = "Secret",
		defaultValue = "",
		
		metaDataName = SecretMetaData,
		quotedInConfig = true,
	};

	public StringField secretIOSTest = new StringField 
	{
		label = "Secret",
		defaultValue = "",
		
		constantName = SecretConstant,
		quotedInConfig = true,
	};

	public StringField secretIOSProduction = new StringField 
	{
		label = "Secret",
		defaultValue = "",
		
		constantName = SecretConstant,
		quotedInConfig = true,
	};
	
	public StringField storeIdAndroidTest = new StringField 
	{
		label = "Store ID",
		defaultValue = "",
		
		metaDataName = StoreIdMetaData,
	};

	public StringField storeIdAndroidProduction = new StringField 
	{
		label = "Store ID",
		defaultValue = "",
		
		metaDataName = StoreIdMetaData,
	};

	public StringField storeIdIOSTest = new StringField 
	{
		label = "Store ID",
		defaultValue = "",
		
		constantName = StoreIdConstant,
	};

	public StringField storeIdIOSProduction = new StringField 
	{
		label = "Store ID",
		defaultValue = "",
		
		constantName = StoreIdConstant,
	};
	
	public StringField adjustId = new StringField 
	{
		label = "Adjust ID",
		defaultValue = "",
		
		metaDataName = AdjustIdMetaData,
		constantName = AdjustIdConstant,
	};
	
	public BooleanField testMode = new BooleanField 
	{ 
		label = "Test Mode", 
		defaultValue = true,
		
		metaDataName = TestModeMetaData,
		trueManifestValue = "true",
		falseManifestValue = "false",
		
		constantName = TestModeConstant,
		trueConfigValue = "1",
		falseConfigValue = "0",
		quotedInConfig = false,
	};
	
	public BooleanField logEnabled = new BooleanField 
	{ 
		label = "Log Enabled", 
		defaultValue = true,
		
		metaDataName = LogEnabledMetaData,
		trueManifestValue = "true",
		falseManifestValue = "false",
		
		constantName = LogEnabledConstant,
		trueConfigValue = "1",
		falseConfigValue = "0",
		quotedInConfig = false,
	};
	
	public BooleanField deepLinkingTest = new BooleanField 
	{ 
		label = "Deep Linking Enabled", 
		defaultValue = true,
		
		constantName = DeepLinkingEnabledConstant,
		trueConfigValue = "1",
		falseConfigValue = "0",
		quotedInConfig = false,
	};

	public BooleanField deepLinkingProduction = new BooleanField 
	{ 
		label = "Deep Linking Enabled", 
		defaultValue = true,
		
		constantName = DeepLinkingEnabledConstant,
		trueConfigValue = "1",
		falseConfigValue = "0",
		quotedInConfig = false,
	};
	
	public StringField androidPlatform = new StringField 
	{
		label = "Platform",
		defaultValue = "Generic",
		
		possibleValues = new string [] { "Generic", "Google Play Market Version", "Google Play Cash Extension" },
		optionIndex = 0,
	};
	
	public BooleanField noCash = new BooleanField 
	{ 
		label = "No Cash", 
		defaultValue = false,
		
		metaDataName = NoCashMetaData,
		trueManifestValue = "true",
		falseManifestValue = "false",
	};
	
	public BooleanField cashExtension = new BooleanField 
	{ 
		label = "Cash Extension", 
		defaultValue = false,
	};
	
	public bool alternativeResultsDeliveryEnabled = true;
	
	public BooleanField alternativeResultsDelivery = new BooleanField 
	{
		label = "Alternative Results Delivery", 
		defaultValue = false,
		
		metaDataName = AlternativeResultsDeliveryMetaData,
		trueManifestValue = "true",
		falseManifestValue = "false",
	};
	
	public BooleanField gcm = new BooleanField 
	{ 
		label = "Google Cloud Messaging", 
		defaultValue = true,
	};
	
	public BooleanField adm = new BooleanField 
	{ 
		label = "Amazon Device Messaging", 
		defaultValue = false,
	};
	
	private void ApplyHighLevelFields()
	{
		switch(androidPlatform.optionIndex)
		{
		case 0:
			noCash.userInput = false;
			cashExtension.userInput = false;
			break;
		case 1:
			noCash.userInput = true;
			cashExtension.userInput = false;
			break;
		case 2:
			noCash.userInput = false;
			cashExtension.userInput = true;
			break;
		}
	}
	
	private void RecoverHighLevelFields()
	{
		androidPlatform.optionIndex = 0;
		if (noCash.userInput)
		{
			androidPlatform.optionIndex = 1;
		}
		else if (cashExtension.userInput)
		{
			androidPlatform.optionIndex = 2;
		}
		androidPlatform.userInput = androidPlatform.possibleValues[androidPlatform.optionIndex];
	}
	
	private List<Field> UpdateRequirements()
	{
		var bundleId = PlayerSettings.bundleIdentifier;
		
		#if !UNITY_4_3_PLUS
		var bundleMainAction = PlayerSettings.bundleIdentifier + ".MAIN";
		#endif
		
		if(File.Exists(FullPathTo(AndroidManifestPath))){
			string text = System.IO.File.ReadAllText(FullPathTo(AndroidManifestPath));
			Match match = Regex.Match(text, @"co.cashplay.android.+android.intent.action.MAIN", RegexOptions.Singleline);
			if(!match.Success)
				alternativeResultsDelivery.userInput = true;
		}
		
		List<Field> allFields;
		if(toolbarInt == 0)
			allFields = new List<Field>() { gameIdAndroidTest, gameIdIOSTest, secretAndroidTest, secretIOSTest, storeIdAndroidTest, storeIdIOSTest, adjustId, testMode, logEnabled, deepLinkingTest, noCash, alternativeResultsDelivery };
		else
			allFields = new List<Field>() { gameIdAndroidProduction, gameIdIOSProduction, secretAndroidProduction, secretIOSProduction, storeIdAndroidProduction, storeIdIOSProduction, adjustId, testMode, logEnabled, deepLinkingProduction, noCash, alternativeResultsDelivery };

		
		manifestRequirements = new AndroidManifest.Requirements();
		
		manifestRequirements.generator = "CashPlay";
		manifestRequirements.defaultManifestFilename = FullPathTo(AndroidManifestDefaultPath);
		manifestRequirements.manifestFilename = FullPathTo(AndroidManifestPath);

		manifestRequirements.metaDataConstraints.Add(toolbarInt == 0 ? gameIdAndroidTest.metaDataName : gameIdAndroidProduction.metaDataName, new AndroidManifest.MetaDataRequirements
		{
			notEmpty = true,
		});

		manifestRequirements.metaDataConstraints.Add(testMode.metaDataName, new AndroidManifest.MetaDataRequirements
		{
			possibleValues = new HashSet<string>() { "false", "true" },
		});
		
		manifestRequirements.usesFeatures.Add("android.hardware.location", AndroidManifest.Requirement.NotRequired);
		manifestRequirements.usesFeatures.Add("android.hardware.location.gps", AndroidManifest.Requirement.NotRequired);
		manifestRequirements.usesFeatures.Add("android.hardware.location.network", AndroidManifest.Requirement.NotRequired);
		manifestRequirements.usesFeatures.Add("android.hardware.camera", AndroidManifest.Requirement.NotRequired);
		manifestRequirements.usesFeatures.Add("android.hardware.camera.front", AndroidManifest.Requirement.NotRequired);
		
		manifestRequirements.usesPermissions.Add("android.permission.CAMERA", AndroidManifest.Requirement.NotRequired);
		manifestRequirements.usesPermissions.Add("android.permission.ACCESS_FINE_LOCATION", AndroidManifest.Requirement.NotRequired);
		manifestRequirements.usesPermissions.Add("android.permission.ACCESS_COARSE_LOCATION", AndroidManifest.Requirement.Any);
		manifestRequirements.usesPermissions.Add("android.permission.INTERNET", AndroidManifest.Requirement.Any);
		manifestRequirements.usesPermissions.Add("android.permission.ACCESS_WIFI_STATE", AndroidManifest.Requirement.Any);
		manifestRequirements.usesPermissions.Add("android.permission.ACCESS_NETWORK_STATE", AndroidManifest.Requirement.Any);
		manifestRequirements.usesPermissions.Add("android.permission.READ_PHONE_STATE", AndroidManifest.Requirement.Any);
		
		if (logEnabled.userInput)
		{
			manifestRequirements.usesPermissions.Add("android.permission.WRITE_EXTERNAL_STORAGE", AndroidManifest.Requirement.Any);
			manifestRequirements.usesPermissions.Add("android.permission.READ_LOGS", AndroidManifest.Requirement.Any);
		}
		
		manifestRequirements.contexts.Add("co.cashplay.android.client.WebUi", new AndroidManifest.ContextRequirements
		{
			type = "activity",
			label = "Cashplay",
			configChanges = new List<string> { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
		});
		
		manifestRequirements.contexts.Add("co.cashplay.android.client.NotificationReceiver", new AndroidManifest.ContextRequirements
		{
			type = "receiver",
		});
		
		if(!adjustId.userInput.Equals("")){
			manifestRequirements.contexts.Add("com.adjust.sdk.AdjustReferrerReceiver", new AndroidManifest.ContextRequirements
			{
				exported = "true",
				mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
				{
					actions = new List<string>() { "com.android.vending.INSTALL_REFERRER" }
				},
				type = "receiver",
			});
		}
		
		if (cashExtension.userInput)
		{
			manifestRequirements.bundleIdEndsWith = CashExtensionSuffix;
		}
		
		if (gcm.userInput)
		{
			manifestRequirements.usesPermissions.Add("com.google.android.c2dm.permission.RECEIVE", AndroidManifest.Requirement.Any);
			manifestRequirements.usesPermissions.Add(bundleId + ".permission.C2D_MESSAGE", AndroidManifest.Requirement.Any);
			manifestRequirements.permissions.Add(bundleId + ".permission.C2D_MESSAGE", AndroidManifest.ProtectionLevel.Signature);
			
			manifestRequirements.contexts.Add("co.cashplay.android.client.GcmRegistrationService", new AndroidManifest.ContextRequirements
			{
				type = "service",
			});
			manifestRequirements.contexts.Add("co.cashplay.android.client.GcmBroadcastReceiver", new AndroidManifest.ContextRequirements
			{
				type = "receiver",
				permission = "com.google.android.c2dm.permission.SEND",
				mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
				{
					actions = new List<string> { "com.google.android.c2dm.intent.REGISTRATION", "com.google.android.c2dm.intent.RECEIVE" },
					categories = new List<string> { bundleId }
				},
			});
		}
		
		if (adm.userInput)
		{
			manifestRequirements.namespaces.Add("xmlns:amazon", "http://schemas.amazon.com/apk/res/android");
			
			manifestRequirements.usesPermissions.Add("android.permission.WAKE_LOCK", AndroidManifest.Requirement.Any);
			manifestRequirements.usesPermissions.Add("com.amazon.device.messaging.permission.RECEIVE", AndroidManifest.Requirement.Any);
			manifestRequirements.usesPermissions.Add(bundleId + ".permission.RECEIVE_ADM_MESSAGE", AndroidManifest.Requirement.Any);
			manifestRequirements.permissions.Add(bundleId + ".permission.RECEIVE_ADM_MESSAGE", AndroidManifest.ProtectionLevel.Signature);
			
			manifestRequirements.contexts.Add("com.amazon.device.messaging", new AndroidManifest.ContextRequirements
			{
				type = "amazon:enable-feature",
				@namespace = "http://schemas.amazon.com/apk/res/android",
				required = "false",
			});
			manifestRequirements.contexts.Add("co.cashplay.android.client.AdmMessageHandler", new AndroidManifest.ContextRequirements
			{
				type = "service",
				exported = "false",
			});
			manifestRequirements.contexts.Add("co.cashplay.android.client.AdmMessageHandler$Receiver", new AndroidManifest.ContextRequirements
			{
				type = "receiver",
				permission = "com.amazon.device.messaging.permission.SEND",
				mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
				{
					actions = new List<string> { "com.amazon.device.messaging.intent.REGISTRATION", "com.amazon.device.messaging.intent.RECEIVE" },
					categories = new List<string> { bundleId },
				},
			});
		}
		
		manifestRequirements.unityActivitiesRequirements = new AndroidManifest.UnityActivitiesRequirements
		{
			#if UNITY_4_3_PLUS
			recognizedPlayerNativeActivity = new Dictionary<string, AndroidManifest.ContextRequirements>()
			{
				{
					AndroidCpPlayerNativeActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
						mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
						{
							actions = new List<string>() { "android.intent.action.MAIN" },
							excludePrevActions = true,
							categories = new List<string>() { "android.intent.category.LAUNCHER", "android.intent.category.LEANBACK_LAUNCHER", },
							excludePrevCategories = true,
						},
						extraFilters = new List<AndroidManifest.IntentFilterRequirements>()
						{
							new AndroidManifest.IntentFilterRequirements()
							{
								actions = new List<string>() { "android.intent.action.VIEW" },
								excludePrevActions = true,
								categories = new List<string>() { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
								excludePrevCategories = true,
								data = new List<Dictionary<string, string>>()
								{
									new Dictionary<string, string>() 
									{
										{ "scheme", ("cashplay-" + (toolbarInt == 0 ? gameIdAndroidTest.userInput : gameIdAndroidProduction.userInput)) },
										{ "host", 	"home" },
									},
								},
								excludePrevData = true,
								exclude = (toolbarInt == 0 ? !deepLinkingTest.userInput : !deepLinkingProduction.userInput),
							},
						},
					}
				},
				{
					AndroidUnityPlayerNativeActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
						mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
						{
							actions = new List<string>() { "android.intent.action.MAIN" },
							excludePrevActions = true,
							categories = new List<string>() { "android.intent.category.LAUNCHER", "android.intent.category.LEANBACK_LAUNCHER", },
							excludePrevCategories = true,
						},
						extraFilters = new List<AndroidManifest.IntentFilterRequirements>()
						{
							new AndroidManifest.IntentFilterRequirements()
							{
								actions = new List<string>() { "android.intent.action.VIEW" },
								excludePrevActions = true,
								categories = new List<string>() { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
								excludePrevCategories = true,
								data = new List<Dictionary<string, string>>()
								{
									new Dictionary<string, string>() 
									{
										{ "scheme", ("cashplay-" + (toolbarInt == 0 ? gameIdAndroidTest.userInput : gameIdAndroidProduction.userInput)) },
										{ "host", 	"home" },
									},
								},
								excludePrevData = true,
								exclude = (toolbarInt == 0 ? !deepLinkingTest.userInput : !deepLinkingProduction.userInput),
							},
						},
					}
				},
			},
			#else
			
			recognizedPlayerActivity = new Dictionary<string, AndroidManifest.ContextRequirements>()
			{
				{
					AndroidCpPlayerActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
					}
				},
				{
					AndroidUnityPlayerActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
					}
				},
			},
			recognizedPlayerProxyActivity = new Dictionary<string, AndroidManifest.ContextRequirements>()
			{
				{
					AndroidCpPlayerProxyActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
						mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
						{
							actions = new List<string>() { cashExtension.userInput ? bundleMainAction : "android.intent.action.MAIN" },
							excludePrevActions = true,
							categories = new List<string>() { cashExtension.userInput ? "android.intent.category.DEFAULT" : "android.intent.category.LAUNCHER" },
							excludePrevCategories = true,
						},
						extraFilters = new List<AndroidManifest.IntentFilterRequirements>()
						{
							new AndroidManifest.IntentFilterRequirements()
							{
								actions = new List<string>() { "android.intent.action.VIEW" },
								excludePrevActions = true,
								categories = new List<string>() { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
								excludePrevCategories = true,
								data = new List<Dictionary<string, string>>()
								{
									new Dictionary<string, string>() 
									{
										{ "scheme", ("cashplay-" + (toolbarInt == 0 ? gameIdAndroidTest.userInput : gameIdAndroidProduction.userInput)) },
										{ "host", 	"home" },
									},
								},
								excludePrevData = true,
								exclude = (toolbarInt == 0 ? !deepLinkingTest.userInput : !deepLinkingProduction.userInput),
							},
						},
					}
				},
				{
					AndroidUnityPlayerProxyActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
						mainIntentFilter = new AndroidManifest.IntentFilterRequirements()
						{
							actions = new List<string>() { cashExtension.userInput ? bundleMainAction : "android.intent.action.MAIN" },
							excludePrevActions = true,
							categories = new List<string>() { cashExtension.userInput ? "android.intent.category.DEFAULT" : "android.intent.category.LAUNCHER" },
							excludePrevCategories = true,
						},
						extraFilters = new List<AndroidManifest.IntentFilterRequirements>()
						{
							new AndroidManifest.IntentFilterRequirements()
							{
								actions = new List<string>() { "android.intent.action.VIEW" },
								excludePrevActions = true,
								categories = new List<string>() { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
								excludePrevCategories = true,
								data = new List<Dictionary<string, string>>()
								{
									new Dictionary<string, string>() 
									{
										{ "scheme", ("cashplay-" + (toolbarInt == 0 ? gameIdAndroidTest.userInput : gameIdAndroidProduction.userInput)) },
										{ "host", 	"home" },
									},
								},
								excludePrevData = true,
								exclude = (toolbarInt == 0 ? !deepLinkingTest.userInput : !deepLinkingProduction.userInput),
							},
						},
					}
				},
			},
			recognizedPlayerNativeActivity = new Dictionary<string, AndroidManifest.ContextRequirements>()
			{
				{
					AndroidCpPlayerNativeActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
					}
				},
				{
					AndroidUnityPlayerNativeActivityName, new AndroidManifest.ContextRequirements()
					{
						type = "activity",
						label = "@string/app_name",
						configChanges = new List<string>() { "fontScale", "keyboard", "keyboardHidden", "locale", "mnc", "mcc", "navigation", "orientation", "screenLayout", "screenSize", "smallestScreenSize", "uiMode", "touchscreen" },
					}
				},
			},
			
			#endif
		};
		
		if (alternativeResultsDelivery.userInput)
		{
			manifestRequirements.unityActivitiesRequirements.playerProxyActivity = AndroidUnityPlayerProxyActivityName;
			manifestRequirements.unityActivitiesRequirements.playerNativeActivity = AndroidUnityPlayerNativeActivityName;
			manifestRequirements.unityActivitiesRequirements.playerActivity = AndroidUnityPlayerActivityName;
		}
		else
		{
			manifestRequirements.unityActivitiesRequirements.playerProxyActivity = AndroidCpPlayerProxyActivityName;
			manifestRequirements.unityActivitiesRequirements.playerNativeActivity = AndroidCpPlayerNativeActivityName;
			manifestRequirements.unityActivitiesRequirements.playerActivity = AndroidCpPlayerActivityName;
		}

		configRequirements = new IosConfig.Requirements();
		configRequirements.configFilename = FullPathTo("Plugins/iOS/CashplayConfig.h");
		configRequirements.constantConstraints.Add(toolbarInt == 0 ? gameIdIOSTest.constantName : gameIdIOSProduction.constantName, new IosConfig.ConstantRequirements
		{
			notEmpty = true,
			isQuoted = true,
		});
		
		configRequirements.constantConstraints.Add(testMode.constantName, new IosConfig.ConstantRequirements
		{
			possibleValues = new HashSet<string>() { "0", "1" },
			isInteger = true,
			isQuoted = false,
		});

		// Setting the config values for WP
			string[] arrLine = System.IO.File.ReadAllLines(FullPathTo (WPClient));

			if(toolbarInt == 0) {
				for(int i = 0; i < arrLine.Length; i++) {
					if(arrLine[i].Contains("private static string gameId_WP"))
						arrLine[i] = "\t\tprivate static string gameId_WP = \"" + gameIdIOSTest.userInput.ToString () + "\";";
					
					if(arrLine[i].Contains("private static string gameSecret_WP"))
						arrLine[i] = "\t\tprivate static string gameSecret_WP = \"" + secretIOSTest.userInput.ToString () + "\";";
					
					if(arrLine[i].Contains("private static bool testMode_WP"))
						arrLine[i] = "\t\tprivate static bool testMode_WP = true;";
					
					if(arrLine[i].Contains("private static bool logEnabled_WP"))
						arrLine[i] = "\t\tprivate static bool logEnabled_WP = true;";
				}
			} else {
				for(int i = 0; i < arrLine.Length; i++) {
					if(arrLine[i].Contains("private static string gameId_WP"))
						arrLine[i] = "\t\tprivate static string gameId_WP = \"" + gameIdIOSTest.userInput.ToString () + "\";";
					
					if(arrLine[i].Contains("private static string gameSecret_WP"))
						arrLine[i] = "\t\tprivate static string gameSecret_WP = \"" + secretIOSTest.userInput.ToString () + "\";";
					
					if(arrLine[i].Contains("private static bool testMode_WP"))
						arrLine[i] = "\t\tprivate static bool testMode_WP = false;";
					
					if(arrLine[i].Contains("private static bool logEnabled_WP"))
						arrLine[i] = "\t\tprivate static bool logEnabled_WP = false;";
				}
			}
			System.IO.File.WriteAllLines(FullPathTo(WPClient), arrLine);
				
		return allFields;
	}
	
	private const string AndroidCpPlayerProxyActivityName 		= "co.cashplay.android.unityadapter.CpUnityPlayerProxyActivity";
	private const string AndroidUnityPlayerProxyActivityName 	= "com.unity3d.player.UnityPlayerProxyActivity";
	
	private const string AndroidCpPlayerNativeActivityName 		= "co.cashplay.android.unityadapter.CpUnityPlayerNativeActivity";
	private const string AndroidUnityPlayerNativeActivityName 	= "com.unity3d.player.UnityPlayerNativeActivity";
	
	private const string AndroidCpPlayerActivityName 			= "co.cashplay.android.unityadapter.CpUnityPlayerActivity";
	private const string AndroidUnityPlayerActivityName 		= "com.unity3d.player.UnityPlayerActivity";
	
	private const string AndroidMainAction 						= "android.intent.action.MAIN";
	
	private const string AndroidDefaultCategory 				= "android.intent.category.DEFAULT";
	private const string AndroidLauncherCategory 				= "android.intent.category.LAUNCHER";
	
	public static string GetGameId()
	{
		var requirements = new IosConfig.Requirements();
		requirements.configFilename = FullPathTo(IosConfigPath);
		var result = IosConfig.Check(requirements, false);

		if (result.config == null || !result.config.Exists)
			return "";

		return result.config.GetConstant(GameIdConstant);
	}
	
	public static bool IsDeepLinkingEnabled()
	{
		var requirements = new IosConfig.Requirements();
		requirements.configFilename = FullPathTo(IosConfigPath);

		var result = IosConfig.Check(requirements, false);
		if (result.config == null || !result.config.Exists)
			return false;

		return result.config.HasConstant(DeepLinkingEnabledConstant) &&
			   result.config.GetConstant(DeepLinkingEnabledConstant) == "1";
	}
}

class CashplaySetupWindow : EditorWindow
{
	static void BuildPlayer(string scene, BuildTarget target, string path, bool allowExternalModifications = false, bool development = false)
	{
		var flags = BuildOptions.None;
		if (allowExternalModifications)
			flags |= BuildOptions.AcceptExternalModificationsToPlayer;
		if (development)
			flags |= BuildOptions.Development;
		BuildPipeline.BuildPlayer(new [] { scene }, path, target, flags);
	}
	
	static string GetStringArg(string name, string defaultValue = "")
	{
		var args = System.Environment.GetCommandLineArgs();
		for (var i = 0; i < args.Length - 1; ++i)
		{
			if (args[i] == name)
				return args[i+1];
		}
		return defaultValue;
	}
	
	static bool GetBoolArg(string name, bool defaultValue = false)
	{
		bool result;
		if (bool.TryParse(GetStringArg(name, defaultValue.ToString()), out result))
		{
			return result;
		}
		return defaultValue;
	}
	
	private CashplaySetup setup = new CashplaySetup();
	
	private void ShowMessages(List<string> messages, MessageType type)
	{
		foreach (var m in messages)
		{
			EditorGUILayout.HelpBox(m, type, true);
		}
	}
	
	[MenuItem("Cashplay/Setup")]
	static void ShowWindow()
	{
		((CashplaySetupWindow)EditorWindow.GetWindow(typeof(CashplaySetupWindow))).Refresh(true);
		((CashplaySetupWindow)EditorWindow.GetWindow(typeof(CashplaySetupWindow))).minSize = new Vector2(300f, 400f);
	}
	
	private void Refresh(bool force = false)
	{
		if (EditorApplication.timeSinceStartup < lastRefreshTime + refreshInterval && !force)
			return;

		lastRefreshTime = (float)EditorApplication.timeSinceStartup;
		
		//setup.Check(false);
	}
	
	private float lastRefreshTime 					= 0.0f;
	private const float refreshInterval 			= 1.0f;

	void OnEnable() {
		setup.adjustId.userInput 					= EditorPrefs.GetString ("adjustId");

		// TEST
		setup.gameIdAndroidTest.userInput 			= EditorPrefs.GetString ("gameIdAndroidTest");
		setup.secretAndroidTest.userInput 			= EditorPrefs.GetString ("secretAndroidTest");
		setup.storeIdAndroidTest.userInput 			= EditorPrefs.GetString ("storeIdAndroidTest");
		setup.gcm.userInput 						= true;

		setup.gameIdIOSTest.userInput 				= EditorPrefs.GetString ("gameIdIOSTest");
		setup.secretIOSTest.userInput 				= EditorPrefs.GetString ("secretIOSTest");
		setup.storeIdIOSTest.userInput 				= EditorPrefs.GetString ("storeIdIOSTest");
		setup.deepLinkingTest.userInput 			= EditorPrefs.GetBool 	("deepLinkingTest");

		// PRODUCTION
		setup.gameIdAndroidProduction.userInput 	= EditorPrefs.GetString ("gameIdAndroidProduction");
		setup.secretAndroidProduction.userInput 	= EditorPrefs.GetString ("secretAndroidProduction");
		setup.storeIdAndroidProduction.userInput 	= EditorPrefs.GetString ("storeIdAndroidProduction");
		setup.gcm.userInput = true;
		
		setup.gameIdIOSProduction.userInput 		= EditorPrefs.GetString ("gameIdIOSProduction");
		setup.secretIOSProduction.userInput 		= EditorPrefs.GetString ("secretIOSProduction");
		setup.storeIdIOSProduction.userInput 		= EditorPrefs.GetString ("storeIdIOSProduction");
		setup.deepLinkingProduction.userInput 		= EditorPrefs.GetBool 	("deepLinkingProduction");
	}

	void OnDisable() {
		EditorPrefs.SetString 	("adjustId", 					setup.adjustId.userInput);

		// TEST
		EditorPrefs.SetString 	("gameIdAndroidTest", 			setup.gameIdAndroidTest.userInput);
		EditorPrefs.SetString 	("secretAndroidTest", 			setup.secretAndroidTest.userInput);
		EditorPrefs.SetString 	("storeIdAndroidTest", 			setup.storeIdAndroidTest.userInput);

		EditorPrefs.SetString 	("gameIdIOSTest", 				setup.gameIdIOSTest.userInput);
		EditorPrefs.SetString 	("secretIOSTest", 				setup.secretIOSTest.userInput);
		EditorPrefs.SetString 	("storeIdIOSTest", 				setup.storeIdIOSTest.userInput);
		EditorPrefs.SetBool 	("deepLinkingTest", 			setup.deepLinkingTest.userInput);

		// PRODUCTION
		EditorPrefs.SetString 	("gameIdAndroidProduction", 	setup.gameIdAndroidProduction.userInput);
		EditorPrefs.SetString 	("secretAndroidProduction", 	setup.secretAndroidProduction.userInput);
		EditorPrefs.SetString 	("storeIdAndroidProduction", 	setup.storeIdAndroidProduction.userInput);
		
		EditorPrefs.SetString 	("gameIdIOSProduction", 		setup.gameIdIOSProduction.userInput);
		EditorPrefs.SetString 	("secretIOSProduction", 		setup.secretIOSProduction.userInput);
		EditorPrefs.SetString 	("storeIdIOSProduction", 		setup.storeIdIOSProduction.userInput);
		EditorPrefs.SetBool 	("deepLinkingProduction", 		setup.deepLinkingProduction.userInput);
	}

	void OnGUI()
	{
		GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
		boxStyle.normal.textColor = Color.white;
		
		Refresh();
	
		GUILayout.Box("CASHPLAY UNITY3D PLUGIN v" + Client.Version, boxStyle, GUILayout.ExpandWidth(true));
	
		GUILayout.Space(10);

		GUILayout.Box("GENERAL OPTIONS", boxStyle, GUILayout.ExpandWidth(false));
		setup.adjustId.OnGUI();
		//setup.testMode.OnGUI();
		setup.logEnabled.OnGUI();
		
		GUILayout.Space(20);

		CashplaySetup.toolbarInt = GUILayout.Toolbar(CashplaySetup.toolbarInt, CashplaySetup.toolbarStrings);
		switch (CashplaySetup.toolbarInt) {
		case 0:
			setup.testMode.userInput = true;
			setup.logEnabled.userInput = true;

			GUILayout.Box("ANDROID OPTIONS", boxStyle, GUILayout.ExpandWidth(false));
			GUILayout.BeginHorizontal();
			setup.gameIdAndroidTest.OnGUI();
			
			if (GUILayout.Button("Get one", GUILayout.Width(60)))
				Application.OpenURL("http://developers.cashplay.co/");
			
			GUILayout.EndHorizontal();
			
			setup.secretAndroidTest.OnGUI();
			setup.storeIdAndroidTest.OnGUI();
			
			setup.gcm.OnGUI();
			setup.adm.OnGUI();
			if (setup.alternativeResultsDeliveryEnabled)
				setup.alternativeResultsDelivery.OnGUI();
			setup.androidPlatform.OnGUI();
			
			GUILayout.Space(20);
			
			GUILayout.Box("IOS / WINDOWS PHONE OPTIONS", boxStyle, GUILayout.ExpandWidth(false));
			GUILayout.BeginHorizontal();
			setup.gameIdIOSTest.OnGUI();
			
			if (GUILayout.Button("Get one", GUILayout.Width(60)))
				Application.OpenURL("http://developers.cashplay.co/");
			
			GUILayout.EndHorizontal();
			
			setup.secretIOSTest.OnGUI();
			setup.storeIdIOSTest.OnGUI();
			setup.deepLinkingTest.OnGUI();
			
			GUILayout.Space(20);
			
			if (GUILayout.Button("Apply to Test Environment"))
			{
				GUIUtility.keyboardControl = 0;
				setup.Check(true);
				EditorUtility.DisplayDialog("CASHPLAY", "Settings applied for the Testing environment.\nYou can proceed with the build.", "Ok");
			}
			
			GUILayout.Space(20);
			ShowMessages(setup.errors, MessageType.Error);
			ShowMessages(setup.warnings, MessageType.Warning);
			break;

		case 1:
			setup.testMode.userInput = false;
			setup.logEnabled.userInput = false;

			GUILayout.Box("ANDROID OPTIONS", boxStyle, GUILayout.ExpandWidth(false));
			GUILayout.BeginHorizontal();
			setup.gameIdAndroidProduction.OnGUI();
			
			if (GUILayout.Button("Get one", GUILayout.Width(60)))
				Application.OpenURL("http://developers.cashplay.co/");
			
			GUILayout.EndHorizontal();
			
			setup.secretAndroidProduction.OnGUI();
			setup.storeIdAndroidProduction.OnGUI();
			
			setup.gcm.OnGUI();
			setup.adm.OnGUI();
			if (setup.alternativeResultsDeliveryEnabled)
				setup.alternativeResultsDelivery.OnGUI();
			setup.androidPlatform.OnGUI();
			
			GUILayout.Space(20);
			
			GUILayout.Box("IOS / WINDOWS PHONE OPTIONS", boxStyle, GUILayout.ExpandWidth(false));
			GUILayout.BeginHorizontal();
			setup.gameIdIOSProduction.OnGUI();
			
			if (GUILayout.Button("Get one", GUILayout.Width(60)))
				Application.OpenURL("http://developers.cashplay.co/");
			
			GUILayout.EndHorizontal();
			
			setup.secretIOSProduction.OnGUI();
			setup.storeIdIOSProduction.OnGUI();
			setup.deepLinkingProduction.OnGUI();
			
			GUILayout.Space(20);
			
			if (GUILayout.Button("Apply to Production Environment"))
			{
				GUIUtility.keyboardControl = 0;
				setup.Check(true);
				EditorUtility.DisplayDialog("CASHPLAY", "Settings applied for the Production environment.\nYou can proceed with the build.", "Ok");
			}
			
			GUILayout.Space(20);
			ShowMessages(setup.errors, MessageType.Error);
			ShowMessages(setup.warnings, MessageType.Warning);
			break;
		}
	}
}