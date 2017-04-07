using System;
using System.Runtime.Serialization;

namespace ATTM2X.Classes
{
	public class ListParams
	{
		public int? page;
		public int? limit;
	}

	public class ListResult
	{
		public int total;
		public int pages;
		public int limit;
		public int current_page;
	}
}
