using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class DistributionList
	{
		public DistributionDetails[] distributions;
	}

	public class DistributionDetails
	{
		public string id;
		public string name;
		public string description;
		public string visibility;
		public string serial;
		public string status;
		public string url;
		public string key;
		public string created;
		public string updated;
		public DistributionDevicesInfo devices;
	}

	public class DistributionDevicesInfo
	{
		public int total;
		public int registered;
		public int unregistered;
	}

	[DataContract]
	public class DistributionParams
	{
		[DataMember]
		public string name;
		[DataMember(EmitDefaultValue = false)]
		public string description;
		[DataMember]
		public string visibility;
		[DataMember(EmitDefaultValue = false)]
		public string base_device;
	}

	public class DistributionDeviceParams
	{
		public string serial;
	}
}
