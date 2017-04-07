using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class JobListParams : ListParams
	{
		public string state;
		public string device;
	}

	public class JobDetails
	{
		public string id;
		public string state;
		public string output;
		public string errors;
		public string started;
		public string finished;
		public string created;
		public string updated;
	}

	public class JobList : ListResult
	{
		public JobDetails[] jobs;
	}
}
