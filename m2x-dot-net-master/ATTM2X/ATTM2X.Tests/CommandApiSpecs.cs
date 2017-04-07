using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATTM2X.Tests
{
	[TestClass]
	public class CommandApiSpecs
	{
		private static readonly string _masterKey = ConfigurationManager.AppSettings["ApiMasterKey"];
		private static Device _testDevice { get; set; }
		private static bool _testDeviceExists { get; set; }

		#region " Initialize and Cleanup Methods "

		[ClassInitialize]
		public static void InitializeTestSpecs(TestContext testContext)
		{
			using (var client = new M2XClient(_masterKey))
			{
				var createDeviceParms = $"{{ \"name\": \"*** PLEASE DELETE ME *** Test Auto Created Device {DateTime.UtcNow.Ticks}\", \"visibility\": \"private\" }}";
				var createDeviceResult = client.CreateDevice(createDeviceParms).Result;
				_testDevice = JsonConvert.DeserializeObject<Device>(createDeviceResult.Raw);
				_testDeviceExists = _testDevice != null;
			}
		}

		[ClassCleanup]
		public static void CleanupTestSpecs()
		{
			DestroyTestDevice();
		}

		[TestInitialize]
		public void InitializeIndividualTest()
		{
			Task.Delay(TimeSpan.FromMilliseconds(250));
		}

		private static void DestroyTestDevice()
		{
			if (_testDeviceExists)
			{
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					device.Delete();
					System.Threading.Thread.Sleep(500);
					_testDevice = null;
					_testDeviceExists = false;
				}
			}
		}

		#endregion " Initialize and Cleanup Methods "

		[TestMethod]
		public async Task CanAccess_MasterApiKey_ListOf_CommandsSent_NoFilters()
		{
			try
			{
				using (var client = new M2XClient(_masterKey))
				{
					var result = await client.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					ProcessCommandSearchResult(result.Raw, null);
				}
			}
			catch (Exception)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_ListOf_CommandsSent_WithFilters()
		{
			try
			{
				using (var client = new M2XClient(_masterKey))
				{
					foreach (var filter in new[] { "limit", "page", "dir|desc", "dir|asc", "start", "end", "name" })
					{
						M2XResponse result = null;
						switch (filter)
						{
							case "limit":
								result = await client.Commands(new { limit = 2 });
								break;
							case "page":
								result = await client.Commands(new { page = 1 });
								break;
							case "dir|desc":
								result = await client.Commands(new { dir = "desc" });
								break;
							case "dir|asc":
								result = await client.Commands(new { dir = "asc" });
								break;
							case "start":
								result = await client.Commands(new { start = DateTime.UtcNow.AddMinutes(-60).ToString(Constants.ISO8601_DateStartFormat) });
								break;
							case "end":
								result = await client.Commands(new { end = DateTime.UtcNow.AddMinutes(-10).ToString(Constants.ISO8601_DateStartFormat) });
								break;
							case "name":
								result = await client.Commands(new { name = "PHONE_HOME" });
								break;
						}

						Assert.IsNotNull(result);
						Assert.IsFalse(result.Error);
						Assert.IsFalse(result.ServerError);
						Assert.IsNull(result.WebError);
						Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
						ProcessCommandSearchResult(result.Raw, null);
					}
				}
			}
			catch (Exception)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_AndSendCommands()
		{
			try
			{
				using (var client = new M2XClient(_masterKey))
				{
					var sendCommandParms = new StringBuilder($"{{ ");
					sendCommandParms.Append($"\"name\": \"PHONE_HOME\"");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"data\": {{ \"server_url\": \"https://m2x.att.com\" }}");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"targets\": {{ \"devices\": [\"{_testDevice.id}\"] }}");
					sendCommandParms.Append($" }}");

					var result = await client.SendCommand(sendCommandParms.ToString());
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					Assert.AreEqual(Constants.M2X_Response_Success_Accepted, result.Raw);
				}
			}
			catch (Exception)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_ListOf_CommandsSent_TakeOne_AndView_Details()
		{
			try
			{
				var targetCommandId = string.Empty;
				using (var client = new M2XClient(_masterKey))
				{
					var retrieveCommandsResult = await client.Commands();
					Assert.IsNotNull(retrieveCommandsResult);
					Assert.IsFalse(retrieveCommandsResult.Error);
					Assert.IsFalse(retrieveCommandsResult.ServerError);
					Assert.IsNull(retrieveCommandsResult.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(retrieveCommandsResult.Raw));

					var commandData = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(retrieveCommandsResult.Raw);
					if (!commandData.commands.Any()) { Assert.Fail("There are no commands to view. Please send one using the appropriate test or create one manually before trying again."); }
					targetCommandId = commandData.commands.First().id;
				}


				using (var client = new M2XClient(_masterKey))
				{
					var result = await client.CommandDetails(targetCommandId);
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

					var commandDetail = JsonConvert.DeserializeObject<ApiResponseForCommandDetail>(result.Raw);
					Assert.IsNotNull(commandDetail);
					Assert.IsNotNull(commandDetail.data);
					Assert.IsNotNull(commandDetail.deliveries);
					Assert.AreNotEqual(default(DateTime), commandDetail.sent_at);
				}
			}
			catch (Exception)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_AndSendCommands_ToSingleDevice_AndList_DeviceCommands()
		{
			try
			{
				using (var client = new M2XClient(_masterKey))
				{
					var sendCommandParms = new StringBuilder($"{{ ");
					sendCommandParms.Append($"\"name\": \"PHONE_HOME\"");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"data\": {{ \"server_url\": \"https://m2x.att.com\" }}");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"targets\": {{ \"devices\": [\"{_testDevice.id}\"] }}");
					sendCommandParms.Append($" }}");

					var result = await client.SendCommand(sendCommandParms.ToString());
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					Assert.AreEqual(Constants.M2X_Response_Success_Accepted, result.Raw);
				}

				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var result = await device.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					var commands = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(result.Raw);
					Assert.IsNotNull(commands);
				}
			}
			catch (Exception)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_AndSendCommands_ToSingleDevice_AndView_Single_DeviceCommand()
		{
			try
			{
				var commandName = "PHONE_HOME";
				using (var client = new M2XClient(_masterKey))
				{
					var sendCommandParms = new StringBuilder($"{{ ");
					sendCommandParms.Append($"\"name\": \"{commandName}\"");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"data\": {{ \"server_url\": \"https://m2x.att.com\" }}");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"targets\": {{ \"devices\": [\"{_testDevice.id}\"] }}");
					sendCommandParms.Append($" }}");

					var result = await client.SendCommand(sendCommandParms.ToString());
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					Assert.AreEqual(Constants.M2X_Response_Success_Accepted, result.Raw);
				}

				System.Threading.Thread.Sleep(500);
				var targetCommandId = string.Empty;
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var result = await device.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					var commandData = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(result.Raw);
					Assert.IsNotNull(commandData);
					targetCommandId = commandData.commands.First().id;
				}

				System.Threading.Thread.Sleep(500);
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var result = await device.CommandDetails(targetCommandId);
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));

					var commandDetail = JsonConvert.DeserializeObject<ApiResponseForCommandDetail>(result.Raw);
					Assert.IsNotNull(commandDetail);
					Assert.IsNotNull(commandDetail.data);
					Assert.AreNotEqual(default(DateTime), commandDetail.sent_at);
					Assert.AreEqual(commandName, commandDetail.name);
				}
			}
			catch (Exception ex)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_AndSendCommands_ToSingleDevice_AndDevice_MarksCommand_AsProcessed()
		{
			try
			{
				var commandName = "PHONE_HOME";
				using (var client = new M2XClient(_masterKey))
				{
					var sendCommandParms = new StringBuilder($"{{ ");
					sendCommandParms.Append($"\"name\": \"{commandName}\"");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"data\": {{ \"server_url\": \"https://m2x.att.com\" }}");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"targets\": {{ \"devices\": [\"{_testDevice.id}\"] }}");
					sendCommandParms.Append($" }}");

					var result = await client.SendCommand(sendCommandParms.ToString());
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					Assert.AreEqual(Constants.M2X_Response_Success_Accepted, result.Raw);
				}

				System.Threading.Thread.Sleep(1000);
				var targetCommandId = string.Empty;
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var result = await device.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					var commandData = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(result.Raw);
					Assert.IsNotNull(commandData);
					targetCommandId = commandData.commands.All(a => a.status_counts == null)
						? commandData.commands.First().id
						: commandData.commands.First(f => f.status_counts.processed == 0 && f.status_counts.rejected == 0 && f.status_counts.pending > 0).id;
				}

				System.Threading.Thread.Sleep(500);
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var processParms = $"{{ \"madethecall\": \"today\" }}";
					var result = await device.ProcessCommand(targetCommandId, processParms);
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsTrue(string.IsNullOrWhiteSpace(result.Raw));
				}

				System.Threading.Thread.Sleep(500);
				using (var client = new M2XClient(_masterKey))
				{
					var result = await client.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					ProcessCommandSearchResultFindTargetCommandAndCheckStatus(result.Raw, _testDevice.id, targetCommandId, "processed");
				}
			}
			catch (Exception ex)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_AndSendCommands_ToSingleDevice_AndDevice_MarksCommand_AsRejected()
		{
			try
			{
				var commandName = "PHONE_HOME";
				using (var client = new M2XClient(_masterKey))
				{
					var sendCommandParms = new StringBuilder($"{{ ");
					sendCommandParms.Append($"\"name\": \"{commandName}\"");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"data\": {{ \"server_url\": \"https://m2x.att.com\" }}");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"targets\": {{ \"devices\": [\"{_testDevice.id}\"] }}");
					sendCommandParms.Append($" }}");

					var result = await client.SendCommand(sendCommandParms.ToString());
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					Assert.AreEqual(Constants.M2X_Response_Success_Accepted, result.Raw);
				}

				System.Threading.Thread.Sleep(1000);
				var targetCommandId = string.Empty;
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var result = await device.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					var commandData = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(result.Raw);
					Assert.IsNotNull(commandData);
					targetCommandId = commandData.commands.All(a => a.status_counts == null)
						? commandData.commands.First().id
						: commandData.commands.First(f => f.status_counts.processed == 0 && f.status_counts.rejected == 0 && f.status_counts.pending > 0).id;
				}

				System.Threading.Thread.Sleep(500);
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					var processParms = $"{{ \"reason\": \"Because I Can!\" }}";
					var result = await device.RejectCommand(targetCommandId, processParms);
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsTrue(string.IsNullOrWhiteSpace(result.Raw));
				}

				System.Threading.Thread.Sleep(500);
				using (var client = new M2XClient(_masterKey))
				{
					var result = await client.Commands();
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					ProcessCommandSearchResultFindTargetCommandAndCheckStatus(result.Raw, _testDevice.id, targetCommandId, "rejected");
				}
			}
			catch (Exception ex)
			{
				DestroyTestDevice();
				throw;
			}
		}

		[TestMethod]
		public async Task CanAccess_MasterApiKey_AndSendCommand_ToSingleDevice_ThenList_CommandsSent_WithFilters()
		{
			try
			{
				var commandName = "PHONE_HOME";
				using (var client = new M2XClient(_masterKey))
				{
					var sendCommandParms = new StringBuilder($"{{ ");
					sendCommandParms.Append($"\"name\": \"{commandName}\"");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"data\": {{ \"server_url\": \"https://m2x.att.com\" }}");
					sendCommandParms.Append($", ");
					sendCommandParms.Append($"\"targets\": {{ \"devices\": [\"{_testDevice.id}\"] }}");
					sendCommandParms.Append($" }}");

					var result = await client.SendCommand(sendCommandParms.ToString());
					Assert.IsNotNull(result);
					Assert.IsFalse(result.Error);
					Assert.IsFalse(result.ServerError);
					Assert.IsNull(result.WebError);
					Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
					Assert.AreEqual(Constants.M2X_Response_Success_Accepted, result.Raw);
				}

				System.Threading.Thread.Sleep(1000);
				using (var client = new M2XClient(_masterKey))
				{
					var device = client.Device(_testDevice.id);
					foreach (var filter in new[] { "limit", "page", "dir|desc", "dir|asc", "start", "end", "name", "status|pending", "status|processed", "status|rejected" })
					{
						M2XResponse result = null;
						switch (filter)
						{
							case "limit":
								result = await device.Commands(new { limit = 2 });
								break;
							case "page":
								result = await device.Commands(new { page = 1 });
								break;
							case "dir|desc":
								result = await device.Commands(new { dir = "desc" });
								break;
							case "dir|asc":
								result = await device.Commands(new { dir = "asc" });
								break;
							case "start":
								result = await device.Commands(new { start = DateTime.UtcNow.AddMinutes(-60).ToString(Constants.ISO8601_DateStartFormat) });
								break;
							case "end":
								result = await device.Commands(new { end = DateTime.UtcNow.AddMinutes(-10).ToString(Constants.ISO8601_DateStartFormat) });
								break;
							case "name":
								result = await device.Commands(new { name = "PHONE_HOME" });
								break;
							case "status|pending":
								result = await device.Commands(new { status = "pending" });
								break;
							case "status|processed":
								result = await device.Commands(new { status = "processed" });
								break;
							case "status|rejected":
								result = await device.Commands(new { status = "rejected" });
								break;
						}

						Assert.IsNotNull(result);
						Assert.IsFalse(result.Error);
						Assert.IsFalse(result.ServerError);
						Assert.IsNull(result.WebError);
						Assert.IsFalse(string.IsNullOrWhiteSpace(result.Raw));
						ProcessCommandSearchResult(result.Raw, null);
					}
				}
			}
			catch (Exception ex)
			{
				DestroyTestDevice();
				throw;
			}
		}


		#region " Process And Evaluate Raw Response Data "

		private void ProcessCommandSearchResult(string json, bool? shouldHaveCommands = true)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(json);
			Assert.IsNotNull(resultValues);
			if (shouldHaveCommands.HasValue)
			{
				if (shouldHaveCommands.Value)
				{
					Assert.IsTrue(resultValues.commands.Any());
				}
				else
				{
					Assert.IsFalse(resultValues.commands.Any());
				}
			}
			foreach (var command in resultValues.commands)
			{
				Console.WriteLine($"Command {command.name} ({command.id}) sent at {command.sent_at.ToString(Constants.ISO8601_DateDefaultFormat)} was found.");
			}
		}

		private void ProcessCommandSearchResultFindTargetCommandAndCheckStatus(string json, string targetDeviceId, string targetCommandId, string status)
		{
			var resultValues = JsonConvert.DeserializeObject<ApiResponseForCommandSearch>(json);
			Assert.IsNotNull(resultValues);
			Assert.IsTrue(resultValues.commands.Any(), "No commands found for master api key.");
			Assert.IsTrue(resultValues.commands.Any(a => a.id == targetCommandId));
			foreach (var command in resultValues.commands.Where(w => w.id == targetCommandId))
			{
				switch (status)
				{
					case "pending":
						Assert.IsTrue(command.status_counts.pending > 0);
						break;
					case "processed":
						Assert.IsTrue(command.status_counts.processed > 0);
						break;
					case "rejected":
						Assert.IsTrue(command.status_counts.rejected > 0);
						break;
				}
				Console.WriteLine($"Command {command.name} ({command.id}) sent at {command.sent_at.ToString(Constants.ISO8601_DateDefaultFormat)} matching status \"{status}\"was found.");
			}
		}


		#endregion " Process And Evaluate Raw Response Data "
	}

	#region " Response Objects "

	public class ApiResponseForCommandSearch
	{
		public Command[] commands { get; set; }
	}

	public class ApiResponseForCommandDetail
	{
		public string id { get; set; }
		public string url { get; set; }
		public string name { get; set; }
		public object data { get; set; }
		public DateTime sent_at { get; set; }
		public object deliveries { get; set; }
	}

	public class Command
	{
		public string id { get; set; }
		public string url { get; set; }
		public string name { get; set; }
		public DateTime sent_at { get; set; }
		public Status_Counts status_counts { get; set; }
	}

	public class Status_Counts
	{
		public int processed { get; set; }
		public int rejected { get; set; }
		public int pending { get; set; }
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

	public class Streams
	{
		public int count { get; set; }
		public string url { get; set; }
	}

	public class Triggers
	{
		public int count { get; set; }
		public string url { get; set; }
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