using System;
using System.Web;

using JsonFx.JsonRpc;
using MyApp.Models;

namespace MyApp.Services
{
	[JsonService(Namespace="Example", Name="Service")]
	public class MyService
	{
		#region Service Methods

		/* methods included in the JavaScript proxy */

		/// <summary>
		/// proxy function will be Example.Service.getInfo
		/// </summary>
		/// <param name="info">a TestInfo object</param>
		/// <returns>TestInfo</returns>
		[JsonMethod(Name="getInfo")]
		public TestInfo GetInfo(TestInfo info)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			// populate data to be bound to a JBST
			info.TimeStamp = this.GetTimeStamp();
			info.Machine = this.GetMachine();

			return info;
		}

		#endregion Service Methods

		#region Utility Methods

		/* these are not exposed in the JavaScript proxy since they do not have a JsonMethod attribute */

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