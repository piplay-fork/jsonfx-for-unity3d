using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace JsonFx.Compilation
{
	public class DirectiveParser
	{
		#region Constants

		private static readonly Regex Regex_Directive = new Regex(Pattern_Directive, RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.Compiled);
		private const string Pattern_Directive = "<%\\s*@(\\s*(?<attrname>\\w[\\w:]*(?=\\W))(\\s*(?<equal>=)\\s*\"(?<attrval>[^\"]*)\"|\\s*(?<equal>=)\\s*'(?<attrval>[^']*)'|\\s*(?<equal>=)\\s*(?<attrval>[^\\s%>]*)|(?<equal>)(?<attrval>\\s*?)))*\\s*?%>";
		private const string ErrorDuplicateAttrib = "The directive contains duplicate \"{0}\" attributes.";

		#endregion Constants

		#region Fields

		private int lineNumber = 1;
		private readonly string virtualPath;
		private readonly string sourceText;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="sourceText"></param>
		/// <param name="virtualPath"></param>
		public DirectiveParser(string sourceText, string virtualPath)
		{
			this.virtualPath = virtualPath;
			this.sourceText = sourceText;
		}

		#endregion Init

		#region Parsing Methods

		public int ParseDirectives(out int lineNumber)
		{
			try
			{
				int index = 0;
				int oldIndex = 0;
				string directiveName;
				IDictionary<string, string> attribs;

				while (this.ParseDirective(this.sourceText, out directiveName, out attribs, ref index))
				{
					while (oldIndex < index)
					{
						oldIndex = this.sourceText.IndexOf('\n', oldIndex);
						if (oldIndex < 0 || oldIndex >= index)
						{
							break;
						}
						oldIndex++;// move past char
						this.lineNumber++;// inc line count
					}
					oldIndex = index;

					if (this.ProcessDirective != null)
					{
						this.ProcessDirective(directiveName, attribs, this.lineNumber);
					}
				}

				// remove the directive from the original source
				return index;
			}
			catch (HttpParseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new HttpParseException("ParseDirective: "+ex.Message, ex, this.virtualPath, this.sourceText, this.lineNumber);
			}
			finally
			{
				lineNumber = this.lineNumber;
			}
		}

		public delegate void ProcessDirectiveEvent(string directiveName, IDictionary<string, string> attribs, int lineNumber);
		public event ProcessDirectiveEvent ProcessDirective;

		/// <summary>
		/// Grabs the next directive.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="directiveName"></param>
		/// <param name="attribs"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private bool ParseDirective(string source, out string directiveName, out IDictionary<string, string> attribs, ref int index)
		{
			Match match = Regex_Directive.Match(source, index);
			if (!match.Success)
			{
				attribs = null;
				directiveName = null;
				return false;
			}

			index = match.Index+match.Length;
			attribs = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			directiveName = this.ParseAttributes(match, attribs);

			return true;
		}

		/// <summary>
		/// Parses the directive for its attributes.
		/// </summary>
		/// <param name="match"></param>
		/// <param name="attribs"></param>
		/// <returns>directive name</returns>
		private string ParseAttributes(Match match, IDictionary<string, string> attribs)
		{
			string directiveName = String.Empty;
			CaptureCollection names = match.Groups["attrname"].Captures;
			CaptureCollection values = match.Groups["attrval"].Captures;
			CaptureCollection equals = match.Groups["equal"].Captures;
			for (int i=0; i<names.Count; i++)
			{
				bool isAttribute = !String.IsNullOrEmpty(equals[i].Value);
				string name = names[i].Value;
				string value = values[i].Value;

				if (!String.IsNullOrEmpty(name))
				{
					if (!isAttribute && (i == 0))
					{
						directiveName = name;
					}
					else
					{
						if (attribs.ContainsKey(name))
						{
							throw new HttpParseException(String.Format(ErrorDuplicateAttrib, name), null, this.virtualPath, this.sourceText, this.lineNumber);
						}

						attribs[name] = value;
					}
				}
			}
			return directiveName;
		}

		#endregion Parsing Methods
	}
}
