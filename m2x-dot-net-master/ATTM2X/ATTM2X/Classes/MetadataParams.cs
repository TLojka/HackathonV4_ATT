using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	[DataContract]
	public class MetadataFieldParams
	{
		[DataMember]
		public string value;
	}
}
