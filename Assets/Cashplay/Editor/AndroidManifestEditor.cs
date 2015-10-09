#if UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5
#define UNITY_4_3_PLUS
#endif

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using UnityEditor;
using UnityEngine;

namespace Cashplay
{
	public class AndroidManifest
	{
		public enum Requirement
		{
			None,
			Required,
			NotRequired,
			Any,
		}
		
		public enum ProtectionLevel
		{
			None,
			Signature,
			Any,
		}
		
		#region Enum Helpers
		private static bool Forced(AndroidManifest.Requirement r)
        {
            return r != AndroidManifest.Requirement.Any;
        }
		
		private static string Value(AndroidManifest.Requirement r)
        {
            if (r == AndroidManifest.Requirement.NotRequired)
				return "false";
			else if (r == AndroidManifest.Requirement.Required)
				return "true";
			return null;
        }
		
		private static bool Forced(AndroidManifest.ProtectionLevel pl)
        {
            return pl != AndroidManifest.ProtectionLevel.Any;
        }
		
		private static string Value(AndroidManifest.ProtectionLevel r)
        {
            if (r == AndroidManifest.ProtectionLevel.Signature)
				return "signature";
			return null;
        }
		#endregion
		
		public class IntentFilterRequirements
		{
			public List<string> actions = new List<string>();
			public List<string> categories = new List<string>();
			public List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
			public bool excludePrevActions = false;
			public bool excludePrevCategories = false;
			public bool excludePrevData = false;
			public List<string> excludedActions = new List<string>();
			public List<string> excludedCategories = new List<string>();
			public List<Dictionary<string, string>> excludedData = new List<Dictionary<string, string>>();
			public bool exclude = false;
			
			private bool HasDataNode(XmlNode dataNode, List<Dictionary<string, string>> contentList)
			{
				if (dataNode.Name != "data")
					return false;
				
				foreach (var dt in contentList)
				{
					bool equal = true;
					
					foreach (string key in dt.Keys)
					{
						var attributeNode = dataNode.Attributes.GetNamedItem(key, "http://schemas.android.com/apk/res/android");
						if (attributeNode != null && attributeNode.Value == dt[key])
							continue;
						
						equal = false;
						break;
					}
										
					if (equal)
						return true;
				}
				
				return false;
			}
			
			public bool HasDataNode(XmlNode dataNode)
			{
				return HasDataNode(dataNode, data);
			}
			
			public bool HasExcludedNode(XmlNode dataNode)
			{
				return HasDataNode(dataNode, excludedData);
			}
			
			public bool HasDataNodes(XmlNodeList nodeList)
			{
				if (nodeList.Count == 0)
					return false;
				
				foreach (XmlNode node in nodeList)
				{
					if (!HasDataNode(node))
						return false;
				}
				
				return true;
			}
		}
		
		public class ContextRequirements
		{
			public string type = "";
			public string @namespace = "";
			public string label = "";
			public string permission = "";
			public string process = "";
			public string theme = "";
			public string exported = "";
			public string required = "";
			public List<string> configChanges = new List<string>();
			public IntentFilterRequirements mainIntentFilter = new IntentFilterRequirements();
			public List<IntentFilterRequirements> extraFilters = new List<IntentFilterRequirements>();			
		}
		
		public class MetaDataRequirements
		{
			public HashSet<string> possibleValues;
			public bool notEmpty;
			public bool isInteger;
		}
		
		public class UnityActivitiesRequirements
		{
			public Dictionary<string, ContextRequirements> recognizedPlayerProxyActivity = new Dictionary<string, ContextRequirements>();
			public Dictionary<string, ContextRequirements> recognizedPlayerNativeActivity = new Dictionary<string, ContextRequirements>();
			public Dictionary<string, ContextRequirements> recognizedPlayerActivity = new Dictionary<string, ContextRequirements>();
			
			public string playerProxyActivity;
			public string playerNativeActivity;
			public string playerActivity;
		}
		
		public class Requirements
		{
			public string defaultManifestFilename;
			public string manifestFilename;
			public string bundleIdEndsWith = "";
			public string generator = "";
			public Dictionary<string, MetaDataRequirements> metaDataConstraints = new Dictionary<string, MetaDataRequirements>();
			public Dictionary<string, string> namespaces = new Dictionary<string, string>();
			public Dictionary<string, string> metaDatas = new Dictionary<string, string>();
			public Dictionary<string, ProtectionLevel> permissions = new Dictionary<string, ProtectionLevel>();
			public Dictionary<string, Requirement> usesPermissions = new Dictionary<string, Requirement>();
			public Dictionary<string, Requirement> usesFeatures = new Dictionary<string, Requirement>();
			public Dictionary<string, ContextRequirements> contexts = new Dictionary<string, ContextRequirements>();
			
			public UnityActivitiesRequirements unityActivitiesRequirements = null;
		}
		
		public class CheckResult
		{
			public AndroidManifest manifest;
			public List<string> errors = new List<string>();
			public List<string> warnings = new List<string>();
		}
		
		public static CheckResult Check(Requirements requirements, bool fix)
		{
			var result = new CheckResult();
			result.manifest = new AndroidManifest(requirements);
			result.manifest.Check(result.errors, result.warnings, fix);
			return result;
		}
		
		#region Checkup Routines		
		private AndroidManifest(Requirements r)
		{
			requirements = r;
		}
		
		private Requirements requirements;
		private XmlDocument doc;
		private bool unityActivitiesAreCustomizable = true;
		
		public bool UnityActivitiesAreCustomizable
		{
			get { return unityActivitiesAreCustomizable; }
		}
		
		private const string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
		
		private const string AndroidNameAttribute = "name";
		private const string AndroidRequiredAttribute = "required";
		private const string AndroidValueAttribute = "value";
		private const string AndroidLabelAttribute = "label";
		private const string AndroidPermissionAttribute = "permission";
		private const string AndroidConfigChangesAttribute = "configChanges";
		private const string AndroidProtectionLevelAttribute = "protectionLevel";
		private const string AndroidThemeAttribute = "theme";
		private const string AndroidProcessAttribute = "process";
		private const string AndroidExportedAttribute = "exported";
		
		private const string GeneratorAttribute = "generator";
		
		private bool HasName(XmlNodeList nodes, string name)
		{
			for (var i = 0; i < nodes.Count; ++i)
			{
				var node = nodes.Item(i);
				if (GetNodeAttribute(node, AndroidNameAttribute) == name)
					return true;
			}
			return false;
		}
		
		private bool HasNode(XmlNodeList nodes, XmlNode node)
		{
			for (var i = 0; i < nodes.Count; ++i)
			{
				var listItem = nodes.Item(i);
				
				bool containsAllAttributes = true;
				
				foreach (XmlAttribute attr in node.Attributes)
					if (attr.Name != "generator")
						containsAllAttributes = containsAllAttributes && GetNodeAttribute(listItem, attr.Name) == attr.Value;
				
				if (containsAllAttributes)
					return true;
			}
			return false;
		}
		
		private void Check(string name, MetaDataRequirements requirements, List<string> errors, List<string> warnings, bool fix)
		{
			if (!HasMetaData(name))
			{
				warnings.Add("meta data " + name + " is not specified");
				return;
			}
			
			var value = GetMetaData(name);
			if (requirements.possibleValues != null && !requirements.possibleValues.Contains(value))
			{
				var possibleValues = "";
				foreach(var p in requirements.possibleValues)
				{
					if (possibleValues.Length != 0)
						possibleValues += ", ";
					possibleValues += (p.Length == 0) ? "<empty>" : p;
				}
				warnings.Add("meta data " + name + " should have one of the following values:\n" + possibleValues);
				return;
			}
			if (requirements.notEmpty && string.IsNullOrEmpty(value))
			{
				warnings.Add("meta data " + name + " should not be empty");
				return;
			}
			int intValue;
			if (requirements.isInteger && !int.TryParse(value, out intValue))
			{
				warnings.Add("meta data " + name + " should be an integer value");
				return;
			}
		}
		
		private void Check(string type, string name, ContextRequirements requirements, List<string> errors, List<string> warnings, bool fix)
		{
			var context = FindContext(type, name);
			
			if (context == null)
			{
				if (fix)
				{
					context = CreateContext(type, name, requirements.@namespace);
				}
				else
				{
					errors.Add(type + " " + name + " is not defined");
					return;
				}
			}
			
			Check (context, requirements, errors, warnings, fix);
		}
		
		private void Check(XmlNode context, ContextRequirements requirements, List<string> errors, List<string> warnings, bool fix)
		{
			Dictionary<XmlNode, IntentFilterRequirements> intentFilters = new Dictionary<XmlNode, IntentFilterRequirements>();
			
			var name = GetNodeAttribute(context, AndroidNameAttribute);
			var type = context.Value;
			
			var configChanges = GetNodeAttribute(context, AndroidConfigChangesAttribute) ?? "";
			var existingConfigChanges = new HashSet<string>(configChanges.Split ('|'));
			
			var excludedFilterNodes = new List<XmlNode>();
			var existingFilters = context.SelectNodes("intent-filter");
			
			foreach (XmlNode filterNode in existingFilters)
			{
				if (GetNodeAttribute(filterNode, GeneratorAttribute, "") == "CashPlay" && fix)
				{
					context.RemoveChild(filterNode);
				}
			}
			
			existingFilters = context.SelectNodes("intent-filter");
			
			foreach (var filterRequirements in requirements.extraFilters)
			{
				if (filterRequirements.actions.Count == 0 &&
					filterRequirements.categories.Count == 0 &&
					filterRequirements.data.Count == 0)
						continue;
				
				bool alreadyExist = false;
				
				foreach (XmlNode filterNode in existingFilters)
				{
					var dataNodes = filterNode.SelectNodes("data");
					
					bool hasAllAttributes = true;
					foreach (var d in filterRequirements.data)
					{
						var dataNode = GenerateNode(XmlNodeType.Element, "data");
						foreach (var attr in d)
						{
							SetNodeAttribute(dataNode, attr.Key, attr.Value);
						}
						
						hasAllAttributes &= HasNode(dataNodes, dataNode);							
					}
					
					if (dataNodes != null && hasAllAttributes && !intentFilters.ContainsKey(filterNode))
					{
						intentFilters.Add(filterNode, filterRequirements);
						excludedFilterNodes.Add(filterNode);
						alreadyExist = true;
						continue;
					}
				}
				
				if (!alreadyExist && !filterRequirements.exclude)
				{
					var intentFilter = GenerateNode(XmlNodeType.Element, "intent-filter");
					context.AppendChild(intentFilter);
					intentFilters.Add(intentFilter, filterRequirements);
				}
			}
			
			XmlNode mainIntentFilter = null;
			foreach (XmlNode filterNode in existingFilters)
			{
				if (!excludedFilterNodes.Contains(filterNode))
				{
					mainIntentFilter = filterNode;
					break;
				}
			}
			
			
			if (requirements.mainIntentFilter.actions.Count != 0 ||
					requirements.mainIntentFilter.categories.Count != 0 ||
								requirements.mainIntentFilter.data.Count != 0)
			{
				if (mainIntentFilter == null)
				{
					mainIntentFilter = GenerateNode(XmlNodeType.Element, "intent-filter");
					context.AppendChild(mainIntentFilter);
				}
				
				if (!intentFilters.ContainsKey(mainIntentFilter))
					intentFilters.Add(mainIntentFilter, requirements.mainIntentFilter);
			}
			
			if (fix)
			{	
				if (!string.IsNullOrEmpty(requirements.label) &&
					string.IsNullOrEmpty(GetNodeAttribute(context, AndroidLabelAttribute)))
					SetNodeAttribute(context, AndroidLabelAttribute, requirements.label);
				
				if (!string.IsNullOrEmpty(requirements.theme) &&
					string.IsNullOrEmpty(GetNodeAttribute(context, AndroidThemeAttribute)))
					SetNodeAttribute(context, AndroidThemeAttribute, requirements.theme);
				
				if (!string.IsNullOrEmpty(requirements.permission))
					SetNodeAttribute(context, AndroidPermissionAttribute, requirements.permission);
				
				if (!string.IsNullOrEmpty(requirements.process))
					SetNodeAttribute(context, AndroidProcessAttribute, requirements.process);
				
				if (!string.IsNullOrEmpty(requirements.exported))
					SetNodeAttribute(context, AndroidExportedAttribute, requirements.exported);
				
				var configChangesUpdated = false;
				foreach (var cc in requirements.configChanges)
				{
					if (existingConfigChanges.Contains(cc))
						continue;
					configChangesUpdated = true;
					if (configChanges.Length > 0)
						configChanges += "|";
					configChanges += cc;
					existingConfigChanges.Add(cc);
				}
				
				if (configChangesUpdated)
					SetNodeAttribute(context, AndroidConfigChangesAttribute, configChanges);
				
				foreach (var pair in intentFilters)
				{
					if (pair.Value.exclude)
					{
						context.RemoveChild(pair.Key);
						continue;
					}
					
					var actions = pair.Key.SelectNodes("action");
					var categories = pair.Key.SelectNodes("category");
					var data = pair.Key.SelectNodes("data");
					
					var excludedNodes = new List<XmlNode>();
					for (var i = 0; i < actions.Count; ++i)
					{
						var action = actions.Item(i);
						if (pair.Value.excludePrevActions ||
							pair.Value.excludedActions.Contains(GetNodeAttribute(action, AndroidNameAttribute)))
							excludedNodes.Add(action);
					}
					for (var i = 0; i < categories.Count; ++i)
					{
						var category = categories.Item(i);
						if (pair.Value.excludePrevCategories ||
							pair.Value.excludedCategories.Contains(GetNodeAttribute(category, AndroidNameAttribute)))
							excludedNodes.Add(category);
					}
					for (var i = 0; i < data.Count; ++i)
					{
						var dataItem = data.Item(i);
						if (pair.Value.excludePrevData ||
							pair.Value.HasExcludedNode(dataItem))
							excludedNodes.Add(dataItem);
					}
					
					foreach (var en in excludedNodes)
					{
						pair.Key.RemoveChild(en);
					}	
					
					actions = pair.Key.SelectNodes("action");
					categories = pair.Key.SelectNodes("category");
					data = pair.Key.SelectNodes("data");
					
					foreach (var a in pair.Value.actions)
					{
						if (HasName(actions, a))
							continue;
						var action = GenerateNode(XmlNodeType.Element, "action");
						SetNodeAttribute(action, AndroidNameAttribute, a);
						pair.Key.AppendChild(action);
					}
					
					foreach (var c in pair.Value.categories)
					{
						if (HasName(categories, c))
							continue;
						var category = GenerateNode(XmlNodeType.Element, "category");
						SetNodeAttribute(category, AndroidNameAttribute, c);
						pair.Key.AppendChild(category);
					}
					
					foreach (var d in pair.Value.data)
					{
						var dataNode = GenerateNode(XmlNodeType.Element, "data");
						foreach (var attr in d)
						{
							SetNodeAttribute(dataNode, attr.Key, attr.Value);
						}
						
						if (!HasNode(data, dataNode))
							pair.Key.AppendChild(dataNode);
					}
				}
			}
			
			if (!string.IsNullOrEmpty(requirements.permission) && 
				GetNodeAttribute(context, AndroidPermissionAttribute) != requirements.permission)
				errors.Add(type + " " + name + " doesn't have required permission attribute");
			
			if (!string.IsNullOrEmpty(requirements.process) && 
				GetNodeAttribute(context, AndroidProcessAttribute) != requirements.process)
				errors.Add(type + " " + name + " doesn't have required process attribute");
			
			if (!string.IsNullOrEmpty(requirements.exported) && 
				GetNodeAttribute(context, AndroidExportedAttribute) != requirements.exported)
				errors.Add(type + " " + name + " doesn't have required exported attribute");
			
			if (!string.IsNullOrEmpty(requirements.theme) && 
				GetNodeAttribute(context, AndroidThemeAttribute) != requirements.theme)
				warnings.Add(type + " " + name + " has theme attribute different from required");
			
			foreach (var cc in requirements.configChanges)
			{
				if (!existingConfigChanges.Contains(cc))
				{
					errors.Add(type + " " + name + " doesn't have all required configChanges");
					break;
				}
			}
			
			foreach (var pair in intentFilters)
			{
				var actions = pair.Key.SelectNodes("action");
				var categories = pair.Key.SelectNodes("category");
				var data = pair.Key.SelectNodes("data");
				
				foreach (var a in pair.Value.actions)
				{
					if (!HasName(actions, a))
						errors.Add(type + " " + name + " doesn't have " + a + " action");
				}
				
				foreach (var c in pair.Value.categories)
				{
					if (!HasName(categories, c))
						errors.Add(type + " " + name + " doesn't have " + c + " category");
				}
				
				foreach (var a in pair.Value.excludedActions)
				{
					if (HasName(actions, a))
						errors.Add(type + " " + name + " contains " + a + " action");
				}
				
				foreach (var c in pair.Value.excludedCategories)
				{
					if (HasName(categories, c))
						errors.Add(type + " " + name + " contains " + c + " category");
				}
				
				foreach (var d in pair.Value.data)
				{
					string attributesList = "";
					var dataNode = GenerateNode(XmlNodeType.Element, "data");
					foreach (var attr in d)
					{
						SetNodeAttribute(dataNode, attr.Key, attr.Value);
					attributesList += attributesList.Length > 0 ? ", " : "";
					attributesList += attr.Key + " = " + attr.Value;
					}
					
					if (!HasNode(data, dataNode))
						errors.Add(type + " " + name + " doesn't have [" + attributesList + "] data");
				}
				
				foreach (var d in pair.Value.excludedData)
				{
					string attributesList = "";
					var dataNode = GenerateNode(XmlNodeType.Element, "data");
					foreach (var attr in d)
					{
						SetNodeAttribute(dataNode, attr.Key, attr.Value);
					attributesList += attributesList.Length > 0 ? ", " : "";
					attributesList += attr.Key + " = " + attr.Value;
					}
					
					if (HasNode(data, dataNode))
						errors.Add(type + " " + name + " contains [" + attributesList + "] data");
				}
			}
		}
		
		private void RemoveGeneratedNodes(XmlNode node)
		{
			var generatedNodes = new List<XmlNode>();
			for (var i = 0; i < node.ChildNodes.Count; ++i)
			{
				var child = node.ChildNodes.Item(i);
				if (GetNodeAttribute(child, GeneratorAttribute, "") == requirements.generator)
					generatedNodes.Add(child);
			}
			
			foreach(var gn in generatedNodes)
			{
				node.RemoveChild(gn);
			}
		}
		
		private bool Check(UnityActivitiesRequirements r, List<string> errors, List<string> warnings, bool fix)
		{
			var application = doc.SelectSingleNode("/manifest/application");
			var activities = doc.SelectNodes("/manifest/application/activity");
			
			XmlNode playerProxyActivity = null;
			XmlNode playerNativeActivity = null;
			XmlNode playerActivity = null;
			
			var nodesToDelete = new List<XmlNode>();
			
			for (var i = 0; i < activities.Count; ++i)
			{
				var activity = activities.Item(i);
				var name = GetNodeAttribute(activity, AndroidNameAttribute);
				if (r.recognizedPlayerProxyActivity.ContainsKey(name))
				{
					if (playerProxyActivity != null)
					{
						errors.Add("Multiple unity player proxy activities detected");
						if (fix)
							nodesToDelete.Add(activity);
					}
					else
					{
						playerProxyActivity = activity;
					}
				}
				if (r.recognizedPlayerNativeActivity.ContainsKey(name))
				{
					if (playerNativeActivity != null)
					{
						errors.Add("Multiple unity player native activities detected");
						if (fix)
							nodesToDelete.Add(activity);
					}
					else
					{
						playerNativeActivity = activity;
					}
				}
				if (r.recognizedPlayerActivity.ContainsKey(name))
				{
					if (playerActivity != null)
					{
						errors.Add("Multiple unity player activities detected");
						if (fix)
							nodesToDelete.Add(activity);
					}
					else
					{
						playerActivity = activity;
					}
				}
			}
			
			if (playerActivity == null && playerNativeActivity == null && playerProxyActivity == null)
			{
				return false;
			}
#if UNITY_4_3_PLUS
			if (playerNativeActivity == null)
			{
				errors.Add("Unity player activities inconsistency");
				return true;
			}
			
			if (fix)
			{
				foreach(var n in nodesToDelete)
					application.RemoveChild(n);
				
				SetNodeAttribute(playerNativeActivity, AndroidNameAttribute, r.playerNativeActivity);
			}
			
			Check(playerNativeActivity, r.recognizedPlayerNativeActivity[r.playerNativeActivity], errors, warnings, fix);
#else
			if ((playerActivity != null || playerNativeActivity != null || playerProxyActivity != null) &&
			    (playerActivity == null || playerNativeActivity == null || playerProxyActivity == null))
			{
				errors.Add("Unity player activities inconsistency");
				return true;
			}

			
			if (fix)
			{
				foreach(var n in nodesToDelete)
					application.RemoveChild(n);
				
				SetNodeAttribute(playerActivity, AndroidNameAttribute, r.playerActivity);
				SetNodeAttribute(playerProxyActivity, AndroidNameAttribute, r.playerProxyActivity);
				SetNodeAttribute(playerNativeActivity, AndroidNameAttribute, r.playerNativeActivity);
			}
			
			Check(playerActivity, r.recognizedPlayerActivity[r.playerActivity], errors, warnings, fix);
			Check(playerProxyActivity, r.recognizedPlayerProxyActivity[r.playerProxyActivity], errors, warnings, fix);
			Check(playerNativeActivity, r.recognizedPlayerNativeActivity[r.playerNativeActivity], errors, warnings, fix);
#endif
			
			return true;
		}
		
		private void Check(List<string> errors, List<string> warnings, bool fix)
		{
			errors.Clear();
			warnings.Clear();
			doc = null;
			
			if (!File.Exists(requirements.manifestFilename))
			{
				if (fix)
				{
					File.Copy(requirements.defaultManifestFilename, requirements.manifestFilename, true);
				}
				else
				{
					errors.Add("AndroidManifest.xml file not found");
					return;
				}
			}
			
			doc = new XmlDocument();
			doc.Load(requirements.manifestFilename);
			
			if (fix)
			{
				RemoveGeneratedNodes(doc.SelectSingleNode("/manifest"));
				RemoveGeneratedNodes(doc.SelectSingleNode("/manifest/application"));
			}
			
			if (!string.IsNullOrEmpty(requirements.bundleIdEndsWith) &&
				!PlayerSettings.bundleIdentifier.EndsWith(requirements.bundleIdEndsWith))
			{
				errors.Add("Bundle ID must end with " + requirements.bundleIdEndsWith);
			}
			
			foreach (var md in requirements.metaDatas)
			{
				if (fix)
					SetMetaData(md.Key, md.Value);
			}
			
			foreach (var n in requirements.namespaces)
			{
				if (fix && doc != null)
				{
					if (!doc.DocumentElement.HasAttribute(n.Key))
						doc.DocumentElement.SetAttribute(n.Key, n.Value);
				}
			}
			
			foreach (var mdc in requirements.metaDataConstraints)
			{
				Check(mdc.Key, mdc.Value, errors, warnings, fix);
			}
			
			foreach (var p in requirements.permissions)
			{
				if (fix)
					SetPermission(p.Key, Value(p.Value), Forced(p.Value));
				
				var permission = FindPermission(p.Key);
				if (permission == null)
					errors.Add("Permission " + p.Key + " is not defined");
				else if (p.Value == ProtectionLevel.Signature &&
					GetNodeAttribute(permission, AndroidProtectionLevelAttribute) != "signature")
					errors.Add("Permission " + p.Key + " has incorrect protection level");
			}
			
			foreach (var up in requirements.usesPermissions)
			{
				if (fix)
					SetUsesPermission(up.Key, Value(up.Value), Forced(up.Value));
				
				var usesPermission = FindUsesPermission(up.Key);
				if (usesPermission == null)
					errors.Add("Permission usage " + up.Key + " is not defined");
			}
			
			foreach (var uf in requirements.usesFeatures)
			{
				if (fix)
					SetUsesFeature(uf.Key, Value(uf.Value), Forced(uf.Value));
				
				var usesFeature = FindUsesFeature(uf.Key);
				if (usesFeature == null)
					errors.Add("Feature usage " + uf.Key + " is not defined");
			}
			
			foreach (var c in requirements.contexts)
			{
				Check(c.Value.type, c.Key, c.Value, errors, warnings, fix);
			}
			
			if (requirements.unityActivitiesRequirements != null)
			{
				unityActivitiesAreCustomizable = Check(requirements.unityActivitiesRequirements, errors, warnings, fix);
			}
			
			if (fix)
			{
				doc.Save(requirements.manifestFilename);
			}
		}
		#endregion
		
		#region Attributes
		private XmlNode GenerateNode(XmlNodeType nodeType, string type, string @namespace = "")
		{
			var node = doc.CreateNode(nodeType, type, @namespace);
			if (!string.IsNullOrEmpty(requirements.generator))
				SetNodeAttribute(node, GeneratorAttribute, requirements.generator, "");
			return node;
		}
		
		private string GetNodeAttribute(XmlNode node, string name, string @namespace = AndroidXmlNamespace)
		{
			if (node == null || node.Attributes == null)
			{
				return null;
			}
			var nodeAttribute = node.Attributes.GetNamedItem(name, @namespace);
			return (nodeAttribute == null) ? null : nodeAttribute.Value;
		}
		
		private void SetNodeAttribute(XmlNode node, string name, string value, string @namespace = AndroidXmlNamespace)
		{
			var attribute = (XmlAttribute)node.Attributes.GetNamedItem(name, @namespace);
			if (value == null)
			{
				if (attribute != null)
				{
					node.Attributes.RemoveNamedItem(name, @namespace);
				}
			}
			else
			{
				if (attribute != null)
				{
					attribute.Value = value;
				}
				else
				{
					attribute = (XmlAttribute)doc.CreateNode(XmlNodeType.Attribute, "", name, @namespace);
					attribute.Value = value;
					node.Attributes.Append(attribute);
				}
			}
		}
		#endregion
		
		#region Permission
		private void SetPermission(string name, string protectionLevel, bool forceProtectionLevel)
		{
			var permission = FindPermission(name) ?? CreatePermission(name);
			if (forceProtectionLevel)
				SetNodeAttribute(permission, AndroidProtectionLevelAttribute, protectionLevel);
		}
		
		private XmlNode FindPermission(string name)
		{
			var permissions = doc.SelectNodes("/manifest/permission");
			for (var i = 0; i < permissions.Count; ++i)
			{
				var permission = permissions.Item(i);
				if (GetNodeAttribute(permission, AndroidNameAttribute) == name)
					return permission;
			}
			return null;
		}
		
		private XmlNode CreatePermission(string name)
		{		
			var permission = GenerateNode(XmlNodeType.Element, "permission");
			SetNodeAttribute(permission, AndroidNameAttribute, name);
	
			var manifest = doc.SelectSingleNode("/manifest");
			if (manifest != null)
				manifest.AppendChild(permission);
			
			return permission;
		}
		#endregion
		
		#region UsesFeature
		private void SetUsesFeature(string name, string required, bool forceRequired)
		{
			var feature = FindUsesFeature(name) ?? CreateUsesFeature(name);
			if (forceRequired)
				SetNodeAttribute(feature, AndroidRequiredAttribute, required);
		}
		
		private XmlNode FindUsesFeature(string name)
		{
			var features = doc.SelectNodes("/manifest/uses-feature");
			for (var i = 0; i < features.Count; ++i)
			{
				var feature = features.Item(i);
				if (GetNodeAttribute(feature, AndroidNameAttribute) == name)
					return feature;
			}
			return null;
		}
		
		private XmlNode CreateUsesFeature(string name)
		{
			var feature = GenerateNode(XmlNodeType.Element, "uses-feature");
			SetNodeAttribute(feature, AndroidNameAttribute, name);
			
			var manifest = doc.SelectSingleNode("/manifest");
			if (manifest != null)
				manifest.AppendChild(feature);
			
			return feature;
		}
		#endregion
		
		#region UsesPermission
		private void SetUsesPermission(string name, string required, bool forceRequired)
		{
			var permission = FindUsesPermission(name) ?? CreateUsesPermission(name);
			if (forceRequired)
				SetNodeAttribute(permission, AndroidRequiredAttribute, required);
		}
		
		private XmlNode FindUsesPermission(string name)
		{
			var permissions = doc.SelectNodes("/manifest/uses-permission");
			for (var i = 0; i < permissions.Count; ++i)
			{
				var permission = permissions.Item(i);
				if (GetNodeAttribute(permission, AndroidNameAttribute) == name)
					return permission;
			}
			return null;
		}
		
		private XmlNode CreateUsesPermission(string name)
		{
			var permission = GenerateNode(XmlNodeType.Element, "uses-permission");
			SetNodeAttribute(permission, AndroidNameAttribute, name);
	
			var manifest = doc.SelectSingleNode("/manifest");
			if (manifest != null)
				manifest.AppendChild(permission);
			
			return permission;
		}
		#endregion
		
		#region MetaData
		public bool Exists { get { return doc != null; } }
		
		public bool HasMetaData(string name)
		{
			if (doc == null)
				return false;
			
			var metaDatas = doc.SelectNodes("/manifest/application/meta-data");
			for (var i = 0; i < metaDatas.Count; ++i)
			{
				var metaData = metaDatas.Item(i);
				if (GetNodeAttribute(metaData, AndroidNameAttribute) == name)
				{
					return true;
				}
			}
			return false;
		}
		
		public string GetMetaData(string name)
		{
			var metaDatas = doc.SelectNodes("/manifest/application/meta-data");
			for (var i = 0; i < metaDatas.Count; ++i)
			{
				var metaData = metaDatas.Item(i);
				if (GetNodeAttribute(metaData, AndroidNameAttribute) == name)
				{
					return GetNodeAttribute(metaData, AndroidValueAttribute) ?? "";
				}
			}
			return "";
		}
		
		private void SetMetaData(string name, string value)
		{
			var metaData = FindMetaData(name);
			if (metaData != null)
				SetNodeAttribute(metaData, AndroidValueAttribute, value);
			else
				CreateMetaData(name, value);
		}
		
		private XmlNode FindMetaData(string name)
		{
			var metaDatas = doc.SelectNodes("/manifest/application/meta-data");
			for (var i = 0; i < metaDatas.Count; ++i)
			{
				var metaData = metaDatas.Item(i);
				if (GetNodeAttribute(metaData, AndroidNameAttribute) == name)
					return metaData;
			}
			return null;
		}
		
		private XmlNode CreateMetaData(string name, string value)
		{
			var metaData = GenerateNode(XmlNodeType.Element, "meta-data");
			SetNodeAttribute(metaData, AndroidNameAttribute, name);
			SetNodeAttribute(metaData, AndroidValueAttribute, value);
			
			var app = doc.SelectSingleNode("/manifest/application");
			if (app != null)
				app.AppendChild(metaData);
			
			return metaData;
		}
		#endregion
		
		#region Contexts
		private XmlNode FindContext(string type, string name)
		{
			var contexts = doc.SelectNodes("/manifest/application/" + type);
			for (var i = 0; i < contexts.Count; ++i)
			{
				var context = contexts.Item(i);
				if (GetNodeAttribute(context, AndroidNameAttribute) == name)
					return context;
			}
			return null;
		}
		
		private XmlNode CreateContext(string type, string name, string @namespace)
		{
			var context = GenerateNode(XmlNodeType.Element, type, @namespace);
			SetNodeAttribute(context, AndroidNameAttribute, name);
			
			var app = doc.SelectSingleNode("/manifest/application");
			if (app != null)
				app.AppendChild(context);
			
			return context;
		}
		#endregion
	}
}
