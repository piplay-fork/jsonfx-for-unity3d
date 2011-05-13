using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Web;

using JsonFx.Json;

namespace JsonFx.Mvc
{
	/// <summary>
	/// An action result for returning a custom non-UI error status.
	/// </summary>
	public class ErrorResult : HttpResult
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ErrorResult(Exception ex)
		{
			ex = this.EnsureException(ex);

			this.Error = ex;
			this.Message = ex.Message;
			this.HttpStatus = this.GetStatusCode(ex);
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the associated error
		/// </summary>
		public Exception Error
		{
			get;
			protected set;
		}

		#endregion Properties

		#region Exception Methods

		/// <summary>
		/// Determines the best HTTP status code for the exception
		/// </summary>
		/// <param name="ex"></param>
		/// <returns></returns>
		protected virtual HttpStatusCode GetStatusCode(Exception ex)
		{
			if (ex is HttpException)
			{
				return (HttpStatusCode)((HttpException)ex).GetHttpCode();
			}

			if (ex is FileNotFoundException ||
				ex is DirectoryNotFoundException ||
				ex is DriveNotFoundException)
			{
				return HttpStatusCode.NotFound;
			}

			if (ex is ArgumentException ||
				ex is FormatException ||
				ex is HttpRequestValidationException ||
				ex is JsonSerializationException ||
				ex is XmlSyntaxException)
			{
				return HttpStatusCode.BadRequest;
			}
			
			if (ex is SecurityException ||
				ex is InvalidOperationException ||
				ex is NotSupportedException ||
				ex is NotImplementedException ||
				ex is UnauthorizedAccessException)
			{
				return HttpStatusCode.Forbidden;
			}

			return HttpStatusCode.InternalServerError;
		}

		/// <summary>
		/// Ensures the exception is not null and unwraps standard wrapper exceptions
		/// </summary>
		/// <param name="ex"></param>
		/// <returns></returns>
		protected virtual Exception EnsureException(Exception ex)
		{
			// ensure and unwrap exception
			if (ex == null)
			{
				return new Exception("");
			}

			if ((ex.InnerException != null) &&
				(ex is TargetInvocationException || ex is HttpUnhandledException))
			{
				return ex.InnerException;
			}

			return ex;
		}

		#endregion Exception Methods
	}
}
