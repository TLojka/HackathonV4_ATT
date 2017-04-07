using System;
using System.Net;
using System.Threading.Tasks;

namespace ATTM2X
{

	/// <summary>
	/// Wrapper for AT&T M2X <a href="https://m2x.att.com/developer/documentation/v2/distribution">Distribution API</a>
	/// </summary>
	public sealed class M2XDistribution : M2XClassWithMetadata
	{
		public const string UrlPath = "/distributions";

		public readonly string DistributionId;

		internal M2XDistribution(M2XClient client, string distributionId)
			: base(client)
		{
			if (String.IsNullOrWhiteSpace(distributionId))
				throw new ArgumentException(String.Format("Invalid distributionId - {0}", distributionId));

			this.DistributionId = distributionId;
		}

		internal override string BuildPath(string path)
		{
			return String.Concat(M2XDistribution.UrlPath, "/", WebUtility.UrlEncode(this.DistributionId), path);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#List-Devices-from-an-existing-Distribution">List Devices from an existing Distribution</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Devices(object parms = null)
		{
			return MakeRequest(M2XDevice.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#Add-Device-to-an-existing-Distribution">Add Device to an existing Distribution</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> AddDevice(object parms)
		{
			return MakeRequest(M2XDevice.UrlPath, M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#List-Data-Streams">List Data Streams</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Streams(object parms = null)
		{
			return MakeRequest(M2XStream.UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/distribution#View-Data-Stream">View Data Stream</a> endpoint
		/// </summary>
		/// <param name="streamName">Name of the stream to be retrieved</param>
		/// <returns>M2XStream - Data stream details</returns>
		public M2XStream Stream(string streamName)
		{
			return new M2XStream(this, streamName);
		}
	}
}
