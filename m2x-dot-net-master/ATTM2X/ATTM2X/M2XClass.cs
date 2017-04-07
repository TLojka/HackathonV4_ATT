using System.Net;
using System.Threading.Tasks;

namespace ATTM2X
{
	public abstract class M2XClass
	{
		public M2XClient Client { get; private set; }

		internal M2XClass(M2XClient client)
		{
			this.Client = client;
		}

		internal abstract string BuildPath(string path);

		public Task<M2XResponse> MakeRequest(
			string path = null, M2XClientMethod method = M2XClientMethod.GET, object parms = null, object addBodyParms = null)
		{
			return this.Client.MakeRequest(BuildPath(path), method, parms, addBodyParms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#View-Device-Details">View Device Details</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#View-Distribution-Details">View Distribution Details</a>, <a href="https://m2x.att.com/developer/documentation/v2/keys#View-Key-Details">View Key Details</a>, <a href="https://m2x.att.com/developer/documentation/v2/device#View-Data-Stream">View Data Stream</a>(Device), <a href="https://m2x.att.com/developer/documentation/v2/distribution#View-Data-Stream">View Data Stream</a>(Distribution) endpoint
		/// </summary>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Details()
		{
			return MakeRequest();
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Update-Device-Details">Update Device Details</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#Update-Distribution-Details">Update Distribution Details</a>, <a href="https://m2x.att.com/developer/documentation/v2/keys#Update-Key">Update Key</a>, <a href="https://m2x.att.com/developer/documentation/v2/device#Create-Update-Data-Stream">Create/Update Data Stream</a>(Device), <a href="https://m2x.att.com/developer/documentation/v2/distribution#Create-Update-Data-Stream">Create/Update Data Stream</a>(Distribution) endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Update(object parms)
		{
			return MakeRequest(null, M2XClientMethod.PUT, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Delete-Device">Delete Device</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#Delete-Distribution">Delete Distribution</a>, <a href="https://m2x.att.com/developer/documentation/v2/keys#Delete-Key">Delete Key</a>, <a href="https://m2x.att.com/developer/documentation/v2/device#Delete-Data-Stream">Delete Data Stream</a>(Device), <a href="https://m2x.att.com/developer/documentation/v2/distribution#Delete-Data-Stream">Delete Data Stream</a>(Distribution) endpoint
		/// </summary>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public virtual Task<M2XResponse> Delete()
		{
			return MakeRequest(null, M2XClientMethod.DELETE);
		}
	}

	public abstract class M2XClassWithMetadata : M2XClass
	{
		internal M2XClassWithMetadata(M2XClient client)
			: base(client)
		{
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Read-Device-Metadata">Read Device Metadata</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#Read-Distribution-Metadata">Read Distribution Metadata</a>, <a href="https://m2x.att.com/developer/documentation/v2/collections#Read-Collection-Metadata">Read Collection Metadata</a> endpoint
		/// </summary>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> Metadata()
		{
			return MakeRequest("/metadata");
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Update-Device-Metadata">Update Device Metadata</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#Update-Distribution-Metadata">Update Distribution Metadata</a>, <a href="https://m2x.att.com/developer/documentation/v2/collections#Update-Collection-Metadata">Update Collection Metadata</a> endpoint
		/// </summary>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> UpdateMetadata(object parms)
		{
			return MakeRequest("/metadata", M2XClientMethod.PUT, parms);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Read-Device-Metadata-Field">Read Device Metadata Field</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#Read-Distribution-Metadata-Field">Read Distribution Metadata Field</a>, <a href="https://m2x.att.com/developer/documentation/v2/collections#Read-Collection-Metadata-Field">Read Collection Metadata Field</a> endpoint
		/// </summary>
		/// <param name="field">Metadata Field to be read</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> MetadataField(string field)
		{
			return MakeRequest("/metadata/" + WebUtility.UrlEncode(field));
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/device#Update-Device-Metadata-Field">Update Device Metadata Field</a>, <a href="https://m2x.att.com/developer/documentation/v2/distribution#Update-Distribution-Metadata-Field">Update Distribution Metadata Field</a>, <a href="https://m2x.att.com/developer/documentation/v2/collections#Update-Collection-Metadata-Field">Update Collection Metadata Field</a> endpoint
		/// </summary>
		/// <param name="field">Metadata Field to be updated</param>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>M2XResponse - The API response, see M2X API docs for details</returns>
		public Task<M2XResponse> UpdateMetadataField(string field, object parms)
		{
			return MakeRequest("/metadata/" + WebUtility.UrlEncode(field), M2XClientMethod.PUT, parms);
		}
	}
}
