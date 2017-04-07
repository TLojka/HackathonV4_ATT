using System;
using System.Net;

namespace ATTM2X
{

	/// <summary>
	/// Wrapper for AT&T M2X <a href="https://m2x.att.com/developer/documentation/v2/charts">Charts API</a>
	/// </summary>
	public sealed class M2XChart : M2XClass
	{
		public const string UrlPath = "/charts";

		public readonly string ChartId;

		internal M2XChart(M2XClient client, string chartId)
			: base(client)
		{
			if (String.IsNullOrWhiteSpace(chartId))
				throw new ArgumentException(String.Format("Invalid chartId - {0}", chartId));

			this.ChartId = chartId;
		}

		internal override string BuildPath(string path)
		{
			return String.Concat(M2XChart.UrlPath, "/", WebUtility.UrlEncode(this.ChartId), path);
		}

		/// <summary>
		/// Method for <a href="https://m2x.att.com/developer/documentation/v2/charts#Render-Chart">Render Chart</a> endpoint
		/// </summary>
		/// <param name="format">Supported formats are png and svg</param>
		/// <param name="parms">Query parameters passed as keyword arguments. View M2X API Docs for listing of available parameters</param>
		/// <returns>String - Response status and content type</returns>
		public string RenderUrl(string format, object parms = null)
		{
			return this.Client.BuildUrl(BuildPath("." + format), parms);
		}
	}
}
