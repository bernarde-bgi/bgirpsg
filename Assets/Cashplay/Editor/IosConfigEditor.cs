using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Cashplay
{
	public class IosConfig
	{
		public class ConstantRequirements
		{
			public bool isQuoted;
			public HashSet<string> possibleValues;
			public bool notEmpty;
			public bool isInteger;
		}
		
		public class Constant
		{
			public string value;
			public bool quoted;
			
			public Constant(string value, bool quoted)
			{
				this.value = value;
				this.quoted = quoted;
			}
		}
		
		public class Requirements
		{
			public string configFilename;
			public Dictionary<string, ConstantRequirements> constantConstraints = new Dictionary<string, ConstantRequirements>();
			public Dictionary<string, Constant> constants = new Dictionary<string, Constant>();
		}
		
		public class CheckResult
		{
			public IosConfig config;
			public List<string> errors = new List<string>();
			public List<string> warnings = new List<string>();
		}
		
		public static CheckResult Check(Requirements requirements, bool fix)
		{
			var result = new CheckResult();
			result.config = new IosConfig(requirements);
			result.config.Check(result.errors, result.warnings, fix);
			return result;
		}
		
		#region Constants
		public bool Exists { get { return constants != null; } }
		
		public bool HasConstant(string name)
		{
			if (constants == null)
				return false;
			
			return constants.ContainsKey(name);
		}
		
		public string GetConstant(string name)
		{
			if (constants == null)
				return "";
			if (!constants.ContainsKey(name))
				return "";
			return constants[name].value;
		}
		
		private void SetConstant(string name, Constant c)
		{
			if (constants.ContainsKey(name))
			{
				constants[name].value = c.value;
				constants[name].quoted = c.quoted;
			}
			else
			{
				constants.Add(name, new Constant(c.value, c.quoted));
			}
		}
		#endregion
		
		#region Checkup Routines
		private IosConfig(Requirements r)
		{
			requirements = r;
		}
		
		private Requirements requirements;
		private Dictionary<string, Constant> constants;
		
		private void Check(string name, ConstantRequirements requirements, List<string> errors, List<string> warnings, bool fix)
		{
			if (!constants.ContainsKey(name))
			{
				warnings.Add("[iOS] constant " + name + " is not specified");
				return;
			}
			
			var quoted = constants[name].quoted;
			var value = constants[name].value;
			
			if (requirements.isQuoted != quoted)
			{
				warnings.Add("[iOS] constant should " + (requirements.isQuoted ? "" : "not ") + "be surrounded with quotation marks");
				return;
			}
			if (requirements.possibleValues != null && !requirements.possibleValues.Contains(value))
			{
				var possibleValues = "";
				foreach(var p in requirements.possibleValues)
				{
					if (possibleValues.Length != 0)
						possibleValues += ", ";
					possibleValues += (p.Length == 0) ? "<empty>" : p;
				}
				warnings.Add("[iOS] constant " + name + " should have one of the following values:\n" + possibleValues);
				return;
			}
			if (requirements.notEmpty && string.IsNullOrEmpty(value))
			{
				warnings.Add("[iOS] constant " + name + " should not be empty");
				return;
			}
			int intValue;
			if (requirements.isInteger && !int.TryParse(value, out intValue))
			{
				warnings.Add("[iOS] constant " + name + " should be an integer value");
				return;
			}
		}
		
		private bool Check(List<string> errors, List<string> warnings, bool fix)
		{
			errors.Clear();
			warnings.Clear();
			constants = null;
			
			if (!File.Exists(requirements.configFilename))
			{
				if (fix)
				{
					constants = new Dictionary<string, Constant>();
				}
				else
				{
					errors.Add("iOS config file not found");
					return false;
				}
			}
			else
			{
				constants = new Dictionary<string, Constant>();
				var configContent = File.ReadAllText(requirements.configFilename);
				var configLines = configContent.Split('\n');
				foreach(var configLine in configLines)
				{
					var lineParts = configLine.Split(' ');
					if (lineParts.Length < 3)
						continue;
					if (lineParts[0] != "#define")
						continue;
					
					var name = lineParts[1];
					var value = lineParts[2];
					var quoted = value.StartsWith("\"") && value.EndsWith("\"");
					if (quoted)
						value = value.Trim('\"');
				
					if (!constants.ContainsKey(name))
					{
						constants.Add(name, new Constant(value, quoted));
					}
					else if (!fix)
					{
						errors.Add("iOS config contans multiple entries of " + name + " constant");
					}
				}
			}
			
			if (fix)
			{
				constants = new Dictionary<string, Constant>();
				foreach (var c in requirements.constants)
					SetConstant(c.Key, c.Value);
			}
			
			foreach (var cc in requirements.constantConstraints)
			{
				Check(cc.Key, cc.Value, errors, warnings, fix);
			}
			
			if (fix)
			{
				var configContent = "";
				foreach (var c in constants)
				{
					var q = c.Value.quoted ? "\"" : "";
					configContent += "#define " + c.Key + " " + q + c.Value.value + q + "\n";
				}
				File.WriteAllText(requirements.configFilename, configContent);
			}
			
			return errors.Count == 0;
		}
		#endregion
	}
}
