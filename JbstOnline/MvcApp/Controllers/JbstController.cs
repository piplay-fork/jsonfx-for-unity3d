using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Mvc;

using JbstOnline.Models;
using JbstOnline.Mvc.ActionResults;
using JsonFx.BuildTools;
using JsonFx.Handlers;
using JsonFx.UI.Jbst;

namespace JbstOnline.Controllers
{
	public class JbstController : AppControllerBase
	{
		#region Constants

		public const string SupportScriptPath = "~/Scripts/JBST.Merge";

		#endregion Constants

		#region Controller Actions

		public ActionResult Compile(TextReader source)
		{
			List<ParseException> compilationErrors = new List<ParseException>();
			List<ParseException> compactionErrors = new List<ParseException>();

			IOptimizedResult result = new JbstCompiler().Compile(source, null, compilationErrors, compactionErrors);

			string jbstName = (result is JbstBuildResult) ?
				(string)((JbstBuildResult)result).JbstName : String.Empty;

			HttpStatusCode statusCode = HttpStatusCode.OK;
			object data;
			if (compilationErrors.Count > 0)
			{
				statusCode = HttpStatusCode.BadRequest;
				List<object> foo = new List<object>(compilationErrors.Count);
				foreach (ParseException ex in compilationErrors)
				{
					foo.Add(new
					{
						Message = ex.Message,
						Line = ex.Line,
						Col = ex.Column
					});
				}
				data = new
				{
					name = jbstName,
					key = result.Hash,
					source = result.Source,
					errors = foo
				};
			}
			else if (compactionErrors.Count > 0)
			{
				statusCode = HttpStatusCode.BadRequest;
				List<CompilationError> foo = new List<CompilationError>(compactionErrors.Count);
				foreach (ParseException ex in compactionErrors)
				{
					foo.Add(new CompilationError
					{
						Message = ex.Message,
						Line = ex.Line,
						Col = ex.Column
					});
				}
				data = new CompilationResult
				{
					name = jbstName,
					key = result.Hash,
					source = result.PrettyPrinted,
					errors = foo
				};
			}
			else
			{
				data = new CompilationResult
				{
					name = jbstName,
					key = result.Hash,
					pretty = result.PrettyPrinted,
					compacted = result.Compacted
				};
			}

			return this.DataResult(data, statusCode);
		}

#if DEBUG
		public ActionResult Test(CompilationResult result)
		{
			return this.DataResult(result);
		}
#endif

		public ActionResult SupportScripts()
		{
			ResourceResult result = new ResourceResult(JbstController.SupportScriptPath);
			result.Filename = "jbst.js";
			result.IsAttachment = true;
			result.IsDebug = true;
			return result;
		}

		public ActionResult ScriptsCompacted()
		{
			ResourceResult result = new ResourceResult(JbstController.SupportScriptPath);
			result.Filename = "jbst.min.js";
			result.IsAttachment = true;
			result.IsDebug = false;
			return result;
		}

		#endregion Controller Actions

	}
}
