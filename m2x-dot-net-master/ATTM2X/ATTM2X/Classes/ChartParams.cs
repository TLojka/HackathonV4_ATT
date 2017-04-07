using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class ChartParams
	{
		public string name;
		public ChartSeries[] series;
	}

	public class ChartDetails : ChartParams
	{
		public string id;
		public string url;
		public ChartRender render;
	}

	public class ChartSeries
	{
		public string device;
		public string stream;
	}

	public class ChartRender
	{
		public string png;
		public string svg;
	}

	public class ChartList
	{
		public ChartDetails[] charts;
	}
}
