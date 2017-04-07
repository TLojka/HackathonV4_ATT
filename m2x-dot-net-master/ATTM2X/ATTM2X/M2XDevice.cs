using System;
using System.Net;
using System.Threading.Tasks;

namespace ATTM2X
{

	/// <summary>
	/// Wrapper for AT&T M2X <a href="https://m2x.att.com/developer/documentation/v2/device">Device API</a>
	/// </summary>
	public sealed class M2XDevice : M2XClassWithMetadata
	{
		public const string UrlPath = "/devices";

		public readonly string DeviceId;
		public readonly string Serial;

		internal M2XDevice(M2XClient client, string deviceId, string serial)
			: base(client)
		{
			if (String.IsNullOrWhiteSpace(deviceId) && String.IsNullOrWhiteSpace(serial))
				throw new ArgumentException(String.Format("Invalid deviceId - {0}", deviceId));

			this.DeviceId = deviceId;
			this.Serial = serial;
		}

		internal override string BuildPath(string path)
		{
			return String.IsNullOrWhiteSpace(this.DeviceId)
				? String.Concat(M2XDevice.UrlPath, "/serial/", WebUtility.UrlEncode(this.Serial), path)
				: String.Concat(M2XDevice.UrlPath, "/", WebUtility.UrlEncode(this.DeviceId), path);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Read-Device-Location">Read Device Location</a> endpoint
		/// </summary>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Location()
		{
			return MakeRequest("/location");
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Update-Device-Location">Update Device Location</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters.</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> UpdateLocation(object parms)
		{
			return MakeRequest("/location", M2XClientMethod.PUT, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Read-Device-Location-History">Read Device Location History</a> endpoint
		/// </summary>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> LocationHistory()
		{
			return MakeRequest("/location/waypoints");
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Delete-Location-History">Delete Location History</a> endpoint
		/// </summary>
		/// <param name="from">timestamp in ISO 8601 format.</param>
		/// <param name="end">timestamp in ISO 8601 format.</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> DeleteLocationHistory(DateTime from, DateTime end)
		{
			//var deleteBodyParams = $"{{ \"from\": \"{from.ToString(M2XClient.DateTimeFormat)}\", \"end\": \"{end.ToString(M2XClient.DateTimeFormat)}\" }}";
			var fromValue = from.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
			var toValue = end.ToString("yyyy-MM-ddTHH:mm:ss.999Z");
			var deleteParams = new { from = fromValue, end = toValue };
			return MakeRequest("/location/waypoints", M2XClientMethod.DELETE, parms: deleteParams);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#List-Data-Streams">List Data Streams</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Streams(object parms = null)
		{
			return MakeRequest(M2XStream.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#View-Data-Stream">View Data Stream</a> endpoint
		/// </summary>
		/// <param name="streamName">Name of the stream to be retrieved</param>
		/// <returns>M2XStream - Data stream details</returns>
		public M2XStream Stream(string streamName)
		{
			return new M2XStream(this, streamName);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#List-Values-from-all-Data-Streams-of-a-Device">List Values from all Data Streams of a Device</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <param name="format">String.</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Values(object parms = null, string format = null)
		{
			string path = "/values";
			if (!String.IsNullOrEmpty(format))
				path += "." + format;
			return MakeRequest(path, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Search-Values-from-all-Data-Streams-of-a-Device">Search Values from all Data Streams of a Device</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <param name="format">String.</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> SearchValues(object parms, string format = null)
		{
			string path = "/values/search";
			if (!String.IsNullOrEmpty(format))
				path += "." + format;
			return MakeRequest(path, M2XClientMethod.POST, null, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Export-Values-from-all-Data-Streams-of-a-Device">Export Values from all Data Streams of a Device</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> ExportValues(object parms = null)
		{
			return MakeRequest("/values/export.csv", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Post-Device-Update--Single-Values-to-Multiple-Streams-">Post Device Update(Single Values to Multiple 	Streams)</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> PostUpdate(object parms)
		{
			return MakeRequest("/update", M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Post-Device-Updates--Multiple-Values-to-Multiple-Streams-">Post Device Updates(Multiple Values to Multiple Streams)</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> PostUpdates(object parms)
		{
			return MakeRequest("/updates", M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#View-Request-Log">View Request Log</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Log(object parms = null)
		{
			return MakeRequest("/log", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#Device-s-List-of-Received-Commands">Device's List of Received Commands</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Commands(object parms = null)
		{
			return MakeRequest("/commands", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#Device-s-View-of-Command-Details">Device's View of Command Details</a> endpoint
		/// </summary>
		/// <param name="commandId">Command ID of the device to be viewed</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CommandDetails(string commandId)
		{
			return MakeRequest("/commands/" + WebUtility.UrlEncode(commandId));
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#Device-Marks-a-Command-as-Processed">Device Marks a Command as Processed</a> endpoint
		/// </summary>
		/// <param name="commandId">Command ID of the device</param>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> ProcessCommand(string commandId, object parms = null)
		{
			return MakeRequest(String.Concat("/commands/", WebUtility.UrlEncode(commandId), "/process"), M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/commands#Device-Marks-a-Command-as-Rejected">Device Marks a Command as Rejected</a> endpoint
		/// </summary>
		/// <param name="commandId">Command ID of the device</param>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> RejectCommand(string commandId, object parms = null)
		{
			return MakeRequest(String.Concat("/commands/", WebUtility.UrlEncode(commandId), "/reject"), M2XClientMethod.POST, parms);
		}
	}
}
