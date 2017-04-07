using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ATTM2X
{

	/// <summary>
	/// Wrapper for AT&T <a href="https://m2x.att.com/developer/documentation/v2/overview">M2X API</a>
	/// </summary>

	/// \mainpage .NET toolkit for the <a href="https://m2x.att.com/developer/documentation/v2/overview">AT&T M2X API</a>
	///
	/// View the <a href="https://github.com/attm2x/m2x-dot-net/blob/master/README.md">M2X .NET Client README</a> for usage details
	///
	/// All methods in this client library require an API Key for authentication. There are multiple types of API Keys which provide granular access to your M2X resources. Please review the <a href="https://m2x.att.com/developer/documentation/v2/overview#API-Keys">API Keys documentation</a> for more details on the different types of keys available
	///
	/// If an invalid API Key is utilized, you will receive the HTTP Error 401 - Unauthorized.
	///

	public sealed class M2XClient : IDisposable
	{
		public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		public const string ApiEndPointSecure = "https://api-m2x.att.com/v2";
		public const string ApiEndPoint = "http://api-m2x.att.com/v2";

		private static readonly string UserAgent;

		public readonly string APIKey;
		public readonly string EndPoint;

		private HttpClient client = new HttpClient();

		private CancellationToken cancellationToken = CancellationToken.None;
		/// <summary>
		/// Gets or sets the cancellation token used in all requests
		/// </summary>
		public CancellationToken CancellationToken
		{
			get { return this.cancellationToken; }
			set { this.cancellationToken = value; }
		}

		private volatile M2XResponse lastResponse;
		/// <summary>
		/// The last API call response
		/// </summary>
		public M2XResponse LastResponse { get { return this.lastResponse; } }

		static M2XClient()
		{
			string version = typeof(M2XClient).GetTypeInfo().Assembly.GetName().Version.ToString();
			string langVersion = "4.5";//Environment.Version.ToString();
			string osVersion = "unknown";//Environment.OSVersion.ToString();
			UserAgent = String.Format("M2X-.NET/{0} .NETFramework/{1} ({2})", version, langVersion, osVersion);
		}

		public M2XClient(string apiKey, string m2xApiEndPoint = ApiEndPoint)
		{
			if (String.IsNullOrWhiteSpace(m2xApiEndPoint))
				throw new ArgumentException("Invalid API end point url");

			this.APIKey = apiKey;
			this.EndPoint = m2xApiEndPoint;

			client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
			if (!String.IsNullOrEmpty(this.APIKey))
				client.DefaultRequestHeaders.Add("X-M2X-KEY", this.APIKey);
		}

		public void Dispose()
		{
			if (this.client != null)
			{
				this.client.Dispose();
				this.client = null;
			}
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#List-Public-Devices-Catalog">List Public Devices Catalog</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> DeviceCatalog(object parms = null)
		{
			return MakeRequest(M2XDevice.UrlPath + "/catalog", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Search-Public-Devices-Catalog">Search Public Devices Catalog</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <param name="bodyParms">Query parameters passed as keyword arguments that will be added request as POST parameters. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> DeviceCatalogSearch(object parms = null, object bodyParms = null)
		{
			return MakeRequest(M2XDevice.UrlPath + "/catalog/search", bodyParms == null ? M2XClientMethod.GET : M2XClientMethod.POST, parms, bodyParms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#List-Devices">List Devices</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Devices(object parms = null)
		{
			return MakeRequest(M2XDevice.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Search-Devices">Search Devices</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <param name="bodyParms">Query parameters passed as keyword arguments that will be added request as POST parameters. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> SearchDevices(object parms = null, object bodyParms = null)
		{
			return MakeRequest(M2XDevice.UrlPath + "/search", bodyParms == null ? M2XClientMethod.GET : M2XClientMethod.POST, parms, bodyParms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#List-Device-Tags">List Devices Tags</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> DeviceTags(object parms = null)
		{
			return MakeRequest(M2XDevice.UrlPath + "/tags", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Create-Device">Create Device</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CreateDevice(object parms)
		{
			return MakeRequest(M2XDevice.UrlPath, M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#View-Device-Details">View Device Details</a> endpoint
		/// </summary>
		/// <param name="deviceId">Device ID to be retrieved</param>
		/// <param name="serial">Serial ID of the device</param>
		/// <returns>M2XDevice - The retrieved device details</returns>
		public M2XDevice Device(string deviceId, string serial = null)
		{
			return new M2XDevice(this, deviceId, serial);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#List-Distributions">List Distributions</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Distributions(object parms = null)
		{
			return MakeRequest(M2XDistribution.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#Create-Distribution">Create Distribution</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CreateDistribution(object parms)
		{
			return MakeRequest(M2XDistribution.UrlPath, M2XClientMethod.POST, parms);
		}
		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#View-Distribution-Details">View Distribution Details</a> endpoint
		/// </summary>
		/// <param name="distributionId">Distribution ID to be retrieved</param>
		/// <returns>M2XDistribution - The retrieved distribution details</returns>
		public M2XDistribution Distribution(string distributionId)
		{
			return new M2XDistribution(this, distributionId);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#Search-Distributions">Search Distributions</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> SearchDistributions(object parms = null)
		{
			return MakeRequest(M2XDistribution.UrlPath + "/search", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/keys#List-Keys">List Keys</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Keys(object parms = null)
		{
			return MakeRequest(M2XKey.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/keys#Create-Key">Create Key</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CreateKey(object parms)
		{
			return MakeRequest(M2XKey.UrlPath, M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/keys#View-Key-Details">View Key Details</a> endpoint
		/// </summary>
		/// <param name="key">Key associated with a developer account</param>
		/// <returns>M2XKey - The retrieved key details</returns>
		public M2XKey Key(string key)
		{
			return new M2XKey(this, key);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#List-collections">List collections</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Collections(object parms = null)
		{
			return MakeRequest(M2XCollection.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#Create-Collection">Create collection</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CreateCollection(object parms)
		{
			return MakeRequest(M2XCollection.UrlPath, M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#View-Collection-Details">View Collection Details</a> endpoint
		/// </summary>
		/// <param name="collectionId">Collection ID to be retrieved</param>
		/// <returns>M2XCollection - The retrieved collection details</returns>
		public M2XCollection Collection(string collectionId)
		{
			return new M2XCollection(this, collectionId);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/jobs#List-Jobs">List Jobs</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Jobs(object parms = null)
		{
			return MakeRequest("/jobs", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/jobs#View-Job-Details">View Job Details</a>, <a href="https://m2x.att.com/developer/documentation/v2/jobs#View-Job-Results">View Job Results</a> endpoint
		/// </summary>
		/// <param name="jobId">Job ID to be retrieved</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> JobDetails(string jobId)
		{
			return MakeRequest("/jobs/" + WebUtility.UrlEncode(jobId));
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/time">Time API</a> endpoint
		/// </summary>
		/// <param name="format">Time format to retrieve server time</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Time(string format = null)
		{
			string path = "/time";
			if (!String.IsNullOrEmpty(format))
				path += "/" + WebUtility.UrlEncode(format);
			return MakeRequest(path);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#List-Sent-Commands">List Sent Commands</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Commands(object parms = null)
		{
			return MakeRequest("/commands", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#Send-Command">Send Command</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> SendCommand(object parms)
		{
			return MakeRequest("/commands", M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#View-Command-Details">View Command Details</a> endpoint
		/// </summary>
		/// <param name="commandId">Command ID to be retrieved</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CommandDetails(string commandId)
		{
			return MakeRequest("/commands/" + WebUtility.UrlEncode(commandId));
		}

		/// <summary>
		/// Method for Formats a DateTime value to an ISO8601 timestamp.
		/// </summary>
		/// <param name="dateTime">DateTime.</param>
		/// <returns>string - timestamp in ISO 8601 format</returns>
		public static string DateTimeToString(DateTime dateTime)
		{
			return dateTime.ToString(DateTimeFormat);
		}

		/// <summary>
		/// Builds url to AT&T M2X API with optional query parameters
		/// </summary>
		/// <param name="path">AT&T M2X API url path</param>
		/// <param name="parms">parameters to build url query</param>
		/// <returns>string - Url to AT&T M2X API with optional query parameters</returns>
		public string BuildUrl(string path, object parms = null)
		{
			string fullUrl = this.EndPoint + path;
			if (parms != null)
			{
				string queryString = ObjectToQueryString(parms);
				if (!String.IsNullOrEmpty(queryString))
					fullUrl += "?" + queryString;
			}
			return fullUrl;
		}

		/// <summary>
		/// Performs async M2X API request
		/// </summary>
		/// <param name="path">API path</param>
		/// <param name="method">HTTP method</param>
		/// <param name="parms">Query string (for GET and DELETE) or body (for POST and PUT) parameters</param>
		/// <param name="addBodyParms">Additional body parameters, if specified, the parms will be treated as query parameters. The passed object will be serialized into a JSON string. In case of a string passed it will be treated as a valid JSON string.</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public async Task<M2XResponse> MakeRequest(
			string path, M2XClientMethod method = M2XClientMethod.GET, object parms = null, object addBodyParms = null)
		{
			M2XResponse result = CreateResponse(path, method, parms, addBodyParms);
			CancellationToken ct = this.cancellationToken;
			try
			{
				HttpResponseMessage responseMessage;
				switch (method)
				{
					case M2XClientMethod.POST:
						responseMessage = ct == CancellationToken.None
							? await this.client.PostAsync(result.RequestUri, result.GetContent())
							: await this.client.PostAsync(result.RequestUri, result.GetContent(), ct);
						break;
					case M2XClientMethod.PUT:
						responseMessage = ct == CancellationToken.None
							? await this.client.PutAsync(result.RequestUri, result.GetContent())
							: await this.client.PutAsync(result.RequestUri, result.GetContent(), ct);
						break;
					case M2XClientMethod.DELETE:
						responseMessage = ct == CancellationToken.None
							? await this.client.DeleteAsync(result.RequestUri)
							: await this.client.DeleteAsync(result.RequestUri, ct);
						break;
					default:
						responseMessage = ct == CancellationToken.None
							? await this.client.GetAsync(result.RequestUri)
							: await this.client.GetAsync(result.RequestUri, ct);
						break;
				}
				if (ct != CancellationToken.None)
					ct.ThrowIfCancellationRequested();
				await result.SetResponse(responseMessage);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				result.WebError = ex;
			}
			this.lastResponse = result;
			return result;
		}

		private M2XResponse CreateResponse(string path, M2XClientMethod method, object parms, object addBodyParms)
		{
			bool isGetOrDelete = method == M2XClientMethod.GET || method == M2XClientMethod.DELETE;
			string url = BuildUrl(path, isGetOrDelete || addBodyParms != null ? parms : null);
			string content = isGetOrDelete ? null : SerializeData(addBodyParms ?? parms);
			return new M2XResponse(new Uri(url), method, content);
		}

		public static string ObjectToQueryString(object queryParams)
		{
			StringBuilder sb = new StringBuilder();
			IEnumerable<FieldInfo> fields = queryParams.GetType().GetFields();
			foreach (var prop in fields)
			{
				if (prop.IsStatic || !prop.IsPublic || prop.FieldType.IsArray)
					continue;
				object value = prop.GetValue(queryParams);
				AppendQuery(sb, prop.Name, value);
			}
			IEnumerable<PropertyInfo> props = queryParams.GetType().GetProperties();
			foreach (var prop in props)
			{
				if (!prop.CanRead || prop.PropertyType.IsArray)
					continue;
				object value = prop.GetValue(queryParams, null);
				AppendQuery(sb, prop.Name, value);
			}
			return sb.ToString();
		}
		private static void AppendQuery(StringBuilder sb, string name, object value)
		{
			if (value == null)
				return;
			if (sb.Length > 0)
				sb.Append('&');
			sb.Append(name).Append('=').Append(WebUtility.UrlEncode(value.ToString()));
		}

		public static string SerializeData(object data)
		{
			if (data == null)
				return null;
			string result = data as string;
			if (result != null)
				return result;

			var serializer = new DataContractJsonSerializer(data.GetType());
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, data);
				stream.Position = 0;
				return new StreamReader(stream).ReadToEnd();
			}
		}
	}
}
