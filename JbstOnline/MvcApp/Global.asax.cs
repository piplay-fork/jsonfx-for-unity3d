//#define GZIP

using System;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;

using JbstOnline.Mvc.IoC;
using JbstOnline.Mvc.ModelBinders;
using JsonFx.Mvc;
using Ninject;
using Ninject.Web.Mvc;

namespace JbstOnline
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : NinjectHttpApplication
	{
		protected virtual void RegisterRoutes(RouteCollection routes)
		{
			routes.MapRoute(
				"Compile",
				"compiler",
				new { controller = "Jbst", action = "Compile" },
				new { httpMethod = new HttpMethodConstraint("POST") });

			routes.MapRoute(
				"PrettyPrinted",
				"compiler/scripts",
				new { controller = "Jbst", action = "SupportScripts" },
				new { httpMethod = new HttpMethodConstraint("GET") });

			routes.MapRoute(
				"Compacted",
				"compiler/compacted",
				new { controller = "Jbst", action = "ScriptsCompacted" },
				new { httpMethod = new HttpMethodConstraint("GET") });

			routes.MapRoute(
				"test",
				"test",
				new { controller = "Jbst", action = "Test" },
				new { httpMethod = new HttpMethodConstraint("POST") });

			routes.MapRoute(
				"Default",
				"{controller}",
				new { controller = "Home", action = "Index" },
				new { httpMethod = new HttpMethodConstraint("GET") }
			);
		}

		private void RegisterBinders()
		{
			// allows this to automatically be bound from the post body
			DataModelBinder binder = this.Kernel.Get<DataModelBinder>();
			binder.DefaultBinder = ModelBinders.Binders.DefaultBinder;

			// set as the new default
			ModelBinders.Binders.DefaultBinder = binder;

			// binder for reading the raw post-body
			ModelBinders.Binders[typeof(TextReader)] = new TextReaderBinder();
		}

		#region Ninject

		protected override IKernel CreateKernel()
		{
			return new StandardKernel(new AppIocModule());
		}

		protected override void OnApplicationStarted()
		{
			this.RegisterRoutes(RouteTable.Routes);
			this.RegisterBinders();

			CleanHeadersModule.Headers["Server"] = CleanHeadersModule.BuildServerHeader(Assembly.GetExecutingAssembly());
			CleanHeadersModule.Headers["X-JsonFx-Version"] = JsonFx.About.Fx.Version.ToString();
		}

		#endregion Ninject

		#region Stream Compression

#if GZIP
		protected void Application_PreRequestHandlerExecute(object sender, System.EventArgs e)
		{
			Page page = this.Context.Handler as Page;
			if (page != null)
			{
				page.PreRenderComplete += new EventHandler(this.Page_PreRenderComplete);
			}
		}

		private void Page_PreRenderComplete(object sender, EventArgs e)
		{
			this.Error += new EventHandler(this.Page_Error);

			// improve the Yslow rating by compressing output
			JsonFx.Handlers.ResourceHandler.EnableStreamCompression(this.Context);
		}

		private void Page_Error(object sender, System.EventArgs e)
		{
			// remove compression in error conditions
			JsonFx.Handlers.ResourceHandler.DisableStreamCompression(this.Context);
		}
#endif
		#endregion Stream Compression
	}
}