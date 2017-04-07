using ATTM2X;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Net;

namespace ATTM2X.Tests
{
	[TestClass]
	public class TestBase
	{
		protected static readonly string MasterKey = ConfigurationManager.AppSettings["ApiMasterKey"];

		protected String TestId = Guid.NewGuid().ToString("N");
		protected DateTime UtcNow = DateTime.UtcNow;

		protected M2XClient m2x = null;
		protected M2XResponse response = null;
		protected M2XDistribution distribution = null;
		protected M2XDevice device = null;
		protected M2XStream stream = null;
		protected M2XKey key = null;
		protected M2XCollection collection = null;

		[TestInitialize]
		public void Init()
		{
			this.TestId = Guid.NewGuid().ToString("N");
			this.UtcNow = DateTime.UtcNow;

			this.m2x = new M2XClient(MasterKey);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (this.key != null)
			{
				Delete(this.key);
				this.key = null;
			}
			if (this.stream != null)
			{
				Delete(this.stream);
				this.stream = null;
			}
			if (this.device != null)
			{
				Delete(this.device);
				this.device = null;
			}
			if (this.distribution != null)
			{
				Delete(this.distribution);
				this.distribution = null;
			}
			if (this.collection != null)
			{
				Delete(this.collection);
				this.collection = null;
			}
			if (this.m2x != null)
			{
				this.m2x.Dispose();
				this.m2x = null;
			}
		}
		protected void Delete(M2XClass entity)
		{
			response = entity.Delete().Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);
		}
	}
}
