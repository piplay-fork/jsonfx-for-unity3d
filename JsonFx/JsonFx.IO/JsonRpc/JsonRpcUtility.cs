#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2009 Stephen M. McKamey

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.

\*---------------------------------------------------------------------------------*/
#endregion License

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using JsonFx.Json;

namespace JsonFx.JsonRpc
{
	/// <summary>
	/// Utility class for calling JSON-RPC services from C#
	/// </summary>
	public class JsonRpcUtility
	{
		#region Utility Methods

		/// <summary>
		/// Helper method for calling a JSON-RPC service from C#
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="service"></param>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static T CallService<T>(Uri service, string methodName, params object[] args)
		{
			CookieCollection cookies = null;
			return CallService<T>(service, methodName, args, ref cookies);
		}

		/// <summary>
		/// Helper method for calling a JSON-RPC service from C#
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="service"></param>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		/// <param name="cookies"></param>
		/// <returns></returns>
		public static T CallService<T>(Uri service, string methodName, object[] args, ref CookieCollection cookies)
		{
			return CallService<T>(service, methodName, args, ref cookies, false);
		}

		/// <summary>
		/// Helper method for calling a JSON-RPC service from C#
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="service"></param>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		/// <param name="cookies"></param>
		/// <param name="ignoreCertErrors"></param>
		/// <returns></returns>
		public static T CallService<T>(Uri service, string methodName, object[] args, ref CookieCollection cookies, bool ignoreCertErrors)
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(service);
			request.Method = "POST";
			request.AllowAutoRedirect = true;
			request.MediaType = "application/json";

			if (cookies != null)
			{
				request.CookieContainer.Add(cookies);
			}

			if (ignoreCertErrors)
			{
				// establish this to bypass cert errors
				ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
			}

			JsonRequest message = new JsonRequest();
			message.Method = methodName;
			message.PositionalParams = args;

			using (JsonWriter writer = new JsonWriter(request.GetRequestStream()))
			{
				writer.Write(message);
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			JsonResponse rpcResponse = new JsonReader(response.GetResponseStream()).Deserialize<JsonResponse>();
			if (rpcResponse.ErrorSpecified)
			{
				JsonServiceException ex = new JsonServiceException(rpcResponse.Error.Message);
				try
				{
					ex.Data["JSON-RPC"] = rpcResponse.Error;
				}
				catch (ArgumentException)
				{
					// TODO: fix "System.ArgumentException: Argument passed in is not serializable."
				}
				throw ex;
			}

			cookies = response.Cookies;

			return JsonReader.CoerceType<T>(rpcResponse.Result);
		}

		private static bool RemoteCertificateValidationCallback(
			object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		#endregion Utility Methods
	}
}
