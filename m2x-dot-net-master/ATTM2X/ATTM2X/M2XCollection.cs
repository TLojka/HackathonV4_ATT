using System;
using System.Net;
using System.Threading.Tasks;

namespace ATTM2X
{

	/// <summary>
	/// Wrapper for AT&T M2X <a href="https://m2x.att.com/developer/documentation/v2/collections">Collections API</a>
	/// </summary>
	public sealed class M2XCollection : M2XClassWithMetadata
	{
		public const string UrlPath = "/collections";

		public readonly string CollectionId;

		internal M2XCollection(M2XClient client, string collectionId)
			: base(client)
		{
			if (string.IsNullOrWhiteSpace(collectionId))
				throw new ArgumentException(string.Format("Invalid collectionId - {0}", collectionId));

			this.CollectionId = collectionId;
		}

		internal override string BuildPath(string path)
		{
			var pathContainsId = !string.IsNullOrWhiteSpace(path) && path.Contains(UrlPath) && !string.IsNullOrWhiteSpace(CollectionId) && path.Contains(CollectionId);
			return string.Concat(pathContainsId ? string.Empty : $"{M2XCollection.UrlPath}/{WebUtility.UrlEncode(CollectionId)}", path);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#List-collections">List Collections</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Collections(object parms = null)
		{
			return MakeRequest(UrlPath, M2XClientMethod.GET, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#Create-Collection">Create Collection</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> CreateCollection(object parms)
		{
			return MakeRequest(UrlPath, M2XClientMethod.POST, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#Add-device-to-collection">Add device to collection</a> endpoint
		/// </summary>
		/// <param name="deviceId">Device ID to be added</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> AddDevice(string deviceId)
		{
			var path = BuildPath($"{M2XDevice.UrlPath}/{deviceId}");
			return MakeRequest(path, M2XClientMethod.PUT);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/collections#Remove-device-from-collection">Remove device from collection</a> endpoint
		/// </summary>
		/// <param name="deviceId">Device ID to be removed</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> RemoveDevice(string deviceId)
		{
			var path = BuildPath($"{M2XDevice.UrlPath}/{deviceId}");
			return MakeRequest(path, M2XClientMethod.DELETE);
		}
	}
}