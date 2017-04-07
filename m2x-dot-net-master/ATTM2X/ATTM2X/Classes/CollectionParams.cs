using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class CollectionListParams
	{
		public string parent;
	}

	[DataContract]
	public class CollectionParams
	{
		[DataMember]
		public string name;
		[DataMember(EmitDefaultValue = false)]
		public string description;
		[DataMember(EmitDefaultValue = false)]
		public string parent;
		[DataMember(EmitDefaultValue = false)]
		public string tags;
	}

	public class CollectionDetails
	{
		public string id;
		public string parent;
		public string name;
		public string description;
		public int devices;
		public int collection;
		public string[] tags;
		public string key;
		public string created;
		public string updated;
	}

	public class CollectionList
	{
		public CollectionDetails[] collections;
	}
}
