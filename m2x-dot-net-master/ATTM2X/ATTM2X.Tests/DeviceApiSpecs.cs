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
	[TestClass]
	public class DeviceApiSpecs
	{
		private static readonly string _masterKey = ConfigurationManager.AppSettings["ApiMasterKey"];

		private static readonly string _testDeviceTestStreamName = Constants.TestStreamName001;

		private static string _testDeviceId { get; set; }
		private static string _testLocationDeviceId { get { return _testDeviceId; } } // you can supply your own if you like.
		private static Dictionary<string, Device> _devices { get; set; }
		private static string _testDeviceSerial { get; set; }

		#region " Initialize and Cleanup Methods "

		[ClassInitialize]
		public static void InitializeTestSpecs(TestContext testContext)
		{
			GenerateTestDevices();
		}

		[ClassCleanup]
		public static void CleanupTestSpecs()
		{
			DestroyTestDevices();
		}

		[TestInitialize]
		public void InitializeIndividualTest()
		{
			Task.Delay(TimeSpan.FromMilliseconds(250));
			if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testLocationDeviceId))
			{
				Assert.Fail($"Test Device ID is now \"{_testDeviceId}\" and Test Location Device ID is now \"{_testLocationDeviceId}\"");
				CleanupTestSpecs();
			}
		}

		private static void GenerateTestDevices()
		{
			_devices = new Dictionary<string, Device>();
			_testDeviceSerial = $"td-{DateTime.UtcNow.Ticks}";

			using (var client = new M2XClient(_masterKey))
			{
				foreach (var basisDeviceId in new[] { "cd85543b1ba7299db205470ebb935117", "d781ab7460136af9db496c97172a6e6c" })
				{
					var testDeviceSerial = basisDeviceId != "d781ab7460136af9db496c97172a6e6c"
						 ? $"td-{DateTime.UtcNow.Ticks}"
						 : _testDeviceSerial;

					var createDeviceEnabledParms = $"{{ \"base_device\": \"{basisDeviceId}\", \"name\": \"{Constants.TestDeviceNamePrefix} {DateTime.UtcNow.Ticks}\", \"description\": \"{Constants.TestDeviceDescription}\", \"serial\": \"{testDeviceSerial}\", \"visibility\": \"private\" }}";
					var createDeviceResult = client.CreateDevice(createDeviceEnabledParms).Result;
					System.Threading.Thread.Sleep(500);
					var device = JsonConvert.DeserializeObject<Device>(createDeviceResult.Raw);

					if (basisDeviceId == "d781ab7460136af9db496c97172a6e6c")
					{
						_testDeviceId = device.id;
						_devices.Add("primary", device);
					}
					else if (basisDeviceId == "cd85543b1ba7299db205470ebb935117")
					{
						_devices.Add("disabled", device);
					}
					else
					{
						_devices.Add(basisDeviceId, device);
					}

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
					System.Threading.Thread.Sleep(200);

					var stream02UpdateParms = $"{{ \"values\": [ {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"normal\" }}, {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"normal\" }} ] }}";
					var stream02 = testDevice.Stream(Constants.TestStreamName002);
					var resultStream02Post = stream02.PostValues(stream02UpdateParms).Result;

					// give things time to settle...
					System.Threading.Thread.Sleep(250);
				}
			}
		}

		private static void DestroyTestDevices()
		{
			if (_devices != null && _devices.Any())
			{
				for (var i = _devices.Count(); i > 0; i--)
				{
					var _device = _devices.ElementAt(i - 1);
					using (var client = new M2XClient(_masterKey))
					{
						var device = client.Device(_device.Value.id);
						device.Delete();
						System.Threading.Thread.Sleep(500);
						_devices.Remove(_device.Key);
					}
				}
			}
		}

		#endregion " Initialize and Cleanup Methods "

		[TestMethod]
		public async Task CanAccess_DeviceCatalog()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var result = await client.DeviceCatalog();

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanSearch_DeviceCatalog_ByNameMatching()
		{
			if (string.IsNullOrWhiteSpace(Constants.TestDeviceNamePrefix))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceNameSearchStringToMatch key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				// note: this is partial of the name of a known public device. The one created above won't match because it is private.
				var searchParameters = new { name = "CB Test" };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanSearch_DeviceCatalog_ByDescriptionMatching()
		{
			using (var client = new M2XClient(_masterKey))
			{
				// note: this is the description of a known public device. The one created above won't match because it is private.
				var searchParameters = new { description = Constants.TestDeviceDescription };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanSearch_DeviceCatalog_ByTagMatching()
		{
			using (var client = new M2XClient(_masterKey))
			{
				// note: this is the tag of a known public device. The one created above won't match because it is private.
				var searchParameters = new { tags = "test only" };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanSearch_DeviceCatalog_BySerialMatching()
		{
			using (var client = new M2XClient(_masterKey))
			{
				// note: this is the serial of a known public device. The one created above won't match because it is private.
				var searchParameters = new { serial = "456notapplicable" };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanSearch_DeviceCatalog_ByStreamsMatching()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchPayloadParameters = $"{{ \"streams\": {{ \"xj2115\": {{ \"match\": \"normal\" }} }} }}";
				var result = await client.DeviceCatalogSearch(bodyParms: searchPayloadParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw, false);
			}
		}

		[TestMethod] // NOTE: This test must be updated with proper handling of bodyParms is implemented. The commented out portion is left for reference.
		public async Task CanSearch_DeviceCatalog_ByLocationMatching()
		{
			using (var client = new M2XClient(_masterKey))
			{
				//var searchPayloadParameters = new { location = new { within_circle = new { center = new { latitude = Constants.TestDeviceLatitude, longitude = Constants.TestDeviceLongitude }, radius = new { mi = 10 } } } };
				var searchPayloadParameters = $"{{ \"location\": {{ \"within_circle\": {{ \"center\": {{ \"latitude\": {Constants.TestDeviceLatitude}, \"longitude\": {Constants.TestDeviceLongitude} }}, \"radius\": {{ \"mi\": 10 }} }} }} }}";
				var result = await client.DeviceCatalogSearch(bodyParms: searchPayloadParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw, false);
			}
		}

		[TestMethod] // NOTE: This test must be updated with proper handling of bodyParms is implemented. The commented out portion is left for reference.
		public async Task CanSearch_DeviceCatalog_ByMetadataMatching()
		{
			using (var client = new M2XClient(_masterKey))
			{
				// note: this is the metadata of a known public device. The one created above won't match because it is private.
				var searchPayloadParameters = $"{{ \"metadata\": {{ \"owner\": {{ \"match\": \"The Testing Guy\" }} }} }}";
				var result = await client.DeviceCatalogSearch(bodyParms: searchPayloadParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanSearch_DeviceCatalog_ByStreamAndValues()
		{
			using (var client = new M2XClient(_masterKey))
			{
				// note: this is data in a stream of a known public device. The one created above won't match because it is private.
				var thresholdValueMin = 98.6;
				var thresholdValueMax = 99.0;
				var searchParameters = new { streams = new object[] { new { temp = new object[] { new { gt = thresholdValueMin }, new { lt = thresholdValueMax } } } } };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanList_PublicDevicesCatalog_ByPage()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { page = 2 };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanList_PublicDevicesCatalog_ByLimit()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { limit = 5 };
				var result = await client.DeviceCatalogSearch(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanList_CanAccess_ApiKey_DeviceList()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var result = await client.Devices();

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanList_CanAccess_ApiKey_DeviceList_ByLimit()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { limit = 2 };
				var result = await client.Devices(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanList_CanAccess_ApiKey_DeviceList_ByPage()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { page = 1 };
				var result = await client.Devices(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanList_CanAccess_ApiKey_DeviceList_ByPage_WithNoDevices()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { page = 2 };
				var result = await client.Devices(searchParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { ids = _testDeviceId };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ByName()
		{
			if (string.IsNullOrWhiteSpace(Constants.TestDeviceNamePrefix))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { name = Constants.TestDeviceNamePrefix };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ByDescription()
		{
			if (string.IsNullOrWhiteSpace(Constants.TestDeviceDescriptionSearchStringToMatch))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { description = Constants.TestDeviceDescriptionSearchStringToMatch };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByPage()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { page = 1 };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByPage_WithNoDevices()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { page = 2 };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByLimit()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { limit = 3 };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByTags()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = "{ \"tags\": \"test only\" }";
				var result = await client.SearchDevices(bodyParms: searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByStatus_ValueEnabled()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { status = "enabled" };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByStatus_ValueDisabled()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { status = "disabled" };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_BySerial()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { serial = $"{_testDeviceSerial}, 456notapplicable" };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByVisibility_ValuePrivate()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { visibility = "private" };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_ByVisibility_ValuePublic()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var searchParameters = new { visibility = "public" };
				var result = await client.SearchDevices(searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw);
			}
		}

		[TestMethod] // NOTE: This test must be updated with proper handling of bodyParms is implemented. The commented out portion is left for reference.
		public async Task CanAccess_ApiKey_AllDevices_ByStream()
		{
			using (var client = new M2XClient(_masterKey))
			{
				//var searchParameters = new DeviceValuesSearchParams<TestDeviceConditions> {
				//	streams = new[] { "BM" },
				//	conditions = new TestDeviceConditions { testdevicestream = new ValueCondition { match = "normal" } },
				//};

				var searchParameters = "{ \"streams\": { \"Temp\": { \"gt\": 50, \"lt\": 120 } } }";
				var result = await client.SearchDevices(bodyParms: searchParameters);

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceSearchResult(result.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_AllDevices_AllTags()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var result = await client.DeviceTags();

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessTagsSearchResult(result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_AllStreams()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var result = await device.Streams();

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_AllStreams_AllValues()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var result = await device.Values();

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_SingleStream_AllValues()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId and TestDeviceTestStreamName keys in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var stream = device.Stream(_testDeviceTestStreamName);
				var result = await stream.Values();

				var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_SingleStream_Values_WithTimestamps_Between_StartAnd_ExplicitEndValue()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId and TestDeviceTestStreamName key in the app.config file to execute this test.");
				return;
			}

			var tsBasis = DateTime.UtcNow;
			var thresholdValue = tsBasis.AddHours(4);
			var fromValue = tsBasis.ToString("yyyy-MM-ddTHH:mm:00.000Z");
			var toValue = thresholdValue.ToString("yyyy-MM-ddTHH:mm:59.999Z");
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var stream = device.Stream(_testDeviceTestStreamName);
				var valueParameters = new { from = fromValue, end = toValue };
				var result = await stream.Values(valueParameters);

				var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessDeviceStreamQueryResult(thresholdValue, result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_SingleStream_Stats()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId and TestDeviceTestStreamName keys in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var stream = device.Stream(_testDeviceTestStreamName);
				var result = await stream.Stats();

				var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				var stats = JsonConvert.DeserializeObject<StreamStats>(result.Raw);
				Assert.IsNotNull(stats);
				Assert.IsTrue(stats.end > default(DateTime));
				Assert.IsTrue(stats.stats.avg > default(float));
				Assert.IsTrue(stats.stats.count > default(float));
				Assert.IsTrue(stats.stats.max > default(float));
				Assert.IsTrue(stats.stats.min > default(float));
				Assert.IsTrue(stats.stats.stddev > default(float));
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_ViewLog()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var result = await device.Log();

				var probableErrorMessage = "Ensure that the Master key specified allows your IP address.";

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error, probableErrorMessage);
				Assert.IsFalse(result.ServerError, probableErrorMessage);
				Assert.IsNull(result.WebError, probableErrorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
			}
		}

		[TestMethod]
		public async Task CanCreate_AndDelete_Private_Device_WithAll_RequiredParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var deviceParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Device {DateTime.UtcNow.Ticks}\", \"visibility\": \"private\" }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var deviceWrapper = client.Device(device.id, device.serial != null ? device.serial.ToString() : null);
				Assert.IsNotNull(deviceWrapper);
				await deviceWrapper.Delete();

				var devicesCheck = await client.SearchDevices(new { ids = device.id });
				Assert.IsNotNull(devicesCheck);
				Assert.IsFalse(devicesCheck.Error);
				Assert.IsFalse(devicesCheck.ServerError);
				Assert.IsNull(devicesCheck.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(devicesCheck.Raw));
				ProcessDeviceSearchResult(devicesCheck.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanCreate_AndDelete_Public_Device_WithAll_RequiredParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var deviceParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Public Device {DateTime.UtcNow.Ticks}\", \"visibility\": \"public\" }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var deviceWrapper = client.Device(device.id, device.serial != null ? device.serial.ToString() : null);
				Assert.IsNotNull(deviceWrapper);
				await deviceWrapper.Delete();

				var devicesCheck = await client.SearchDevices(new { ids = device.id });
				Assert.IsNotNull(devicesCheck);
				Assert.IsFalse(devicesCheck.Error);
				Assert.IsFalse(devicesCheck.ServerError);
				Assert.IsNull(devicesCheck.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(devicesCheck.Raw));
				ProcessDeviceSearchResult(devicesCheck.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanCreate_AndDelete_Private_Device_WithAll_AllowedLocalParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var deviceParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Device {DateTime.UtcNow.Ticks}\", \"visibility\": \"private\", \"description\": \"This is just a test device\", \"tags\": \"tag 1, tag 2, tag3\", \"serial\": \"test-{DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"owner\": \"SkyNet\", \"favorite_player\": \"Stephen C.\" }} }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var deviceWrapper = client.Device(device.id, device.serial != null ? device.serial.ToString() : null);
				Assert.IsNotNull(deviceWrapper);
				await deviceWrapper.Delete();

				var devicesCheck = await client.SearchDevices(new { ids = device.id });
				Assert.IsNotNull(devicesCheck);
				Assert.IsFalse(devicesCheck.Error);
				Assert.IsFalse(devicesCheck.ServerError);
				Assert.IsNull(devicesCheck.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(devicesCheck.Raw));
				ProcessDeviceSearchResult(devicesCheck.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanDuplicate_AndDelete_Device_WithNo_AdditionalParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var deviceParameters = $"{{ \"base_device\": \"cd85543b1ba7299db205470ebb935117\" }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var deviceWrapper = client.Device(device.id, device.serial != null ? device.serial.ToString() : null);
				Assert.IsNotNull(deviceWrapper);
				await deviceWrapper.Delete();

				var devicesCheck = await client.SearchDevices(new { ids = device.id });
				Assert.IsNotNull(devicesCheck);
				Assert.IsFalse(devicesCheck.Error);
				Assert.IsFalse(devicesCheck.ServerError);
				Assert.IsNull(devicesCheck.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(devicesCheck.Raw));
				ProcessDeviceSearchResult(devicesCheck.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanDuplicate_AndDelete_Device_With_SubstitutionParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var deviceParameters = $"{{ \"base_device\": \"cd85543b1ba7299db205470ebb935117\", \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Device {DateTime.UtcNow.Ticks}\", \"visibility\": \"private\", \"description\": \"This is just a test device\", \"tags\": \"tag 1, tag 2, tag3\", \"serial\": \"test-{DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"owner\": \"SkyNet\", \"favorite_player\": \"Stephen C.\" }} }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var deviceWrapper = client.Device(device.id, device.serial != null ? device.serial.ToString() : null);
				Assert.IsNotNull(deviceWrapper);
				await deviceWrapper.Delete();

				var devicesCheck = await client.SearchDevices(new { ids = device.id });
				Assert.IsNotNull(devicesCheck);
				Assert.IsFalse(devicesCheck.Error);
				Assert.IsFalse(devicesCheck.ServerError);
				Assert.IsNull(devicesCheck.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(devicesCheck.Raw));
				ProcessDeviceSearchResult(devicesCheck.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanDuplicate_AndUpdate_AndDelete_Device_With_SubstitutionParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var deviceParameters = $"{{ \"base_device\": \"cd85543b1ba7299db205470ebb935117\", \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Device {DateTime.UtcNow.Ticks}\" }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var deviceWrapper = client.Device(device.id, device.serial != null ? device.serial.ToString() : null);
				Assert.IsNotNull(deviceWrapper);

				var updateParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** This Device has been updated at {DateTime.Now}\", \"visibility\": \"private\", \"description\": \"Blah blah blah test device\", \"tags\": \"tag 4, tag 5, tag6\", \"serial\": \"test-{DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"owner\": \"The Justice League\", \"favorite_player\": \"Labron J.\" }} }}";
				var updateResult = await deviceWrapper.Update(updateParameters);
				Assert.IsNotNull(updateResult);
				Assert.IsFalse(updateResult.Error);
				Assert.IsNull(updateResult.WebError);
				Assert.IsNotNull(updateResult.Status);
				Assert.AreEqual(System.Net.HttpStatusCode.NoContent, updateResult.Status);

				await deviceWrapper.Delete();

				var devicesCheck = await client.SearchDevices(new { ids = device.id });
				Assert.IsNotNull(devicesCheck);
				Assert.IsFalse(devicesCheck.Error);
				Assert.IsFalse(devicesCheck.ServerError);
				Assert.IsNull(devicesCheck.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(devicesCheck.Raw));
				ProcessDeviceSearchResult(devicesCheck.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndView_DeviceDetails()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var result = await device.Details();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				var deviceDetails = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(deviceDetails);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_BySerial_AndView_DeviceDetails()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(null, _devices.Count() > 1 ? _devices.Last().Value.serial : _devices["primary"].serial);
				var result = await device.Details();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				var deviceDetails = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(deviceDetails);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndView_LocationInformation()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var result = await device.Location();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				var deviceLocation = JsonConvert.DeserializeObject<Location>(result.Raw);
				Assert.IsNotNull(deviceLocation);
				Assert.IsTrue(deviceLocation.latitude != 0);
				Assert.IsTrue(deviceLocation.longitude != 0);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndView_LocationHistory()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var result = await device.LocationHistory();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				var deviceLocationHistory = JsonConvert.DeserializeObject<LocationHistory>(result.Raw);
				Assert.IsNotNull(deviceLocationHistory);
				Assert.IsNotNull(deviceLocationHistory.waypoints, "Please add at least one location entry to your device.");
				Assert.IsTrue(deviceLocationHistory.waypoints.All(a => a.latitude != 0));
				Assert.IsTrue(deviceLocationHistory.waypoints.All(a => a.longitude != 0));

				var locationUpdateCount = 10;
				var basis = DateTime.Now.AddYears(-5);
				var startTime = new DateTime(basis.Year, basis.Month, basis.Day, basis.Hour, basis.Minute, 0);
				DateTime endTime = default(DateTime);
				for (var i = 0; i < locationUpdateCount; i++)
				{
					endTime = startTime.AddHours(i);
					var updateLocationParms = $"{{ \"name\": \"Test Device Location{i + 1}\", \"latitude\": {(Constants.TestDeviceLatitude + i)}, \"longitude\": {(Constants.TestDeviceLongitude + i)}, \"timestamp\": \"{endTime.ToString("yyyy-MM-ddTHH:mm:00.000Z")}\" }}";
					var resultLocation = await device.UpdateLocation(updateLocationParms);
					Assert.IsNotNull(resultLocation);
				}
				await Task.Delay(3000); // required to let server processing catch up.

				var resultPostUpdate = await device.LocationHistory();
				var deviceLocationHistoryPostUpdate = JsonConvert.DeserializeObject<LocationHistory>(resultPostUpdate.Raw);
				Assert.IsNotNull(deviceLocationHistoryPostUpdate);
				Assert.IsTrue(deviceLocationHistoryPostUpdate.waypoints.Count() > deviceLocationHistory.waypoints.Count());

				var deleteResult = await device.DeleteLocationHistory(startTime, endTime);
				Assert.IsNotNull(deleteResult);
				Assert.IsTrue(deleteResult.Headers.ToString().Contains("Accept"));

				// due to the variability of message processing timing in the API, the confidence with the remainder of this test is 100% only while debugging.
				// hence, it is commented out here but left in for those developers who wish to ensure that their delete code is working properly.

				/*
				
				await Task.Delay(5000); // required to let server processing catch up.

				var deviceLocationHistoryPostDeleteResult = await device.LocationHistory();
				Assert.IsNotNull(deviceLocationHistoryPostDeleteResult);
				var deviceLocationHistoryPostDelete = JsonConvert.DeserializeObject<LocationHistory>(deviceLocationHistoryPostDeleteResult.Raw);
				Assert.IsNotNull(deviceLocationHistoryPostDelete);
				Assert.IsNotNull(deviceLocationHistoryPostDelete.waypoints, "Please add at least one location entry to your device.");
				Assert.IsTrue(deviceLocationHistoryPostUpdate.waypoints.Count() > deviceLocationHistoryPostDelete.waypoints.Count());
				Assert.IsTrue(deviceLocationHistoryPostDelete.waypoints.Count() >= deviceLocationHistory.waypoints.Count());

				*/
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndUpdate_Location()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var updateParms = "{ \"name\": \"Test Update Name\", \"latitude\": 28.375252, \"longitude\": -81.549370, \"elevation\": 100.52, \"timestamp\": \"2016-05-20T15:03:32.006Z\" }";
				var result = await device.UpdateLocation(updateParms);
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.AreEqual("{\"status\":\"accepted\"}", result.Raw);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndView_AllMetadata()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var result = await device.Metadata();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndView_MetadataSingleField()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var result = await device.MetadataField("owner");
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Contains("\"value\":"));
				Assert.IsTrue(result.Raw.Length > 6);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndUpdate_AllMetadata()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var existingMetadata = await device.Metadata();

				var updateParams = $"{{ \"{Constants.TestMetadataDefaultFieldName}\": \"Somebody Else\"}}";
				await device.UpdateMetadata(updateParams);

				var result = await device.Metadata();

				// reset value for other tests
				var resetParams = $"{{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\"}}";
				await device.UpdateMetadata(resetParams);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
				Assert.AreNotEqual(existingMetadata.Raw, result.Raw);
				Assert.IsTrue(result.Raw.ToLowerInvariant().Contains(Constants.TestMetadataDefaultFieldName.ToLowerInvariant()));
				Assert.IsTrue(result.Raw.ToLowerInvariant().Contains("somebody else"));
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndUpdate_MetadataSingleField()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var existingMetadata = await device.Metadata();

				var updateParams = "{ \"value\": \"Somebody Else\"}";
				await device.UpdateMetadataField("owner", updateParams);

				var result = await device.Metadata();

				// reset value for other tests
				var resetParams = $"{{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\"}}";
				await device.UpdateMetadata(resetParams);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
				Assert.AreNotEqual(existingMetadata.Raw, result.Raw);
				Assert.IsTrue(result.Raw.ToLowerInvariant().Contains(Constants.TestMetadataDefaultFieldName.ToLowerInvariant()));
				Assert.IsTrue(result.Raw.ToLowerInvariant().Contains("somebody else"));
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndUpdate_ExistingStream()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testDeviceId);
				var stream = device.Stream(Constants.TestStreamName002);
				var existingDetails = await stream.Details();
				var existing = JsonConvert.DeserializeObject<Stream>(existingDetails.Raw);

				var updateParms = "{ \"display_name\": \"Not for polite company!\" }";
				var result = await stream.Update(updateParms);

				var checkDetails = await stream.Details();
				var check = JsonConvert.DeserializeObject<Stream>(checkDetails.Raw);

				// reset values for other tests
				var resetParms = $"{{ \"display_name\": \"{existing.display_name}\" }}";
				await stream.Update(resetParms);


				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsTrue(string.IsNullOrWhiteSpace(result.Raw));
				Assert.AreNotEqual(existing.display_name, check.display_name);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndCreate_NewSteam_ThenAddValues_ThenDelete_ThatStream()
		{
			var testStreamName = "TestStream01";
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var stream = device.Stream(testStreamName);
				var createParms = "{ \"display_name\": \"To be deleted!\", \"type\": \"alphanumeric\" }";
				var result = await stream.Update(createParms);
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var valueParms00 = $"{{ \"values\": [ {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"Eureka! It Works!\" }}, {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"Eureka! Thi one works too!\" }} ] }}";
				var resultValue00 = await stream.PostValues(valueParms00);
				var checkDetails = await stream.Details();
				Assert.IsNotNull(resultValue00);
				Assert.IsFalse(resultValue00.Error);
				Assert.IsFalse(resultValue00.ServerError);
				Assert.IsNull(resultValue00.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(resultValue00.Raw));
				Assert.IsTrue(resultValue00.Raw.Length > 6);
				Assert.IsFalse(string.IsNullOrWhiteSpace(checkDetails.Raw));

				var valueParms01 = $"{{ \"timestamp\": \"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"Eureka! It Works AGAIN!\" }}";
				var resultValue01 = await stream.UpdateValue(valueParms01);
				var checkDetailsValue01 = await stream.Details();
				Assert.IsNotNull(resultValue01);
				Assert.IsFalse(resultValue01.Error);
				Assert.IsFalse(resultValue01.ServerError);
				Assert.IsNull(resultValue01.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(resultValue01.Raw));
				Assert.IsTrue(resultValue01.Raw.Length > 6);
				Assert.IsFalse(string.IsNullOrWhiteSpace(checkDetailsValue01.Raw));

				var reset = await stream.Delete();
				Assert.IsNotNull(reset);
				Assert.IsFalse(reset.Error);
				Assert.IsFalse(reset.ServerError);
				Assert.IsNull(reset.WebError);
				Assert.IsTrue(string.IsNullOrWhiteSpace(reset.Raw));
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_Export_AllData()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var result = await device.ExportValues();

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndCreate_NewSteam_ThenAddValues_ThenDelete_SomeStreamValues_ThenDelete_ThatStream()
		{
			var testStreamName = "testnumeric999";
			DateTime tsBasis = DateTime.UtcNow.AddMinutes(-10);
			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var stream = device.Stream(testStreamName);
				var createParms = "{ \"display_name\": \"To be deleted!\", \"type\": \"numeric\" }";
				var result = await stream.Update(createParms);
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
			}

			var values = new StringBuilder();
			for (var i = 0; i < 10; i++)
			{
				var ts = tsBasis.AddMinutes(i);
				if (values.Length > 0) { values.Append(", "); }
				values.Append($"{{ \"timestamp\": \"{ts.ToString("yyyy-MM-ddTHH:mm:00Z")}\", \"value\": {i} }}");
			}
			var postParam = $"{{ \"values\": [ {values.ToString()} ] }}";

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var stream = device.Stream(testStreamName);
				var valueParms00 = postParam;
				var resultValue00 = await stream.PostValues(valueParms00);
				var checkDetails = await stream.Details();
				Assert.IsNotNull(resultValue00);
				Assert.IsFalse(resultValue00.Error, resultValue00.Raw);
				Assert.IsFalse(resultValue00.ServerError);
				Assert.IsNull(resultValue00.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(resultValue00.Raw));
				Assert.IsTrue(resultValue00.Raw.Length > 6);
				Assert.IsFalse(string.IsNullOrWhiteSpace(checkDetails.Raw));
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var stream = device.Stream(testStreamName);
				var deleteFrom = tsBasis;
				var deleteParams = new { from = deleteFrom.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), end = deleteFrom.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss.999Z") };

				var deleteValuesResult = await stream.DeleteValues(deleteParams);
				var checkDetails = await stream.Details();
				Assert.IsNotNull(deleteValuesResult);
				Assert.IsFalse(deleteValuesResult.Error, deleteValuesResult.Raw);
				Assert.IsFalse(deleteValuesResult.ServerError);
				Assert.IsNull(deleteValuesResult.WebError);
				Assert.IsTrue(string.IsNullOrWhiteSpace(deleteValuesResult.Raw));
				Assert.IsFalse(string.IsNullOrWhiteSpace(checkDetails.Raw));
				Assert.IsTrue(checkDetails.Raw.Length > 6);
			}

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var stream = device.Stream(testStreamName);
				var reset = await stream.Delete();
				Assert.IsNotNull(reset);
				Assert.IsFalse(reset.Error);
				Assert.IsFalse(reset.ServerError);
				Assert.IsNull(reset.WebError);
				Assert.IsTrue(string.IsNullOrWhiteSpace(reset.Raw));
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndPost_SingleValues_ToMultiple_Streams()
		{
			DateTime tsBasis = DateTime.UtcNow.AddMinutes(-10);
			var testStreamName00 = "testnumeric900";
			var testStreamName01 = "testnumeric901";
			var streams = new[] { testStreamName00, testStreamName01 };
			foreach (var testStreamName in streams)
			{
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testLocationDeviceId);
					var stream = device.Stream(testStreamName);
					var createParms = $"{{ \"display_name\": \"{testStreamName}!\", \"type\": \"numeric\" }}";
					var result = await stream.Update(createParms);
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				}
			}

			var postParam = $"{{ \"timestamp\": \"{tsBasis.ToString("yyyy-MM-ddTHH:mm:00Z")}\", \"values\": {{ \"{testStreamName00}\": 44, \"{testStreamName01}\": 55 }} }}";

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var resultValue00 = await device.PostUpdate(postParam);
				Assert.IsNotNull(resultValue00);
				Assert.IsFalse(resultValue00.Error, resultValue00.Raw);
				Assert.IsFalse(resultValue00.ServerError);
				Assert.IsNull(resultValue00.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(resultValue00.Raw));
				Assert.IsTrue(resultValue00.Raw.Length > 6);
			}

			foreach (var testStreamName in streams)
			{
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testLocationDeviceId);
					var stream = device.Stream(testStreamName);
					var reset = await stream.Delete();
					Assert.IsNotNull(reset);
					Assert.IsFalse(reset.Error);
					Assert.IsFalse(reset.ServerError);
					Assert.IsNull(reset.WebError);
					Assert.IsTrue(string.IsNullOrWhiteSpace(reset.Raw));
				}
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleDevice_ById_AndPost_MultipleValues_ToMultiple_Streams()
		{
			DateTime tsBasis = DateTime.UtcNow.AddMinutes(-10);
			var testStreamName00 = "testnumeric900";
			var testStreamName01 = "testnumeric901";
			var streams = new[] { testStreamName00, testStreamName01 };
			var aggregateStreamsPayload = new StringBuilder();
			var payloadFormat = "{{ \"values\": {{ {0} }} }}";
			var streamPayloadFormat = "\"{0}\": [ {1} ]";
			var valuesPayloadFormat = "{{ \"timestamp\": \"{0}\", \"value\": {1} }}";
			foreach (var testStreamName in streams)
			{
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testLocationDeviceId);
					var stream = device.Stream(testStreamName);
					var createParms = $"{{ \"display_name\": \"{testStreamName}!\", \"type\": \"numeric\" }}";
					var result = await stream.Update(createParms);
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				}

				var rand = new Random((int)DateTime.UtcNow.Ticks);
				var tempValues = new StringBuilder();
				for (var i = 0; i < rand.Next(5, 20); i++)
				{
					if (tempValues.Length > 0) { tempValues.Append(", "); }
					tempValues.AppendFormat(valuesPayloadFormat, tsBasis.AddSeconds(rand.Next(0, 1000)).ToString("yyyy-MM-ddTHH:mm:00Z"), rand.Next(0, 1000));
				}
				if (aggregateStreamsPayload.Length > 0) { aggregateStreamsPayload.Append(", "); }
				aggregateStreamsPayload.AppendFormat(streamPayloadFormat, testStreamName, tempValues);
			}

			var postParam = string.Format(payloadFormat, aggregateStreamsPayload);

			using (var client = new M2XClient(_masterKey))
			{
				var device = client.Device(_testLocationDeviceId);
				var resultValue00 = await device.PostUpdates(postParam);
				Assert.IsNotNull(resultValue00);
				Assert.IsFalse(resultValue00.Error, resultValue00.Raw);
				Assert.IsFalse(resultValue00.ServerError);
				Assert.IsNull(resultValue00.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(resultValue00.Raw));
				Assert.IsTrue(resultValue00.Raw.Length > 6);
			}

			foreach (var testStreamName in streams)
			{
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testLocationDeviceId);
					var stream = device.Stream(testStreamName);
					var reset = await stream.Delete();
					Assert.IsNotNull(reset);
					Assert.IsFalse(reset.Error);
					Assert.IsFalse(reset.ServerError);
					Assert.IsNull(reset.WebError);
					Assert.IsTrue(string.IsNullOrWhiteSpace(reset.Raw));
				}
			}
		}

		[TestMethod]
		public async Task CanAccess_SingleDevice_ViewSampling_All()
		{
			if (string.IsNullOrWhiteSpace(_testDeviceId))
			{
				Assert.Inconclusive("The user has chosen not to use this test. Add a valid value for the TestDeviceId key in the app.config file to execute this test.");
				return;
			}

			foreach (var samplingType in new[] { "nth", "min", "max", "count", "avg", "sum", "stddev" })
			{
				var rand = new Random((int)DateTime.UtcNow.Ticks);
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDeviceId);
					var stream = device.Stream("Temp");
					var samplingParms = new { type = samplingType, interval = samplingType == "nth" ? 2 : rand.Next(1000, 86400) };
					var result = await stream.Sampling(samplingParms);

					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error, result.Raw);
					Assert.IsFalse(result.ServerError, result.Raw);
					Assert.IsNull(result.WebError, result.Raw);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				}
			}
		}

		private void ProcessDeviceSearchResult(string json, bool shouldHaveDevices = true)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForDeviceSearch>(json);
			Assert.IsNotNull(resultValues);
			if (shouldHaveDevices)
			{
				Assert.IsTrue(resultValues.devices.Any(), $"If this is a location search, ensure that the bounding coordiates contain the CURRENT coordinates of the expected device!{Environment.NewLine}{Environment.NewLine}If this is a disabled device search, please make sure tha that at least one device on your account is set to 'disabled'.");
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


		//[TestMethod]
		//public async Task CanAccess_SingleDevice_SingleStream_ValuesFilteredByRange()
		//{
		//	if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
		//	{
		//		Assert.IsTrue(true);
		//		return;
		//	}

		//	using (var client = new M2XClient(_masterKey))
		//	{
		//		var device = client.Device(_testDeviceId);
		//		var stream = device.Stream(_testDeviceTestStreamName);
		//		var filterPayloadParameters = "{{ \"temp\": {{ \"gt\": 98.3, \"lt\": 100 }} }}";
		//		var result = await stream.Values( filterPayloadParameters);

		//		var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

		//		Assert.IsNotNull(result);
		//		Assert.IsFalse(result.Error, probableErrorMessage);
		//		Assert.IsFalse(result.ServerError, probableErrorMessage);
		//		Assert.IsNull(result.WebError, probableErrorMessage);
		//		Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Raw));
		//	}
		//}

		//[TestMethod]
		//public async Task CanAccess_SingleDevice_SingleStream_ValuesFilteredBy_GreaterThan_ExplicitValue()
		//{
		//	if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
		//	{
		//		Assert.IsTrue(true);
		//		return;
		//	}

		//	using (var client = new M2XClient(_masterKey))
		//	{
		//		var device = client.Device(_testDeviceId);
		//		var stream = device.Stream(_testDeviceTestStreamName);
		//		var filterParameters = new { temp = new { gt = 98.6 } };
		//		var result = await stream.Values(filterParameters);

		//		var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

		//		Assert.IsNotNull(result);
		//		Assert.IsFalse(result.Error, probableErrorMessage);
		//		Assert.IsFalse(result.ServerError, probableErrorMessage);
		//		Assert.IsNull(result.WebError, probableErrorMessage);
		//		Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Raw));
		//	}
		//}

		//[TestMethod]
		//public async Task CanAccess_SingleDevice_SingleStream_ValuesFilteredBy_GreaterThanEqualTo_ExplicitValue()
		//{
		//	if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
		//	{
		//		Assert.IsTrue(true);
		//		return;
		//	}

		//	using (var client = new M2XClient(_masterKey))
		//	{
		//		var device = client.Device(_testDeviceId);
		//		var stream = device.Stream(_testDeviceTestStreamName);
		//		var filterParameters = new { temp = new { gte = 98.6 } };
		//		var result = await stream.Values(filterParameters);

		//		var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

		//		Assert.IsNotNull(result);
		//		Assert.IsFalse(result.Error, probableErrorMessage);
		//		Assert.IsFalse(result.ServerError, probableErrorMessage);
		//		Assert.IsNull(result.WebError, probableErrorMessage);
		//		Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Raw));
		//	}
		//}


		//[TestMethod]
		//public async Task CanAccess_SingleDevice_SingleStream_ValuesFilteredBy_LessThan_ExplicitValue()
		//{
		//	if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
		//	{
		//		Assert.IsTrue(true);
		//		return;
		//	}

		//	using (var client = new M2XClient(_masterKey))
		//	{
		//		var device = client.Device(_testDeviceId);
		//		var stream = device.Stream(_testDeviceTestStreamName);
		//		var thresholdValue = 98.6;
		//		var filterParameters = string.Format("{0} \"conditions\": {0} \"temp\": {0} \"lt\": {2} {1} {1} {1}", "{", "}", thresholdValue);
		//		var result = await stream.Values(filterParameters);

		//		var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

		//		Assert.IsNotNull(result);
		//		Assert.IsFalse(result.Error, probableErrorMessage);
		//		Assert.IsFalse(result.ServerError, probableErrorMessage);
		//		Assert.IsNull(result.WebError, probableErrorMessage);
		//		Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Raw));

		//		var resultValues = JsonConvert.DeserializeObject<ApiResponseForNumericStream>(result.Raw);
		//		Assert.IsNotNull(resultValues);
		//		Assert.IsTrue(resultValues.values.Any());
		//		foreach (var observation in resultValues.values)
		//		{
		//			var errorMessage = $"Threshold Value is {thresholdValue}; an observation of {observation.value} found in item with timestamp {observation.timestamp}.";
		//			Assert.IsTrue(observation.value <= thresholdValue, errorMessage);
		//		}
		//	}
		//}

		//[TestMethod]
		//public async Task CanAccess_SingleDevice_SingleStream_ValuesFilteredBy_LessThanEqualTo_ExplicitValue()
		//{
		//	if (string.IsNullOrWhiteSpace(_testDeviceId) || string.IsNullOrWhiteSpace(_testDeviceTestStreamName))
		//	{
		//		Assert.IsTrue(true);
		//		return;
		//	}

		//	using (var client = new M2XClient(_masterKey))
		//	{
		//		var device = client.Device(_testDeviceId);
		//		var stream = device.Stream(_testDeviceTestStreamName);
		//		var thresholdValue = 98.6;
		//		var filterParameters = new { temp = new { lte = thresholdValue } };
		//		var result = await stream.Values(filterParameters);

		//		var probableErrorMessage = "Ensure that the specified stream exists on the specified test device.";

		//		Assert.IsNotNull(result);
		//		Assert.IsFalse(result.Error, probableErrorMessage);
		//		Assert.IsFalse(result.ServerError, probableErrorMessage);
		//		Assert.IsNull(result.WebError, probableErrorMessage);
		//		Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Raw));

		//		var resultValues = JsonConvert.DeserializeObject<ApiResponseForNumericStream>(result.Raw);
		//		Assert.IsNotNull(resultValues);
		//		Assert.IsTrue(resultValues.values.Any());
		//		foreach (var observation in resultValues.values)
		//		{
		//			var errorMessage = $"Threshold Value is {thresholdValue}; an observation of {observation.value} found in item with timestamp {observation.timestamp}.";
		//			Assert.IsTrue(observation.value <= thresholdValue, errorMessage);
		//		}
		//	}
		//}

		#region " Response Classes "

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