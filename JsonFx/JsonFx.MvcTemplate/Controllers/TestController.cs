using System;
using System.Net;
using System.Web;
using System.Web.Mvc;

using JsonFx.Mvc;
using MyApp.Models;

namespace MyApp.Controllers
{
	/// <summary>
	/// Alternate implementation of JSON-RPC service as REST controller
	/// </summary>
	public class TestController : LiteController
	{
		#region REST Actions

		/// <summary>
		/// REST endpoint will be /test/getInfo
		/// </summary>
		/// <param name="info">a TestInfo object</param>
		/// <returns>TestInfo</returns>
		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult GetInfo(TestInfo info)
		{
			if (info == null)
			{
				return this.DataResult("argument was null", HttpStatusCode.BadRequest);
			}

			// populate data to be bound to a JBST
			info.TimeStamp = this.GetTimeStamp();
			info.Machine = this.GetMachine();

			return this.DataResult(info);
		}

		#endregion REST Actions

		#region Utility Methods

		/* these are not exposed as Actions since they do not return ActionResults */

		public DateTime GetTimeStamp()
		{
			return DateTime.UtcNow;
		}

		public string GetMachine()
		{
			return HttpContext.Current.Server.MachineName;
		}

		#endregion Utility Methods
	}
}
