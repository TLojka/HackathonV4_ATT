using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATTM2X.Tests
{
	/// <summary>
	/// REQUIRES A PRO ACCOUNT API KEY TO COMPLETE TESTS SUCCESSFULLY!
	/// If you don't have one, leave the "Ignore" decorator value on the class or these tests will fail.
	/// </summary>
	[TestClass]
	public class DistributionApiSpecs
	{
		private static readonly string _masterKey = ConfigurationManager.AppSettings["ApiMasterKey"];

		private static string _testDeviceId { get; set; }
		private static string _testLocationDeviceId { get { return _testDeviceId; } } // you can supply your own if you like.
		private static string _testDeviceSerial { get; set; }
		private static Dictionary<string, Device> _devices { get; set; }
		private static Dictionary<string, Distribution> _distributions { get; set; }

		private static bool _accountIsNotPro = false;

		#region " Initialize and Cleanup Methods "

		[ClassInitialize]
		public static void InitializeTestSpecs(TestContext testContext)
		{
			_devices = new Dictionary<string, Device>();
			_distributions = new Dictionary<string, Distribution>();
			_testDeviceSerial = $"td-{DateTime.UtcNow.Ticks}";

			using (var client = new M2XClient(_masterKey))
			{
				var createDeviceParms = $"{{ \"base_device\": \"d781ab7460136af9db496c97172a6e6c\", \"name\": \"{Constants.TestDeviceNamePrefix} {DateTime.UtcNow.Ticks}\", \"description\": \"{Constants.TestDeviceDescription}\", \"serial\": \"{_testDeviceSerial}\", \"visibility\": \"private\" }}";
				var createDeviceResult = client.CreateDevice(createDeviceParms).Result;
				var device = JsonConvert.DeserializeObject<Device>(createDeviceResult.Raw);
				_devices.Add("primary", device);
				_testDeviceId = device.id;

				var testDevice = client.Device(device.id);
				var updateTagsParms = "{ \"tags\": \"test only\" }";
				var resultTags = testDevice.Update(updateTagsParms).Result;
				for (var i = 0; i < 5; i++)
				{
					var updateLocationParms = $"{{ \"name\": \"Test Device Location{i + 1}\", \"latitude\": {(Constants.TestDeviceLatitude + i)}, \"longitude\": {(Constants.TestDeviceLongitude + i)} }}";
					var resultLocation = testDevice.UpdateLocation(updateLocationParms).Result;
				}
				var updateMetadataParms = $"{{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} ";
				var resultMetadata = testDevice.UpdateMetadata(updateMetadataParms);

				var stream01UpdateParms = $"{{ \"values\": [ {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": 98.6 }}, {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": 98.7 }} ] }}";
				var stream01 = testDevice.Stream(Constants.TestStreamName001);
				var resultStream01Post = stream01.PostValues(stream01UpdateParms).Result;
				System.Threading.Thread.Sleep(250);

				var stream02UpdateParms = $"{{ \"values\": [ {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"normal\" }}, {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"normal\" }} ] }}";
				var stream02 = testDevice.Stream(Constants.TestStreamName002);
				var resultStream02Post = stream02.PostValues(stream02UpdateParms).Result;
				System.Threading.Thread.Sleep(250);

				var testDistributionCreateParms = $"{{ \"name\": \"Test Distribution {DateTime.Now.Ticks}\", \"description\": \"This is just a test!\", \"visibility\": \"private\", \"base_device\": \"{_testDeviceId}\", \"metadata\": {{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} }}";
				var testDistributionCreateResult = client.CreateDistribution(testDistributionCreateParms).Result;
				_accountIsNotPro = testDistributionCreateResult.Error;

				if (_accountIsNotPro) { return; }

				var testDistribution = JsonConvert.DeserializeObject<Distribution>(testDistributionCreateResult.Raw);
				var distributionAddDeviceParms = $"{{ \"serial\": \"td-{(DateTime.UtcNow.Ticks + 50)}\" }}";
				var distributionDeviceData = client.Distribution(testDistribution.id).AddDevice(distributionAddDeviceParms).Result;
				var distributionDevice = JsonConvert.DeserializeObject<Device>(distributionDeviceData.Raw);
				_devices.Add(distributionDevice.serial, distributionDevice);
				_distributions.Add("primary", testDistribution);
				System.Threading.Thread.Sleep(250);
			}
		}

		[ClassCleanup]
		public static void CleanupTestSpecs()
		{
			DestroyTestResources();
		}

		[TestInitialize]
		public void InitializeIndividualTest()
		{
			Task.Delay(TimeSpan.FromMilliseconds(250));
		}

		private static void DestroyTestResources()
		{
			if ((_devices != null && _devices.Any()) || (_distributions != null && _distributions.Any()))
			{
				using (var client = new M2XClient(_masterKey))
				{
					if (_distributions != null && _distributions.Any())
					{
						for (var i = _distributions.Count(); i > 0; i--)
						{
							var _distribution = _distributions.ElementAt(i - 1);
							var distribution = client.Distribution(_distribution.Value.id);
							var result = distribution.Delete().Result;
							System.Threading.Thread.Sleep(250);
							_distributions.Remove(_distribution.Key);
						}
					}

					if (_devices != null && _devices.Any())
					{
						for (var i = _devices.Count(); i > 0; i--)
						{
							var _device = _devices.ElementAt(i - 1);
							var device = client.Device(_device.Value.id);
							var result = device.Delete().Result;
							System.Threading.Thread.Sleep(250);
							_devices.Remove(_device.Key);
						}
					}
				}
			}
		}

		#endregion " Initialize and Cleanup Methods "

		[TestMethod]
		public async Task CanAccess_ApiKey_Distributions()
		{
			if (_accountIsNotPro)
			{
				Assert.Inconclusive("The API key used for this test run is not enabled for Full Access. Please upgrade the account or use an API key that has the necessary permissions.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var result = await client.Distributions();

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDistributionSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDistribution_ById_AndView_AllMetadata()
		{
			if (_accountIsNotPro)
			{
				Assert.Inconclusive("The API key used for this test run is not enabled for Full Access. Please upgrade the account or use an API key that has the necessary permissions.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var distribution = client.Distribution(_distributions["primary"].id);

				var result = await distribution.Metadata();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDistribution_ById_AndUpdate_DistributionMetadata()
		{
			if (_accountIsNotPro)
			{
				Assert.Inconclusive("The API key used for this test run is not enabled for Full Access. Please upgrade the account or use an API key that has the necessary permissions.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var distributionName = $"*** PLEASE DELETE ME *** Test Auto Created Distribution {DateTime.UtcNow.Ticks}";
				var distributionParameters = $"{{ \"name\": \"{distributionName}\", \"visibility\": \"private\", \"metadata\": {{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} }}";
				var createDistributionResult = await client.CreateDistribution(distributionParameters);

				Assert.IsNotNull(createDistributionResult);
				Assert.IsFalse(createDistributionResult.Error);
				Assert.IsFalse(createDistributionResult.ServerError);
				Assert.IsNull(createDistributionResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(createDistributionResult.Raw));

				var distributionData = JsonConvert.DeserializeObject<Distribution>(createDistributionResult.Raw);
				var distribution = client.Distribution(distributionData.id);
				_distributions.Add(distributionName, distributionData);

				var updateMetaDataValue = "The man sitting next to the man...";
				var updateMetaDataParams = $"{{ \"owner\": \"{updateMetaDataValue}\" }}";
				var result = await distribution.UpdateMetadata(updateMetaDataParams);
				RequestWasProcessedAndReturnedExpectedValue(result);

				var verifyUpdateResult = await distribution.Metadata();
				Assert.IsNotNull(verifyUpdateResult);
				Assert.IsFalse(verifyUpdateResult.Error);
				Assert.IsFalse(verifyUpdateResult.ServerError);
				Assert.IsNull(verifyUpdateResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(verifyUpdateResult.Raw));
				Assert.IsTrue(verifyUpdateResult.Raw.Length > 6);
				Assert.IsTrue(verifyUpdateResult.Raw.ToLowerInvariant().Contains(updateMetaDataValue.ToLowerInvariant()));
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDistribution_ById_AndView_SingleMetadataField()
		{
			if (_accountIsNotPro)
			{
				Assert.Inconclusive("The API key used for this test run is not enabled for Full Access. Please upgrade the account or use an API key that has the necessary permissions.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var distribution = client.Distribution(_distributions["primary"].id);

				var result = await distribution.MetadataField(Constants.TestMetadataDefaultFieldName);
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDistribution_ById_AndUpdate_DistributionMetadata_SingleField()
		{
			if (_accountIsNotPro)
			{
				Assert.Inconclusive("The API key used for this test run is not enabled for Full Access. Please upgrade the account or use an API key that has the necessary permissions.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var distribution = client.Distribution(_distributions["primary"].id);

				var updateMetaDataValue = "The man sitting next to the man...";
				var updateMetaDataParams = $"{{ \"value\": \"{updateMetaDataValue}\" }}";
				var result = await distribution.UpdateMetadataField(Constants.TestMetadataDefaultFieldName, updateMetaDataParams);
				RequestWasProcessedAndReturnedExpectedValue(result);

				var verifyUpdateResult = await distribution.Metadata();
				Assert.IsNotNull(verifyUpdateResult);
				Assert.IsFalse(verifyUpdateResult.Error);
				Assert.IsFalse(verifyUpdateResult.ServerError);
				Assert.IsNull(verifyUpdateResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(verifyUpdateResult.Raw));
				Assert.IsTrue(verifyUpdateResult.Raw.Length > 6);
				Assert.IsTrue(verifyUpdateResult.Raw.ToLowerInvariant().Contains(updateMetaDataValue.ToLowerInvariant()));
			}
		}

		[TestMethod]
		public async Task CanSearch_ApiKey_Distributions_ByAllowed_GETParameters()
		{
			if (_accountIsNotPro)
			{
				Assert.Inconclusive("The API key used for this test run is not enabled for Full Access. Please upgrade the account or use an API key that has the necessary permissions.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				foreach (var searchType in new object[] { null, "q1", "q2", "page", "limit", "tags", "modified_since", "unmodified_since", "visibility" })
				{
					object searchParams = null;
					if (searchType != null)
					{
						switch (searchType.ToString())
						{
							case "q1":
								searchParams = new { q = Constants.TestDeviceNamePrefix };
								break;

							case "q2":
								searchParams = new { q = Constants.TestDeviceDescription };
								break;

							case "page":
								searchParams = new { page = 1 };
								break;

							case "limit":
								searchParams = new { limit = 2 };
								break;

							case "tags":
								searchParams = new { tags = "test only" };
								break;

							case "modified_since":
								searchParams = new { modified_since = $"{DateTime.Now.AddHours(-1).ToString(Constants.ISO8601_DateStartFormat)}" };
								break;

							case "unmodified_since":
								searchParams = new { unmodified_since = $"{DateTime.Now.AddHours(-1).ToString(Constants.ISO8601_DateStartFormat)}" };
								break;

							case "visibility":
								searchParams = new { visibility = "private" };
								break;
						}
					}

					var result = await client.SearchDistributions(searchParams);

					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					ProcessDistributionSearchResult(result.Raw, true);
				}
			}
		}

		private void ProcessDistributionSearchResult(string json, bool shouldHaveDistributions = true)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForDistributionSearch>(json);
			Assert.IsNotNull(resultValues);
			if (shouldHaveDistributions)
			{
				Assert.IsTrue(resultValues.distributions.Any(), "If this is a location search, ensure that the bounding coordiates contain the CURRENT coordinates of the expected device!");
			}
			else
			{
				Assert.IsFalse(resultValues.distributions.Any());
			}
			foreach (var distribution in resultValues.distributions)
			{
				Console.WriteLine($"Distribution {distribution.name} ({distribution.id}) was found.");
			}
		}

		private void ProcessDeviceSearchResult(string json, bool shouldHaveDevices = true)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForDeviceSearch>(json);
			Assert.IsNotNull(resultValues);
			if (shouldHaveDevices)
			{
				Assert.IsTrue(resultValues.devices.Any(), "If this is a location search, ensure that the bounding coordiates contain the CURRENT coordinates of the expected device!");
			}
			else
			{
				Assert.IsFalse(resultValues.devices.Any());
			}
			foreach (var device in resultValues.devices)
			{
				Console.WriteLine($"Device {device.name} ({device.id}) was found.");
			}
		}

		private void ProcessTagsSearchResult(string json, bool shouldHaveItems = true)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForTagsQuery>(json);
			Assert.IsNotNull(resultValues);
			if (shouldHaveItems)
			{
				Assert.IsNotNull(resultValues.tags);
			}
			else
			{
				Assert.IsNull(resultValues.tags);
			}
		}

		private void ProcessDeviceStreamQueryResult(DateTime thresholdValue, string json)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForNumericStream>(json);
			Assert.IsNotNull(resultValues);
			Assert.IsTrue(resultValues.values.Any());
			foreach (var observation in resultValues.values)
			{
				var errorMessage = $"Threshold Value is {thresholdValue}; an observation of {observation.value} found in item with timestamp {observation.timestamp}.";
				Assert.IsTrue(observation.timestamp <= thresholdValue, errorMessage);
			}
		}

		private void RequestWasProcessedAndReturnedExpectedValue(M2XResponse response, string expectedValue = "Accept")
		{
			Assert.IsNotNull(response);
			Assert.IsFalse(response.Error);
			Assert.IsFalse(response.ServerError);
			Assert.IsNull(response.WebError);
			Assert.IsTrue(string.IsNullOrWhiteSpace(response.Raw));
			Assert.IsNotNull(response.Headers);
			Assert.IsTrue(response.Headers.Any());
			Assert.IsNotNull(response.Headers.First().Value);
			Assert.IsTrue(response.Headers.First().Value.Any());
			Assert.AreEqual(expectedValue.ToLowerInvariant(), response.Headers.First().Value.First().ToLowerInvariant());
		}

		#region " Response Classes "

		public class ApiResponseForDistributionSearch
		{
			public Distribution[] distributions { get; set; }
			public int total { get; set; }
			public int pages { get; set; }
			public int current_page { get; set; }
		}

		public class ApiResponseForNumericStream
		{
			public int limit { get; set; }
			public DateTime end { get; set; }
			public Value[] values { get; set; }
		}

		public class ApiResponseForDeviceSearch
		{
			public Device[] devices { get; set; }
			public int total { get; set; }
			public int pages { get; set; }
			public int limit { get; set; }
			public int current_page { get; set; }
		}

		public class ApiResponseForTagsQuery
		{
			public object tags { get; set; }
		}

		public class DistributionDevices
		{
			public int total { get; set; }
			public int registered { get; set; }
			public int unregistered { get; set; }
		}

		public class Distribution
		{
			public string id { get; set; }
			public string name { get; set; }
			public string description { get; set; }
			public string visibility { get; set; }
			public string status { get; set; }
			public string url { get; set; }
			public string key { get; set; }
			public DateTime created { get; set; }
			public DateTime updated { get; set; }
			public DistributionDevices devices { get; set; }
		}

		public class Device
		{
			public string url { get; set; }
			public string name { get; set; }
			public string status { get; set; }
			public string serial { get; set; }
			public string[] tags { get; set; }
			public string visibility { get; set; }
			public string description { get; set; }
			public DateTime created { get; set; }
			public DateTime updated { get; set; }
			public DateTime last_activity { get; set; }
			public Location location { get; set; }
			public string id { get; set; }
			public Streams streams { get; set; }
			public object metadata { get; set; }
			public string key { get; set; }
			public Triggers triggers { get; set; }
		}

		public class Location
		{
			public string name { get; set; }
			public float latitude { get; set; }
			public float longitude { get; set; }
			public string elevation { get; set; }
			public DateTime timestamp { get; set; }
			public Waypoint[] waypoints { get; set; }
		}

		public class M2XTime
		{
			public int seconds { get; set; }
			public long millis { get; set; }
			public DateTime iso8601 { get; set; }
		}

		public class Stream
		{
			public string name { get; set; }
			public string display_name { get; set; }
			public string value { get; set; }
			public DateTime latest_value_at { get; set; }
			public string type { get; set; }
			public Unit unit { get; set; }
			public string url { get; set; }
			public DateTime created { get; set; }
			public DateTime updated { get; set; }
		}

		public class Streams
		{
			public int count { get; set; }
			public string url { get; set; }
		}

		public class StreamStats
		{
			public DateTime end { get; set; }
			public Stats stats { get; set; }
		}

		public class Stats
		{
			public float count { get; set; }
			public float min { get; set; }
			public float max { get; set; }
			public float avg { get; set; }
			public float stddev { get; set; }
		}

		public class Triggers
		{
			public int count { get; set; }
			public string url { get; set; }
		}

		public class Value
		{
			public DateTime timestamp { get; set; }
			public float value { get; set; }
		}

		public class Unit
		{
			public object label { get; set; }
			public object symbol { get; set; }
		}

		public class Waypoint
		{
			public string name { get; set; }
			public float latitude { get; set; }
			public float longitude { get; set; }
			public string elevation { get; set; }
			public DateTime timestamp { get; set; }
		}

		public class LocationHistory
		{
			public IEnumerable<Waypoint> waypoints { get; set; }
		}

		#endregion " Response Classes 
	}
}