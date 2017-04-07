using System;
using System.Net;
using System.Threading.Tasks;

namespace ATTM2X
{

	/// <summary>
	/// Wrapper for AT&T M2X <a href="https://m2x.att.com/developer/documentation/v2/device">Device API</a>
	/// </summary>
	public sealed class M2XStream : M2XClass
	{
		public const string UrlPath = "/streams";

		public readonly string StreamName;
		public readonly M2XDevice Device;
		public readonly M2XDistribution Distribution;

		private M2XStream(M2XClient client, string streamName)
			: base(client)
		{
			if (String.IsNullOrWhiteSpace(streamName))
				throw new ArgumentException(String.Format("Invalid streamName - {0}", streamName));

			this.StreamName = streamName;
		}
		internal M2XStream(M2XDevice device, string streamName)
			: this(device.Client, streamName)
		{
			this.Device = device;
		}
		internal M2XStream(M2XDistribution distribution, string streamName)
			: this(distribution.Client, streamName)
		{
			this.Distribution = distribution;
		}

		internal override string BuildPath(string path)
		{
			path = String.Concat(M2XStream.UrlPath, "/", WebUtility.UrlEncode(this.StreamName), path);
			return this.Device == null
				? this.Distribution.BuildPath(path)
				: this.Device.BuildPath(path);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Update-Data-Stream-Value">Update Data Stream Value</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> UpdateValue(object parms)
		{
			return MakeRequest("/value", M2XClientMethod.PUT, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#List-Data-Stream-Values">List Data Stream Values</a> endpoint
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
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Data-Stream-Sampling">Data Stream Sampling</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <param name="format">String.</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Sampling(object parms, string format = null)
		{
			string path = "/sampling";
			if (!String.IsNullOrEmpty(format))
				path += "." + format;
			return MakeRequest(path, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Data-Stream-Stats">Data Stream Stats</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Stats(object parms = null)
		{
			return MakeRequest("/stats", M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Post-Data-Stream-Values">Post Data Stream Values</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> PostValues(object parms)
		{
			return MakeRequest("/values", M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Delete-Data-Stream-Values">Delete Data Stream Values</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> DeleteValues(object parms)
		{
			return MakeRequest("/values", M2XClientMethod.DELETE, parms);
		}
	}
}