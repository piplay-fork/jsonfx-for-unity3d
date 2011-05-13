using System;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Serialization;

using JsonFx.Json;
using JsonFx.Xml;

namespace JsonFx.Mvc
{
	/// <summary>
	/// A light-weight Controller base for basis of a Layer Supertype.
	/// Encourages IoC and reduces default clutter.
	/// </summary>
	public abstract class LiteController :
		ControllerBase,
		IExceptionFilter
	{
		#region Error Handling Methods

		void IExceptionFilter.OnException(ExceptionContext context)
		{
			this.OnException(context);
		}

		protected virtual void OnException(ExceptionContext context)
		{
		}

		#endregion Error Handling Methods

		#region ActionResult Methods

		/// <summary>
		/// Override with IoC container resolution of DataResult
		/// </summary>
		/// <returns></returns>
		protected virtual DataResult DataResult()
		{
			bool isDebug =
				this.ControllerContext.HttpContext != null ?
				this.ControllerContext.HttpContext.IsDebuggingEnabled :
				false;

			return new DataResult(
				new DataWriterProvider(new IDataWriter[]
				{
					new JsonDataWriter(JsonDataWriter.CreateSettings(isDebug)),
					new XmlDataWriter(XmlDataWriter.CreateSettings(Encoding.UTF8, isDebug), new XmlSerializerNamespaces())
				}));
		}

		protected DataResult DataResult(object data)
		{
			DataResult result = this.DataResult();

			result.Data = data;

			return result;
		}

		protected DataResult DataResult(object data, HttpStatusCode status)
		{
			DataResult result = this.DataResult(data);

			if (status != HttpStatusCode.OK)
			{
				result.HttpStatusCode = status;
			}

			return result;
		}

		protected ViewResult View()
		{
			return View(/*viewName*/ null, /*masterName*/ null, /*model*/ null);
		}

		protected ViewResult View(object model)
		{
			return View(/*viewName*/ null, /*masterName*/ null, model);
		}

		protected ViewResult View(string viewName)
		{
			return View(viewName, /*masterName*/ null, /*model*/ null);
		}

		protected ViewResult View(string viewName, object model)
		{
			return View(viewName, /*masterName*/ null, model);
		}

		protected virtual ViewResult View(string viewName, string masterName, object model)
		{
			if (model != null)
			{
				this.ViewData.Model = model;
			}

			return new ViewResult
			{
				ViewName = viewName,
				MasterName = masterName,
				ViewData = this.ViewData,
				TempData = this.TempData
			};
		}

		#endregion ActionResult Methods

		#region ControllerBase Members

		private IActionInvoker actionInvoker;

		/// <summary>
		/// Override with IoC container resolution of IActionInvoker
		/// </summary>
		/// <returns></returns>
		protected virtual IActionInvoker ActionInvoker
		{
			get
			{
				if (this.actionInvoker == null)
				{
					this.actionInvoker = new ControllerActionInvoker();
				}
				return this.actionInvoker;
			}
			set { this.actionInvoker = value; }
		}

		protected override void ExecuteCore()
		{
			RouteData routeData =
				(this.ControllerContext == null) ? null :
				this.ControllerContext.RouteData;

			string actionName = routeData.GetRequiredString("action");
			if (!this.ActionInvoker.InvokeAction(this.ControllerContext, actionName))
			{
				this.HandleUnknownAction(actionName);
			}
		}

		protected virtual void HandleUnknownAction(string actionName)
		{
			throw new HttpException(
				(int)HttpStatusCode.NotFound,
				String.Format("A public action method '{0}' could not be found on controller '{1}'.", actionName, this.GetType().FullName));
		}

		#endregion ControllerBase Members
	}
}
