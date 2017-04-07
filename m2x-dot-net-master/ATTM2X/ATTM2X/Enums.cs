
namespace ATTM2X
{
	public enum M2XClientMethod
	{
		GET,
		POST,
		PUT,
		DELETE,
	}

	public static class M2XDistanceUnit
	{
		public const string Mi = "mi";
		public const string Miles = "miles";
		public const string Km = "km";
	}

	public static class M2XVisibility
	{
		public const string Public = "public";
		public const string Private = "private";
	}

	public static class M2XStreamType
	{
		public const string Numeric = "numeric";
		public const string Alphanumeric = "alphanumeric";
	}

	public static class M2XSamplingType
	{
		public const string Nth = "nth";
		public const string Min = "min";
		public const string Max = "max";
		public const string Count = "count";
		public const string Avg = "avg";
		public const string Sum = "sum";
		public const string Stddev = "stddev";
	}

	public static class M2XStatus
	{
		public const string Enabled = "enabled";
		public const string Disabled = "disabled";
	}

	public static class M2XSortDirection
	{
		public const string Asc = "asc";
		public const string Desc = "desc";
	}

	public static class M2XDeviceSortOrder
	{
		public const string Created = "created";
		public const string Name = "name";
		public const string LastActivity = "last_activity";
	}

	public static class M2XJobState
	{
		public const string Queued = "queued";
		public const string Working = "working";
		public const string Complete = "complete";
		public const string Failed = "failed";
	}

	public static class M2XTimeFormat
	{
		public const string Seconds = "seconds";
		public const string Millis = "millis";
		public const string Iso8601 = "iso8601";
	}

	public static class M2XStreamOperator
	{
		public const string Greater = "gt";
		public const string GreaterEqual = "gte";
		public const string Less = "lt";
		public const string LessEqual = "lte";
		public const string Equal = "eq";
		public const string Match = "match";
	}

	public static class M2XStreamValuesFormat
	{
		public const string Json = "json";
		public const string Csv = "csv";
		public const string Msgpack = "msgpack";
	}

	public static class M2XCommandStatus
	{
		public const string Sent = "sent";
		public const string Processed = "processed";
		public const string Rejected = "rejected";
	}
}
