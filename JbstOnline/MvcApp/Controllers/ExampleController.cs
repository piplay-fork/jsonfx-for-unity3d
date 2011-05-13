using System;
using System.Web.Mvc;

namespace JbstOnline.Controllers
{
	public class ExampleController : AppControllerBase
	{
		public ActionResult Index()
		{
			return this.View();
		}
	}
}
