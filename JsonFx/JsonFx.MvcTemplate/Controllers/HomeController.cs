using System;
using System.Web;
using System.Web.Mvc;

using JsonFx.Mvc;
using MyApp.Models;

namespace MyApp.Controllers
{
	public class HomeController : LiteController
	{
		public ActionResult Index()
		{
			// populate data to be used in a JBST
			return this.View(
				new HomeViewModel
				{
					RenderTime = DateTime.Now,
					ServerName = HttpContext.Current.Server.MachineName,
					JsonFxVersion = JsonFx.About.Fx.Version
				});
		}
	}
}
