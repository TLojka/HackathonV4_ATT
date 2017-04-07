using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class StreamList
	{
		public StreamDetails[] streams;
	}

	public class StreamDetails
	{
		public string name;
		public string type;
		public string value;
		public string latest_value_at;
		public StreamUnit unit;
		public string url;
		public string created;
		public string updated;
	}

	public class StreamUnit
	{
		public string label;
		public string symbol;
	}

	[DataContract]
	public class StreamParams
	{
		[DataMember(EmitDefaultValue = false)]
		public string type;
		[DataMember]
		public StreamUnit unit;
	}

	public class StreamValuesFilter
	{
		public string start;
		public string end;
		public double? min;
		public double? max;
		public int? limit;
	}

	[DataContract]
	public class StreamValues
	{
		[DataMember(EmitDefaultValue = false)]
		public string start;
		[DataMember(EmitDefaultValue = false)]
		public string end;
		[DataMember(EmitDefaultValue = false)]
		public int? limit;
		[DataMember]
		public StreamValue[] values;
	}

	[DataContract]
	public class StreamValue
	{
		[DataMember(EmitDefaultValue = false)]
		public string timestamp;
		[DataMember]
		public string value;
	}

	public class StreamSamplingParams
	{
		public string type;
		public int interval;
		public string start;
		public string end;
		public string min;
		public string max;
		public int? limit;
	}

	public class StreamStatsParams
	{
		public string start;
		public string end;
		public string min;
		public string max;
	}

	public class StreamStatsInfo
	{
		public string start;
		public string end;
		public StreamStats stats;
	}

	public class StreamStats
	{
		public int count;
		public double min;
		public double max;
		public double avg;
		public double stddev;
	}

	public class DeleteValuesParams
	{
		public string from;
		public string end;
	}

	[DataContract]
	public class ValueCondition
	{
		[DataMember(EmitDefaultValue = false)]
		public double? gt;
		[DataMember(EmitDefaultValue = false)]
		public double? gte;
		[DataMember(EmitDefaultValue = false)]
		public double? lt;
		[DataMember(EmitDefaultValue = false)]
		public double? lte;
		[DataMember(EmitDefaultValue = false)]
		public double? eq;
	}
}
