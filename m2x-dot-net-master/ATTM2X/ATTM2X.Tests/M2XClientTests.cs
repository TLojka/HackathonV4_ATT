using ATTM2X;
using ATTM2X.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;

namespace ATTM2X.Tests
{
	[TestClass, Ignore] // todo: to be revisted once api has been revised.
	public class M2XClientTests : TestBase
	{
		[TestMethod]
		public void ErrorTest()
		{
			response = m2x.CreateDevice(new DeviceParams()).Result;
			Assert.AreEqual(422, (int)response.Status, response.Raw);
			var errorInfo = response.Json<ErrorInfo>();
			Assert.IsNotNull(errorInfo.message);
		}

		[TestMethod]
		public void DeviceAPITest()
		{
			// device

			response = m2x.DeviceTags().Result;
			Assert.IsNotNull(m2x.LastResponse);
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			Assert.IsTrue(response.Success);
			Assert.IsFalse(response.Error);
			Assert.IsNotNull(response.Raw);

			response = m2x.DeviceCatalog(new DeviceListParams { page = 1, limit = 10, sort = M2XDeviceSortOrder.Name, dir = M2XSortDirection.Asc }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var deviceList = response.Json<DeviceList>();
			Assert.IsNotNull(deviceList.devices);
			Assert.IsTrue(deviceList.devices.Length > 0);

			DeviceDetails deviceDetails = deviceList.devices.FirstOrDefault(d => d.location != null && d.location.latitude.HasValue && d.location.longitude.HasValue);
			Assert.IsNotNull(deviceDetails);
			response = m2x.DeviceCatalogSearch(null, new DeviceSearchBodyParamsBase
			{
				location = new LocationFilter
				{
					within_circle = new WithinCircleFilter
					{
						center = new LocationPointParams
						{
							latitude = deviceDetails.location.latitude.Value,
							longitude = deviceDetails.location.longitude.Value,
						},
						radius = new RadiusParams { km = 10 },
					},
				},
			}).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			deviceList = response.Json<DeviceList>();
			Assert.IsNotNull(deviceList.devices);
			// TODO: Assert.IsTrue(deviceList.devices.Length > 0);

			response = m2x.CreateDevice(new DeviceParams
				{
					name = "TestDevice-" + this.TestId,
					visibility = M2XVisibility.Private,
				}).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			deviceDetails = response.Json<DeviceDetails>();
			Assert.IsNotNull(deviceDetails.id);
			this.device = m2x.Device(deviceDetails.id);
			Assert.AreEqual(M2XVisibility.Private, deviceDetails.visibility);

			Thread.Sleep(1000);

			response = device.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			deviceDetails = response.Json<DeviceDetails>();
			Assert.AreEqual(M2XStatus.Enabled, deviceDetails.status);

			response = device.Update(new DeviceParams
				{
					name = "TestDevice-" + this.TestId,
					visibility = M2XVisibility.Private,
					description = "test",
				}).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);
			response = device.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			deviceDetails = response.Json<DeviceDetails>();
			Assert.AreEqual("test", deviceDetails.description);

			response = device.UpdateLocation(new LocationParams
				{
					name = "Test Location",
					latitude = 12,
					longitude = -34,
				}).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);

			Thread.Sleep(2000);

			response = device.Location().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var location = response.Json<LocationDetails>();
			Assert.AreEqual("Test Location", location.name);

			response = m2x.Devices(new { visibility = M2XVisibility.Private }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			deviceList = response.Json<DeviceList>();
			Assert.IsNotNull(deviceList.devices);
			Assert.IsTrue(deviceList.devices.Length > 0);

			response = m2x.SearchDevices(new DeviceSearchParams { visibility = M2XVisibility.Private }, new DeviceSearchBodyParamsBase
			{
				location = new LocationFilter
				{
					within_circle = new WithinCircleFilter
					{
						center = new LocationPointParams
						{
							latitude = location.latitude.Value,
							longitude = location.longitude.Value,
						},
						radius = new RadiusParams { km = 10 },
					},
				},
			}).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			deviceList = response.Json<DeviceList>();
			Assert.IsNotNull(deviceList.devices);
			Assert.IsTrue(deviceList.devices.Length > 0);

			response = device.Log().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var requestList = response.Json<RequestList>();
			Assert.IsNotNull(requestList.requests);
			Assert.IsTrue(requestList.requests.Length > 0);

			// stream

			this.stream = device.Stream("testdevicestream");
			response = stream.Update(new StreamParams
				{
					type = M2XStreamType.Numeric,
					unit = new StreamUnit { label = "points", symbol = "pt" },
				}).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);

			Thread.Sleep(1000);

			response = device.Streams().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var streamList = response.Json<StreamList>();
			Assert.IsNotNull(streamList.streams);
			Assert.AreEqual(1, streamList.streams.Length);
			Assert.AreEqual(stream.StreamName, streamList.streams[0].name);

			response = stream.UpdateValue(new StreamValue { value = "10" }).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);

			Thread.Sleep(1000);

			response = stream.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var streamDetails = response.Json<StreamDetails>();
			Assert.AreEqual("10", streamDetails.value);

			response = stream.Values(new StreamValuesFilter { start = M2XClient.DateTimeToString(this.UtcNow.AddHours(-1)) }, M2XStreamValuesFormat.Json).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var streamValues = response.Json<StreamValues>();
			Assert.IsNotNull(streamValues.values);
			Assert.AreEqual(1, streamValues.values.Length);

			response = stream.UpdateValue(new StreamValue { value = "20" }).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);

			Thread.Sleep(1500);

			response = stream.Sampling(new StreamSamplingParams { type = M2XSamplingType.Sum, interval = 100 }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			streamValues = response.Json<StreamValues>();
			Assert.IsNotNull(streamValues.values);
			Assert.AreEqual(1, streamValues.values.Length);
			Assert.AreEqual("30.0", streamValues.values[0].value);

			response = stream.Stats().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var streamStats = response.Json<StreamStatsInfo>();
			Assert.IsNotNull(streamStats.stats);
			Assert.AreEqual(2, streamStats.stats.count);
			Assert.AreEqual(15.0, streamStats.stats.avg);

			string from = M2XClient.DateTimeToString(this.UtcNow.AddMinutes(-2));
			string end = M2XClient.DateTimeToString(this.UtcNow.AddMinutes(-1));
			response = stream.PostValues(new StreamValues
			{
				values = new StreamValue[]
				{
					new StreamValue { timestamp = from, value = "1", },
					new StreamValue { timestamp = end, value = "2", },
				}
			}).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);

			response = stream.DeleteValues(new DeleteValuesParams { from = from, end = end }).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);

			// values

			response = device.PostUpdate(new TestDeviceValue
			{
				timestamp = M2XClient.DateTimeToString(this.UtcNow.AddMinutes(-3)),
				values = new TestDeviceStreamValue { testdevicestream = 3 },
			}).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);

			response = device.PostUpdates(new TestDeviceValues
			{
				values = new TestDeviceStreamValues
				{
					testdevicestream = new StreamValue[]
					{
						new StreamValue { timestamp = from, value = "1", },
						new StreamValue { timestamp = end, value = "2", },
					},
				},
			}).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);

			Thread.Sleep(1000);

			response = device.Values(new DeviceValuesFilter { streams = "testdevicestream" }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var values = response.Json<DeviceValueList<TestDeviceValue>>();
			Assert.IsNotNull(values);
			Assert.IsNotNull(values.values);
			Assert.IsTrue(values.values.Length >= 3);
			int count = values.values.Length;

			response = device.SearchValues(new DeviceValuesSearchParams<TestDeviceConditions>
			{
				start = values.values.Min(v => v.timestamp),
				end = values.values.Max(v => v.timestamp),
				streams = new string[] { "testdevicestream" },
				conditions = new TestDeviceConditions { testdevicestream = new ValueCondition { gt = 1 } },
			}).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			values = response.Json<DeviceValueList<TestDeviceValue>>();
			Assert.IsNotNull(values);
			Assert.IsNotNull(values.values);
			Assert.IsTrue(values.values.Length > 0);
			Assert.IsTrue(values.values.Length < count);
		}

		[Ignore]
		[TestMethod]
		public void DistributionApiTest()
		{
			// distribution

			response = m2x.CreateDistribution(new DistributionParams
				{
					name = "TestDistribution-" + this.TestId,
					visibility = M2XVisibility.Public,
				}).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			var distributionDetails = response.Json<DistributionDetails>();
			string distributionId = distributionDetails.id;
			Assert.IsNotNull(distributionId);
			this.distribution = m2x.Distribution(distributionId);
			Assert.AreEqual(M2XVisibility.Public, distributionDetails.visibility);

			Thread.Sleep(1000);

			response = distribution.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			distributionDetails = response.Json<DistributionDetails>();
			Assert.AreEqual(M2XStatus.Enabled, distributionDetails.status);

			response = m2x.Distributions().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var distributionList = response.Json<DistributionList>();
			Assert.IsNotNull(distributionList.distributions);
			Assert.IsTrue(distributionList.distributions.Length > 0);

			response = distribution.Update(new DistributionParams
			{
				name = distributionDetails.name,
				visibility = M2XVisibility.Private,
				description = "test",
			}).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);

			// device

			response = distribution.AddDevice(new DistributionDeviceParams { serial = this.TestId }).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			var deviceDetails = response.Json<DeviceDetails>();
			Assert.IsNotNull(deviceDetails.id);
			this.device = m2x.Device(deviceDetails.id);
			Assert.AreEqual(this.TestId, deviceDetails.serial);

			Thread.Sleep(1000);

			response = distribution.Devices().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var deviceList = response.Json<DeviceList>();
			Assert.IsNotNull(deviceList.devices);
			Assert.AreEqual(1, deviceList.devices.Length);

			// stream

			this.stream = distribution.Stream("testdevicestream");
			response = stream.Update(new StreamParams
			{
				unit = new StreamUnit { label = "points", symbol = "pt" },
			}).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);

			Thread.Sleep(1000);

			response = stream.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var streamDetails = response.Json<StreamDetails>();
			Assert.AreEqual(M2XStreamType.Numeric, streamDetails.type);

			response = distribution.Streams().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var streamList = response.Json<StreamList>();
			Assert.IsNotNull(streamList.streams);
			Assert.AreEqual(1, streamList.streams.Length);
		}

		[TestMethod]
		public void KeyApiTest()
		{
			// keys

			response = m2x.Keys().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var keyList = response.Json<KeyList>();
			Assert.IsNotNull(keyList.keys);
			Assert.IsTrue(keyList.keys.Length > 0);

			response = m2x.CreateKey(new KeyParams
				{
					name = "testkey" + this.TestId,
					permissions = new[] { "POST" },
				}).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			var keyDetails = response.Json<KeyDetails>();
			Assert.IsNotNull(keyDetails.key);
			this.key = m2x.Key(keyDetails.key);

			Thread.Sleep(1000);

			response = key.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			keyDetails = response.Json<KeyDetails>();
			Assert.IsTrue(keyDetails.master);
			Assert.IsFalse(keyDetails.expired);

			response = key.Update(new KeyParams
				{
					name = keyDetails.name,
					permissions = new[] { "GET" },
					device_access = M2XVisibility.Public,
				}).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);

			response = key.Regenerate().Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			keyDetails = response.Json<KeyDetails>();
			Assert.IsNotNull(keyDetails.key);
			var newKey = m2x.Key(keyDetails.key);

			Thread.Sleep(1000);

			Delete(newKey);
			key = null;
		}

		[TestMethod]
		public void CollectionsApiTest()
		{
			var parms = new CollectionParams
			{
				name = "TestCollection-" + this.TestId,
				description = "UnitTest",
			};
			response = m2x.CreateCollection(parms).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			var collectionDetails = response.Json<CollectionDetails>();
			Assert.IsNotNull(collectionDetails.id);
			this.collection = m2x.Collection(collectionDetails.id);
			Assert.AreEqual(parms.name, collectionDetails.name);

			Thread.Sleep(1000);

			response = this.collection.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			collectionDetails = response.Json<CollectionDetails>();
			Assert.AreEqual(parms.name, collectionDetails.name);
			Assert.AreEqual(parms.description, collectionDetails.description);

			response = m2x.Collections().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var collectionList = response.Json<CollectionList>();
			Assert.IsNotNull(collectionList.collections);
			Assert.IsTrue(collectionList.collections.Length > 0);
			Assert.AreEqual(parms.name, collectionList.collections[collectionList.collections.Length - 1].name);

			parms.name += "_";
			response = collection.Update(parms).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);
			response = collection.Details().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			collectionDetails = response.Json<CollectionDetails>();
			Assert.AreEqual(parms.name, collectionDetails.name);

			// metadata

			response = collection.UpdateMetadata(new TestMetadata { field1 = "value1", field2 = "value2" }).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);

			response = collection.Metadata().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var metadata = response.Json<TestMetadata>();
			Assert.IsNotNull(metadata);
			Assert.AreEqual("value1", metadata.field1);

			response = collection.UpdateMetadataField("field1", new MetadataFieldParams { value = "value3" }).Result;
			Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);

			response = collection.MetadataField("field2").Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var value = response.Json<MetadataFieldParams>();
			Assert.IsNotNull(value);
			Assert.AreEqual("value2", value.value);

			response = collection.MetadataField("field1").Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			value = response.Json<MetadataFieldParams>();
			Assert.IsNotNull(value);
			Assert.AreEqual("value3", value.value);
		}

		[TestMethod]
		public void JobsApiTest()
		{
			response = m2x.Jobs(new JobListParams { page = 1, limit = 10 }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var list = response.Json<JobList>();
			Assert.IsNotNull(list);

			if (list.jobs != null && list.jobs.Length > 0)
			{
				string jobId = list.jobs[0].id;
				response = m2x.JobDetails(jobId).Result;
				Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
				var job = response.Json<JobDetails>();
				Assert.IsNotNull(job);
				Assert.AreEqual(jobId, job.id);
			}
		}

		[TestMethod]
		public void TimeApiTest()
		{
			response = m2x.Time().Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var time = response.Json<TimeInfo>();
			Assert.IsNotNull(time);

			response = m2x.Time(M2XTimeFormat.Seconds).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			Assert.IsNotNull(response.Raw);

			response = m2x.Time(M2XTimeFormat.Millis).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			Assert.IsNotNull(response.Raw);

			response = m2x.Time(M2XTimeFormat.Iso8601).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			Assert.IsNotNull(response.Raw);
		}

		[TestMethod]
		public void CommandsApiTest()
		{
			response = m2x.CreateDevice(new DeviceParams
			{
				name = "TestDevice-" + this.TestId,
				visibility = M2XVisibility.Private,
			}).Result;
			Assert.AreEqual(HttpStatusCode.Created, response.Status, response.Raw);
			var deviceDetails = response.Json<DeviceDetails>();
			Assert.IsNotNull(deviceDetails.id);
			this.device = m2x.Device(deviceDetails.id);
			Thread.Sleep(1000);

			string commandName = "TestCommand-" + this.TestId;
			response = m2x.SendCommand(new SendCommandParams
			{
				name = commandName,
				targets = new CommandTargets { devices = new string[] { deviceDetails.id } },
			}).Result;
			Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);
			Thread.Sleep(1000);

			response = m2x.Commands(new CommandParams { name = commandName }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var commands = response.Json<CommandList<CommandDetails>>();
			Assert.IsNotNull(commands, response.Raw);
			Assert.IsNotNull(commands.commands, response.Raw);
			Assert.AreEqual(1, commands.commands.Length, response.Raw);
			Assert.AreEqual(commandName, commands.commands[0].name, response.Raw);
			string commandId = commands.commands[0].id;
			Assert.IsNotNull(commandId, response.Raw);

			response = m2x.CommandDetails(commandId).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var command = response.Json<CommandDetails>();
			Assert.IsNotNull(command, response.Raw);
			Assert.AreEqual(commandName, command.name, response.Raw);

			response = m2x.Keys(new KeyListParams { device = deviceDetails.id }).Result;
			Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
			var keys = response.Json<KeyList>();
			Assert.IsNotNull(keys, response.Raw);
			Assert.IsNotNull(keys.keys, response.Raw);
			Assert.AreEqual(1, keys.keys.Length, response.Raw);
			Assert.IsTrue(keys.keys[0].device.Contains(deviceDetails.id), response.Raw);
			string key = keys.keys[0].key;
			Assert.AreEqual(deviceDetails.key, key, response.Raw);
			Assert.IsNotNull(key, response.Raw);

			using (var deviceClient = new M2XClient(deviceDetails.key))
			{
				var device_ = deviceClient.Device(deviceDetails.id);

				response = device_.Commands(new CommandParams { status = M2XCommandStatus.Sent }).Result;
				Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
				commands = response.Json<CommandList<CommandDetails>>();
				Assert.IsNotNull(commands, response.Raw);
				Assert.IsNotNull(commands.commands, response.Raw);
				Assert.AreEqual(1, commands.commands.Length, response.Raw);
				Assert.AreEqual(commandName, commands.commands[0].name, response.Raw);
				Assert.AreEqual(commandId, commands.commands[0].id, response.Raw);
				Assert.AreEqual(M2XCommandStatus.Sent, commands.commands[0].status, response.Raw);

				response = device_.CommandDetails(commandId).Result;
				Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
				command = response.Json<CommandDetails>();
				Assert.IsNotNull(command, response.Raw);
				Assert.AreEqual(commandName, command.name, response.Raw);
				Assert.AreEqual(M2XCommandStatus.Sent, command.status, response.Raw);

				response = device_.ProcessCommand(commandId).Result;
				Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);
				Thread.Sleep(1000);
				response = device_.CommandDetails(commandId).Result;
				Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
				command = response.Json<CommandDetails>();
				Assert.IsNotNull(command, response.Raw);
				Assert.AreEqual(commandName, command.name, response.Raw);
				Assert.AreEqual(M2XCommandStatus.Processed, command.status, response.Raw);

				commandName = "TestCommand2-" + this.TestId;
				response = m2x.SendCommand(new SendCommandParams
				{
					name = commandName,
					targets = new CommandTargets { devices = new string[] { deviceDetails.id } },
				}).Result;
				Assert.AreEqual(HttpStatusCode.Accepted, response.Status, response.Raw);
				Thread.Sleep(1000);

				response = device_.Commands(new CommandParams { status = M2XCommandStatus.Sent }).Result;
				Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
				commands = response.Json<CommandList<CommandDetails>>();
				Assert.IsNotNull(commands, response.Raw);
				Assert.IsNotNull(commands.commands, response.Raw);
				Assert.AreEqual(1, commands.commands.Length, response.Raw);
				Assert.AreEqual(commandName, commands.commands[0].name, response.Raw);
				commandId = commands.commands[0].id;

				response = device_.RejectCommand(commandId).Result;
				Assert.AreEqual(HttpStatusCode.NoContent, response.Status, response.Raw);
				Thread.Sleep(1000);
				response = device_.CommandDetails(commandId).Result;
				Assert.AreEqual(HttpStatusCode.OK, response.Status, response.Raw);
				command = response.Json<CommandDetails>();
				Assert.IsNotNull(command, response.Raw);
				Assert.AreEqual(commandName, command.name, response.Raw);
				Assert.AreEqual(M2XCommandStatus.Rejected, command.status, response.Raw);
			}
		}

		[DataContract]
		private class TestMetadata
		{
			[DataMember]
			public string field1;
			[DataMember]
			public string field2;
		}

		[DataContract]
		private class TestDeviceValue
		{
			[DataMember]
			public string timestamp;
			[DataMember]
			public TestDeviceStreamValue values;
		}

		[DataContract]
		private class TestDeviceStreamValue
		{
			[DataMember]
			public int testdevicestream;
		}

		[DataContract]
		private class TestDeviceValues
		{
			[DataMember]
			public TestDeviceStreamValues values;
		}

		[DataContract]
		private class TestDeviceStreamValues
		{
			[DataMember]
			public StreamValue[] testdevicestream;
		}

		[DataContract]
		private class TestDeviceConditions
		{
			[DataMember]
			public ValueCondition testdevicestream;
		}
	}
}
