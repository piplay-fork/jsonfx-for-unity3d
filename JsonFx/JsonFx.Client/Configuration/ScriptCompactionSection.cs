using System;
using System.Configuration;

namespace JsonFx.Configuration
{
	/// <summary>
	/// Allows the app control over script compaction settings
	/// </summary>
	public class ScriptCompactionSection : System.Configuration.ConfigurationSection
	{
		#region Constants

		private const string DefaultSectionPath = "jsonfxSettings/scriptCompaction";

		private const string Key_DisableMicroOptimizations = "disableMicroOptimizations";
		private const string Key_Firewall = "firewallScripts";
		private const string Key_IgnoreEval = "ignoreEval";
		private const string Key_Obfuscate = "obfuscate";
		private const string Key_PreserveSemicolons = "preserveSemicolons";
		private const string Key_Verbose = "verbose";
		private const string Key_WordWrapWidth = "wordWrapWidth";

		#endregion Constants

		#region Properties

		[ConfigurationProperty(Key_DisableMicroOptimizations, DefaultValue="true", IsRequired=false)]
		public bool DisableMicroOptimizations
		{
			get
			{
				try
				{
					return (bool)this[Key_DisableMicroOptimizations];
				}
				catch
				{
					return true;
				}
			}
			set { this[Key_DisableMicroOptimizations] = value; }
		}

		[ConfigurationProperty(Key_Firewall, DefaultValue="true", IsRequired=false)]
		public bool Firewall
		{
			get
			{
				try
				{
					return (bool)this[Key_Firewall];
				}
				catch
				{
					return true;
				}
			}
			set { this[Key_Firewall] = value; }
		}

		[ConfigurationProperty(Key_IgnoreEval, DefaultValue="true", IsRequired=false)]
		public bool IgnoreEval
		{
			get
			{
				try
				{
					return (bool)this[Key_IgnoreEval];
				}
				catch
				{
					return true;
				}
			}
			set { this[Key_IgnoreEval] = value; }
		}

		[ConfigurationProperty(Key_Obfuscate, DefaultValue="false", IsRequired=false)]
		public bool Obfuscate
		{
			get
			{
				try
				{
					return (bool)this[Key_Obfuscate];
				}
				catch
				{
					return false;
				}
			}
			set { this[Key_Obfuscate] = value; }
		}

		[ConfigurationProperty(Key_PreserveSemicolons, DefaultValue="true", IsRequired=false)]
		public bool PreserveSemicolons
		{
			get
			{
				try
				{
					return (bool)this[Key_PreserveSemicolons];
				}
				catch
				{
					return true;
				}
			}
			set { this[Key_PreserveSemicolons] = value; }
		}

		[ConfigurationProperty(Key_Verbose, DefaultValue="false", IsRequired=false)]
		public bool Verbose
		{
			get
			{
				try
				{
					return (bool)this[Key_Verbose];
				}
				catch
				{
					return false;
				}
			}
			set { this[Key_Verbose] = value; }
		}

		[ConfigurationProperty(Key_WordWrapWidth, DefaultValue="-1", IsRequired=false)]
		public int WordWrapWidth
		{
			get
			{
				try
				{
					return (int)this[Key_WordWrapWidth];
				}
				catch
				{
					return -1;
				}
			}
			set { this[Key_WordWrapWidth] = value; }
		}

		#endregion Properties

		#region Methods

		public static ScriptCompactionSection GetSettings()
		{
			return ScriptCompactionSection.GetSettings(DefaultSectionPath);
		}

		public static ScriptCompactionSection GetSettings(string sectionPath)
		{
			ScriptCompactionSection config = null;
			try
			{
				config = (ScriptCompactionSection)ConfigurationManager.GetSection(sectionPath);
			}
			catch {}

			return config??new ScriptCompactionSection();
		}

		#endregion Methods
	}
}
