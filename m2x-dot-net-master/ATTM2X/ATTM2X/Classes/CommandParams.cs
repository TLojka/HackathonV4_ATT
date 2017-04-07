using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class CommandParams : ListParams
	{
		public string start;
		public string end;
		public string name;
		public string status;
	}

	public class CommandList<TDetails>
	{
		public TDetails[] commands;
	}

	public class CommandDetails
	{
		public string id;
		public string url;
		public string name;
		public string sent_at;
		public string status;
		public string received_at;
	}

	[DataContract]
	public class SendCommandParams
	{
		[DataMember]
		public string name;
		[DataMember]
		public CommandTargets targets;
	}

	[DataContract]
	public class SendCommandParams<TData> : SendCommandParams
	{
		[DataMember]
		public TData data;
	}

	[DataContract]
	public class CommandTargets
	{
		[DataMember]
		public string[] devices;
	}
}
