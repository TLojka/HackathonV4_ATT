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
	public class CollectionApiSpecs
	{
		private static readonly string _masterKey = ConfigurationManager.AppSettings["ApiMasterKey"];

		private static string _testDeviceId { get; set; }

		private static Dictionary<string, Device> _devices { get; set; }
		private static string _testDeviceSerial { get; set; }

		#region " Initialize and Cleanup Methods "

		[ClassInitialize]
		public static void InitializeTestSpecs(TestContext testContext)
		{
			_devices = new Dictionary<string, Device>();
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
				var updateLocationParms = $"{{ \"name\": \"Test Device Location\", \"latitude\": {Constants.TestDeviceLatitude}, \"longitude\": {Constants.TestDeviceLongitude} }}";
				var resultLocation = testDevice.UpdateLocation(updateLocationParms).Result;
				var updateMetadataParms = "{ \"owner\": \"The Testing Guy\" } ";
				var resultMetadata = testDevice.UpdateMetadata(updateMetadataParms);

				var stream01UpdateParms = $"{{ \"values\": [ {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": 98.6 }}, {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": 98.7 }} ] }}";
				var stream01 = testDevice.Stream(Constants.TestStreamName001);
				var resultStream01Post = stream01.PostValues(stream01UpdateParms).Result;
				System.Threading.Thread.Sleep(500);

				var stream02UpdateParms = $"{{ \"values\": [ {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"normal\" }}, {{ \"timestamp\": \"{DateTime.Now.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")}\", \"value\": \"normal\" }} ] }}";
				var stream02 = testDevice.Stream(Constants.TestStreamName002);
				var resultStream02Post = stream02.PostValues(stream02UpdateParms).Result;
				System.Threading.Thread.Sleep(500);
			}
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
		public async Task CanList_CanAccess_ApiKey_Collections_List()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var result = await client.Collections();

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				ProcessCollectionsSearchResult(result.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanCreate_AndDelete_Collection_WithAll_RequiredParameters()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var collectionParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Collection {DateTime.UtcNow.Ticks}\" }}";
				var result = await client.CreateCollection(collectionParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var collection = JsonConvert.DeserializeObject<Collection>(result.Raw);
				Assert.IsNotNull(collection);

				var collectionWrapper = client.Collection(collection.id);
				Assert.IsNotNull(collectionWrapper);
				await collectionWrapper.Delete();

				var collectionCheck = client.Collection(collection.id);
				var collectionCheckResult = await collectionCheck.Details();
				Assert.IsNotNull(collectionCheck);
				Assert.IsTrue(collectionCheckResult.Error);
				Assert.IsFalse(collectionCheckResult.ServerError);
				Assert.IsNull(collectionCheckResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(collectionCheckResult.Raw));
				ProcessCollectionsSearchResult(collectionCheckResult.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanCreate_Collection_And_PrivateDevice_ThenAdd_DeviceToCollection_TheRemove_DeviceFromCollection_AndDelete_Both()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var collectionParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Collection {DateTime.UtcNow.Ticks}\" }}";
				var createCollectionResult = await client.CreateCollection(collectionParameters);

				Assert.IsNotNull(createCollectionResult);
				Assert.IsFalse(createCollectionResult.Error);
				Assert.IsFalse(createCollectionResult.ServerError);
				Assert.IsNull(createCollectionResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(createCollectionResult.Raw));

				var collection = JsonConvert.DeserializeObject<Collection>(createCollectionResult.Raw);
				Assert.IsNotNull(collection);

				var collectionWrapper = client.Collection(collection.id);
				Assert.IsNotNull(collectionWrapper);

				var deviceParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Device {DateTime.UtcNow.Ticks}\", \"visibility\": \"private\" }}";
				var result = await client.CreateDevice(deviceParameters);

				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

				var device = JsonConvert.DeserializeObject<Device>(result.Raw);
				Assert.IsNotNull(device);

				var addCollectionDeviceResult = await collectionWrapper.AddDevice(device.id);
				Assert.IsNotNull(addCollectionDeviceResult);
				Assert.IsTrue(string.IsNullOrWhiteSpace(addCollectionDeviceResult.Raw));


				var checkCollectionDevices0 = await collectionWrapper.Details();
				Assert.IsNotNull(checkCollectionDevices0);
				Assert.IsFalse(string.IsNullOrWhiteSpace(checkCollectionDevices0.Raw));
				var checkCollection0 = JsonConvert.DeserializeObject<Collection>(checkCollectionDevices0.Raw);
				Assert.IsNotNull(checkCollection0);
				Assert.IsTrue(checkCollection0.devices > 0);

				var removeCollectionDeviceResult = await collectionWrapper.RemoveDevice(device.id);
				Assert.IsNotNull(removeCollectionDeviceResult);
				Assert.IsTrue(string.IsNullOrWhiteSpace(removeCollectionDeviceResult.Raw));


				var checkCollectionDevices1 = await collectionWrapper.Details();
				Assert.IsNotNull(checkCollectionDevices1);
				Assert.IsFalse(string.IsNullOrWhiteSpace(checkCollectionDevices1.Raw));
				var checkCollection1 = JsonConvert.DeserializeObject<Collection>(checkCollectionDevices1.Raw);
				Assert.IsNotNull(checkCollection1);
				Assert.IsTrue(checkCollection1.devices == 0);

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

				await collectionWrapper.Delete();

				var collectionCheck = client.Collection(collection.id);
				var collectionCheckResult = await collectionCheck.Details();
				Assert.IsNotNull(collectionCheck);
				Assert.IsTrue(collectionCheckResult.Error);
				Assert.IsFalse(collectionCheckResult.ServerError);
				Assert.IsNull(collectionCheckResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(collectionCheckResult.Raw));
				ProcessCollectionsSearchResult(collectionCheckResult.Raw, false);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleCollection_ById_AndView_AllMetadata()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var collectionParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Collection {DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} }}";
				var createCollectionResult = await client.CreateCollection(collectionParameters);

				Assert.IsNotNull(createCollectionResult);
				Assert.IsFalse(createCollectionResult.Error);
				Assert.IsFalse(createCollectionResult.ServerError);
				Assert.IsNull(createCollectionResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(createCollectionResult.Raw));

				var collectionData = JsonConvert.DeserializeObject<Collection>(createCollectionResult.Raw);
				var collection = client.Collection(collectionData.id);

				var result = await collection.Metadata();
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);
				
				var deleteCollectionResult = await collection.Delete();
				RequestWasProcessedAndReturnedExpectedValue(deleteCollectionResult);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleCollection_ById_AndUpdate_CollectionMetadata()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var collectionParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Collection {DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} }}";
				var createCollectionResult = await client.CreateCollection(collectionParameters);

				Assert.IsNotNull(createCollectionResult);
				Assert.IsFalse(createCollectionResult.Error);
				Assert.IsFalse(createCollectionResult.ServerError);
				Assert.IsNull(createCollectionResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(createCollectionResult.Raw));

				var collectionData = JsonConvert.DeserializeObject<Collection>(createCollectionResult.Raw);
				var collection = client.Collection(collectionData.id);

				var updateMetaDataValue = "The man sitting next to the man...";
				var updateMetaDataParams = $"{{ \"owner\": \"{updateMetaDataValue}\" }}";
				var result = await collection.UpdateMetadata(updateMetaDataParams);
				RequestWasProcessedAndReturnedExpectedValue(result);

				var verifyUpdateResult = await collection.Metadata();
				Assert.IsNotNull(verifyUpdateResult);
				Assert.IsFalse(verifyUpdateResult.Error);
				Assert.IsFalse(verifyUpdateResult.ServerError);
				Assert.IsNull(verifyUpdateResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(verifyUpdateResult.Raw));
				Assert.IsTrue(verifyUpdateResult.Raw.Length > 6);
				Assert.IsTrue(verifyUpdateResult.Raw.ToLowerInvariant().Contains(updateMetaDataValue.ToLowerInvariant()));


				var deleteCollectionResult = await collection.Delete();
				RequestWasProcessedAndReturnedExpectedValue(deleteCollectionResult);
			}
		}
		
		[TestMethod]
		public async Task CanAccess_ApiKey_SingleCollection_ById_AndView_SinggleMetadataField()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var collectionParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Collection {DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} }}";
				var createCollectionResult = await client.CreateCollection(collectionParameters);

				Assert.IsNotNull(createCollectionResult);
				Assert.IsFalse(createCollectionResult.Error);
				Assert.IsFalse(createCollectionResult.ServerError);
				Assert.IsNull(createCollectionResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(createCollectionResult.Raw));

				var collectionData = JsonConvert.DeserializeObject<Collection>(createCollectionResult.Raw);
				var collection = client.Collection(collectionData.id);

				var result = await collection.MetadataField(Constants.TestMetadataDefaultFieldName);
				Assert.IsNotNull(result);
				Assert.IsFalse(result.Error);
				Assert.IsFalse(result.ServerError);
				Assert.IsNull(result.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
				Assert.IsTrue(result.Raw.Length > 6);

				var deleteCollectionResult = await collection.Delete();
				RequestWasProcessedAndReturnedExpectedValue(deleteCollectionResult);
			}
		}

		[TestMethod]
		public async Task CanAccess_ApiKey_SingleCollection_ById_AndUpdate_CollectionMetadata_SingleField()
		{
			using (var client = new M2XClient(_masterKey))
			{
				var collectionParameters = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Collection {DateTime.UtcNow.Ticks}\", \"metadata\": {{ \"{Constants.TestMetadataDefaultFieldName}\": \"{Constants.TestMetadataDefaultFieldValue}\" }} }}";
				var createCollectionResult = await client.CreateCollection(collectionParameters);

				Assert.IsNotNull(createCollectionResult);
				Assert.IsFalse(createCollectionResult.Error);
				Assert.IsFalse(createCollectionResult.ServerError);
				Assert.IsNull(createCollectionResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(createCollectionResult.Raw));

				var collectionData = JsonConvert.DeserializeObject<Collection>(createCollectionResult.Raw);
				var collection = client.Collection(collectionData.id);

				var updateMetaDataValue = "The man sitting next to the man...";
				var updateMetaDataParams = $"{{ \"value\": \"{updateMetaDataValue}\" }}";
				var result = await collection.UpdateMetadataField(Constants.TestMetadataDefaultFieldName, updateMetaDataParams);
				RequestWasProcessedAndReturnedExpectedValue(result);

				var verifyUpdateResult = await collection.Metadata();
				Assert.IsNotNull(verifyUpdateResult);
				Assert.IsFalse(verifyUpdateResult.Error);
				Assert.IsFalse(verifyUpdateResult.ServerError);
				Assert.IsNull(verifyUpdateResult.WebError);
				Assert.IsFalse(string.IsNullOrWhiteSpace(verifyUpdateResult.Raw));
				Assert.IsTrue(verifyUpdateResult.Raw.Length > 6);
				Assert.IsTrue(verifyUpdateResult.Raw.ToLowerInvariant().Contains(updateMetaDataValue.ToLowerInvariant()));


				var deleteCollectionResult = await collection.Delete();
				RequestWasProcessedAndReturnedExpectedValue(deleteCollectionResult);
			}
		}


		private void ProcessCollectionsSearchResult(string json, bool shouldHaveItems = true)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForCollectionSearch>(json);
			Assert.IsNotNull(resultValues);
			if (shouldHaveItems)
			{
				Assert.IsTrue(resultValues.collections.Any(), "If this is a location search, ensure that the bounding coordiates contain the CURRENT coordinates of the expected device!");
			}
			else
			{
				Assert.IsFalse(resultValues.collections.Any());
			}
			foreach (var collection in resultValues.collections)
			{
				Console.WriteLine($"Collection {collection.name} ({collection.id}) was found.");
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

		public class ApiResponseForCollectionSearch
		{
			public ApiResponseForCollectionSearch()
			{
				collections = new List<Collection>().ToArray();
			}

			public Collection[] collections { get; set; }
		}

		public class ApiResponseForDeviceSearch
		{
			public Device[] devices { get; set; }
			public int total { get; set; }
			public int pages { get; set; }
			public int limit { get; set; }
			public int current_page { get; set; }
		}

		public class Collection
		{
			public string id { get; set; }
			public string url { get; set; }
			public object parent { get; set; }
			public string name { get; set; }
			public object description { get; set; }
			public int devices { get; set; }
			public int collections { get; set; }
			public string[] tags { get; set; }
			public object metadata { get; set; }
			public string key { get; set; }
			public DateTime created { get; set; }
			public DateTime updated { get; set; }
		}

		public class Device
		{
			public string url { get; set; }
			public string name { get; set; }
			public string status { get; set; }
			public object serial { get; set; }
			public object[] tags { get; set; }
			public string visibility { get; set; }
			public object description { get; set; }
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

		#endregion " Response Classes 
	}
}