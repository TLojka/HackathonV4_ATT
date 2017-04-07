using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	[DataContract]
	public class TriggerParams
	{
		[DataMember]
		public string name;
		[DataMember]
		public string stream;
		[DataMember]
		public string condition;
		[DataMember]
		public string value;
		[DataMember]
		public string callback_url;
		[DataMember(EmitDefaultValue = false)]
		public string status;
		[DataMember]
		public bool send_location;
	}

	[DataContract]
	public class TriggerDetails : TriggerParams
	{
		[DataMember]
		public string id;
		[DataMember]
		public string url;
		[DataMember]
		public string created;
		[DataMember]
		public string updated;
	}

	public class TriggerList
	{
		public TriggerDetails[] triggers;
	}

	[DataContract]
	public class TriggerTestParams
	{
		[DataMember]
		public string device_id;
		[DataMember]
		public string stream;
		[DataMember]
		public string trigger_name;
		[DataMember]
		public string trigger_description;
		[DataMember]
		public string condition;
		[DataMember]
		public double threshold;
		[DataMember]
		public string value;
		[DataMember]
		public string timestamp;
		[DataMember(EmitDefaultValue = false)]
		public LocationParams location;
	}
}
