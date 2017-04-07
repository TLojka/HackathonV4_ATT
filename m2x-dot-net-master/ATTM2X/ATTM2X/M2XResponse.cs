using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ATTM2X
{
	/// <summary>
	/// Wrapper for AT&T M2X API response
	/// </summary>
	public sealed class M2XResponse
	{
		/// <summary>
		/// The URL of API call
		/// </summary>
		public readonly Uri RequestUri;
		/// <summary>
		/// The HTTP method of API call
		/// </summary>
		public readonly M2XClientMethod RequestMethod;
		/// <summary>
		/// The text of API call
		/// </summary>
		public readonly string RequestContent;

		/// <summary>
		/// The exception occured during API call
		/// </summary>
		public Exception WebError { get; internal set; }

		/// <summary>
		/// The status code of the response.
		/// </summary>
		public HttpStatusCode Status { get; internal set; }
		/// <summary>
		/// The headers included on the response.
		/// </summary>
		public HttpResponseHeaders Headers { get; internal set; }
		/// <summary>
		/// The content headers included on the response.
		/// </summary>
		public HttpContentHeaders ContentHeaders { get; internal set; }
		/// <summary>
		/// The raw response body.
		/// </summary>
		public string Raw { get; internal set; }

		/// <summary>
		/// The parsed response body.
		/// </summary>
		public T Json<T>() where T: class
		{
			if (String.IsNullOrWhiteSpace(this.Raw) || this.ContentHeaders == null)
				return null;
			var contentType = this.ContentHeaders.ContentType;
			if (contentType == null || contentType.MediaType != "application/json")
				return null;

			byte[] bytes = Encoding.UTF8.GetBytes(this.Raw);
			var serializer = new DataContractJsonSerializer(typeof(T));
			using (var stream = new MemoryStream(bytes))
			{
				return (T)serializer.ReadObject(stream);
			}
		}

		/// <summary>
		/// Whether Status is a success (status code 2xx)
		/// </summary>
		public bool Success
		{
			get { return (int)this.Status >= 200 && (int)this.Status < 300; }
		}
		/// <summary>
		/// Whether Status is one of 4xx
		/// </summary>
		public bool ClientError
		{
			get { return (int)this.Status >= 400 && (int)this.Status < 500; }
		}
		/// <summary>
		/// Whether Status is one of 5xx
		/// </summary>
		public bool ServerError
		{
			get { return (int)this.Status >= 500 && (int)this.Status < 600; }
		}
		/// <summary>
		/// Whether ClientError or ServerError is true
		/// </summary>
		public bool Error
		{
			get { return this.ClientError || this.ServerError; }
		}

		internal M2XResponse(Uri url, M2XClientMethod method, string content)
		{
			this.RequestUri = url;
			this.RequestMethod = method;
			this.RequestContent = content;
		}

		internal HttpContent GetContent()
		{
			return this.RequestContent == null ? null :
				new StringContent(this.RequestContent, Encoding.UTF8, "application/json");
		}

		internal async Task SetResponse(HttpResponseMessage responseMessage)
		{
			this.Status = responseMessage.StatusCode;
			this.Headers = responseMessage.Headers;
			if (responseMessage.Content != null)
			{
				this.ContentHeaders = responseMessage.Content.Headers;
				this.Raw = await responseMessage.Content.ReadAsStringAsync();
			}
		}
	}
}
