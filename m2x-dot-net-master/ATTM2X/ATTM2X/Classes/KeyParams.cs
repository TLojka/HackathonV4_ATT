using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class KeyListParams
	{
		public string device;
		public string destribution;
		public string collection;
	}

	public class KeyList
	{
		public KeyDetails[] keys;
	}

	[DataContract]
	public class KeyDetails : KeyParams
	{
		[DataMember]
		public string key;
		[DataMember]
		public bool master;
		[DataMember]
		public bool expired;
	}

	[DataContract]
	public class KeyParams
	{
		[DataMember]
		public string name;
		[DataMember(EmitDefaultValue = false)]
		public string device;
		[DataMember(EmitDefaultValue = false)]
		public string stream;
		[DataMember(EmitDefaultValue = false)]
		public string expires_at;
		[DataMember(EmitDefaultValue = false)]
		public string origin;
		[DataMember(EmitDefaultValue = false)]
		public string device_access;
		[DataMember]
		public string[] permissions;
	}
}
