using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace JbstOnline.Mvc.ModelBinders
{
	public class TextReaderBinder : IModelBinder
	{
		#region IModelBinder Members

		public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			HttpRequestBase request = controllerContext.HttpContext.Request;

			return new StreamReader(request.InputStream, request.ContentEncoding);
		}

		#endregion IModelBinder Members
	}
}
