using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace JsonFx.Mvc
{
	/// <summary>
	/// Customizes the HTTP Headers to remove default cruft and add any custom headers
	/// </summary>
	public class CleanHeadersModule : IHttpModule
	{
		#region Constants

		private const string ServerHeader = "Server";
		private const string VersionHeader = "X-AspNet-Version";

		#endregion Constants

		#region Fields

		private HttpApplication Application;
		private static readonly IDictionary<string, string> headers = new Dictionary<string, string>();

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets a collection of headers to add to every request
		/// </summary>
		public static IDictionary<string, string> Headers
		{
			get { return CleanHeadersModule.headers; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Defaults to "{Name}/{Version}" of Assembly
		/// </summary>
		/// <returns></returns>
		public static string BuildServerHeader(Assembly assembly)
		{
			if (assembly == null)
			{
				return String.Empty;
			}

			AssemblyName info = assembly.GetName();

			return String.Concat(
				info.Name,
				"/",
				info.Version.Major.ToString(),
				".",
				info.Version.Minor.ToString());
		}

		#endregion Methods

		#region IHttpModule Members

		void IHttpModule.Init(HttpApplication app)
		{
			this.Application = app;
			this.Application.EndRequest += this.OnEndRequest;

			MvcHandler.DisableMvcResponseHeader = true;
		}

		void IHttpModule.Dispose() { }

		#endregion IHttpModule Members

		#region Request Events

		private void OnEndRequest(object sender, EventArgs e)
		{
			HttpResponse response = this.Application.Context.Response;

			// customize the "Server" HTTP Header
			try
			{
				response.Headers.Remove(CleanHeadersModule.ServerHeader);
				response.Headers.Remove(CleanHeadersModule.VersionHeader);
			}
			catch (PlatformNotSupportedException)
			{
				// disable on IIS 6
				this.Application.EndRequest -= this.OnEndRequest;
			}

			foreach (string name in CleanHeadersModule.Headers.Keys)
			{
				response.AppendHeader(name, CleanHeadersModule.Headers[name]);
			}
		}

		#endregion Request Events
	}
}