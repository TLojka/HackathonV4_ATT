using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class DeviceListParams : ListParams
	{
		public string dir;
		public string sort;
	}

	public class DeviceCatalogSearchParams : DeviceListParams
	{
		public string name;
		public string description;
		public string tags;
		public string serial;
	}

	[DataContract]
	public class DeviceSearchBodyParamsBase
	{
		[DataMember(EmitDefaultValue = false)]
		public LocationFilter location;
	}

	public class DeviceSearchParams : DeviceCatalogSearchParams
	{
		public string ids;
		public string status;
		public string visibility;
		public string modified_since;
		public string unmodified_since;
		public string collection;
	}

	public class DeviceList : ListResult
	{
		public DeviceDetails[] devices;
	}

	public class DeviceDetails
	{
		public string id;
		public string name;
		public string description;
		public string visibility;
		public string status;
		public string serial;
		public string[] tags;
		public string key;
		public string url;
		public LocationDetails location;
		public string created;
		public string updated;
	}

	public class LocationDetails : WaypointDetails
	{
		public string name;
		public WaypointDetails[] waypoints;
	}

	public class WaypointDetails
	{
		public string timestamp;
		public double? latitude;
		public double? longitude;
		public int? elevation;
	}

	[DataContract]
	public class DeviceParams
	{
		[DataMember]
		public string name;
		[DataMember(EmitDefaultValue = false)]
		public string description;
		[DataMember]
		public string visibility;
		[DataMember(EmitDefaultValue = false)]
		public string tags;
		[DataMember(EmitDefaultValue = false)]
		public string base_device;
		[DataMember(EmitDefaultValue = false)]
		public string collection;
		[DataMember(EmitDefaultValue = false)]
		public string serial;
	}

	[DataContract]
	public class LocationParams
	{
		[DataMember]
		public string name;
		[DataMember]
		public double latitude;
		[DataMember]
		public double longitude;
		[DataMember(EmitDefaultValue = false)]
		public int? elevation;
		[DataMember(EmitDefaultValue = false)]
		public string timestamp;
	}

	public class RequestList
	{
		public RequestDetails[] requests;
	}

	public class RequestDetails
	{
		public string timestamp;
		public int status;
		public string method;
		public string path;
	}

	[DataContract]
	public class LocationFilter
	{
		[DataMember(EmitDefaultValue = false)]
		public WithinCircleFilter within_circle;
		[DataMember(EmitDefaultValue = false)]
		public LocationPointParams[] within_polygon;
	}

	[DataContract]
	public class WithinCircleFilter
	{
		[DataMember]
		public LocationPointParams center;
		[DataMember]
		public RadiusParams radius;
	}

	[DataContract]
	public class LocationPointParams
	{
		[DataMember]
		public double latitude;
		[DataMember]
		public double longitude;
	}

	[DataContract]
	public class RadiusParams
	{
		[DataMember(EmitDefaultValue = false)]
		public int? mi;
		[DataMember(EmitDefaultValue = false)]
		public int? miles;
		[DataMember(EmitDefaultValue = false)]
		public int? km;
	}

	public class DeviceValuesFilter
	{
		public string start;
		public string end;
		public int? limit;
		public string streams;
	}

	[DataContract]
	public class DeviceValueList<TValue>
	{
		[DataMember]
		public string start;
		[DataMember]
		public string end;
		[DataMember]
		public int limit;
		[DataMember]
		public TValue[] values;
	}

	[DataContract]
	public class DeviceValuesSearchParams<TConditions>
	{
		[DataMember]
		public string start;
		[DataMember]
		public string end;
		[DataMember]
		public string[] streams;
		[DataMember]
		public TConditions conditions;
	}
}
