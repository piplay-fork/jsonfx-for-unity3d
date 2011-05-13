using System;
using System.Web.Compilation;
using System.Web.Mvc;

using JbstOnline.Models;
using JsonFx.Handlers;

namespace JbstOnline.Controllers
{
	public class HomeController : AppControllerBase
	{
		private const string SamplePath = "~/Views/Example/Foo.MyZebraList.jbst";

		public ActionResult Index()
		{
			IOptimizedResult sample = ResourceHandler.Create<IOptimizedResult>(HomeController.SamplePath);
			IOptimizedResult support = ResourceHandler.Create<IOptimizedResult>(JbstController.SupportScriptPath);

			// populate data to be used in view
			return this.View(new HomeViewModel(sample.Source, support));
		}
	}
}
