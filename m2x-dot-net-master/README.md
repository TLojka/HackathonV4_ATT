AT&T's M2X .NET Client
========================

[AT&T M2X](http://m2x.att.com) is a cloud-based fully managed time-series data storage service for network connected machine-to-machine (M2M) devices and the Internet of Things (IoT).

The [AT&T M2X API](https://m2x.att.com/developer/documentation/overview) provides all the needed operations and methods to connect your devices to AT&T's M2X service. This library aims to provide a simple wrapper to interact with the AT&T M2X API for [.NET](http://www.microsoft.com/net). Refer to the [Glossary of Terms](https://m2x.att.com/developer/documentation/glossary) to understand the nomenclature used throughout this documentation.

Getting Started
==========================

1. Signup for an [M2X Account](https://m2x.att.com/signup).
2. Obtain your _Master Key_ from the Master Keys tab of your [Account Settings](https://m2x.att.com/account) screen.
2. Create your first [Device](https://m2x.att.com/devices) and copy its _Device ID_.
3. Review the [M2X API Documentation](https://m2x.att.com/developer/documentation/overview).

Installation and System Requirements
==========================

The M2X .NET Client library is a Portable Class Library. Visual Studio support for the Portable Class Library depends on the version of Visual Studio that you are using.
In some cases, you'll have everything you need, and in other cases, you'll need to install additional items, as shown in the table available here: http://msdn.microsoft.com/en-us/library/gg597391(v=vs.110).aspx.

You will also need .NET Framework version 4.5, which can be downloaded here: http://www.microsoft.com/en-us/download/details.aspx?id=30653.

To begin, simple add the M2X .NET client library as an Existing Project into your VS solution or if you are using a different version of Visual Studio you can create a new class library project and include the content of the [ATTM2X/ATTM2X](https://github.com/attm2x/m2x-dot-net/tree/master/ATTM2X/ATTM2X) folder in it.

Besides the M2X .NET Client library, this solution also includes a tests project which contains multiple examples of library usage, which can be found here: [ATTM2X.Tests](https://github.com/attm2x/m2x-dot-net/tree/master/ATTM2X/ATTM2X.Tests) folder.

System requirements match those for .NET Framework 4.5.

 - Supported Operating System:

		Windows Vista SP2 (x86 and x64)
		Windows 7 SP1 (x86 and x64)
		Windows Server 2008 R2 SP1 (x64)
		Windows Server 2008 SP2 (x86 and x64)
		Windows 8.x, Windows Server 2012
		Windows Phone 8.1

 - Hardware Requirements:

		1 GHz or faster processor
		512 MB of RAM
		850 MB of available hard disk space (x86)
		2 GB hard drive (x64)

Note: Windows 8 and Windows Server 2012 include the .NET Framework 4.5. Therefore, you don't have to install it if you are using those operating systems.

By default, this library targets the following platforms:
		.NET Framework 4.5
		Windows 8
		Windows Phone 8.1

The client library only references assemblies that are supported by those platforms.
To add or remove target platforms, in Solution Explorer, right-click the ATTM2X Library project name, and select **```Properties```** then in the Library tab in the Targeting section click **```Change```**.

Library structure
==========================

Currently, this client supports M2X API v2. All M2X API documents can be found here: [M2X API Documentation](https://m2x.att.com/developer/documentation/overview).
All classes are located within the ATTM2X namespace. All methods of M2X* classes are thread safe.

* [M2XClient](https://github.com/attm2x/m2x-dot-net/blob/master/ATTM2X/ATTM2X/M2XClient.cs): This is the library's main entry point.
In order to communicate with the M2X API you need an instance of this class. The constructor signature includes two (2) parameters:

 apiKey - mandatory parameter. You can find it in your M2X [Account page](https://m2x.att.com/account#master-keys-tab)
Read more about M2X API keys in the [API Keys](https://m2x.att.com/developer/documentation/overview#API-Keys) section of the [M2X API Documentation](https://m2x.att.com/developer/documentation/overview).

 m2xApiEndPoint - optional parameter. You don't need to pass it unless you want to connect to a different API endpoint.

 Client class provides access to API calls returning lists of the following API objects: collections, commands, devices, distributions, jobs, keys.
 There are also a number of methods allowing you to get an instance of an individual API object by providing its id or name as a parameter.
 Refer to the documentation on each class for further usage instructions.

* [M2XResponse](https://github.com/attm2x/m2x-dot-net/blob/master/ATTM2X/ATTM2X/M2XResponse.cs)

 All API responses are wrapped in M2XResponse object.

* [Enums](https://github.com/attm2x/m2x-dot-net/blob/master/ATTM2X/ATTM2X/Enums.cs)

 This file contains all enumerable values that can be used in the API calls.

* [Classes](https://github.com/attm2x/m2x-dot-net/blob/master/ATTM2X/ATTM2X/Classes)

 This folder contains most classes for parameters that can be used within the API calls.

Example
==========================

 - Create a client instance:

```csharp
	using ATTM2X;

	M2XClient client = new M2XClient("<YOUR-API-KEY>");
```

 - Get the list of all your keys:

```csharp
	using ATTM2X.Classes;

	M2XResponse response = client.Keys().Result;
	KeyList keyList = response.Json<KeyList>();
	
	Console.WriteLine("Number of keys is " + keyList.keys.Length);
```
 - Get an instance of a device and its location details:

```csharp
	using ATTM2X.Classes;
	
	M2XDevice device = client.Device("[Device id]");
	M2XResponse response = device.Location().Result;
	LocationDetails location = response.Json<LocationDetails>();
	
	Console.WriteLine("Location name is " + location.name);
```

 - Create a new device, stream and put current value into it:

```csharp
	using ATTM2X.Classes;
	
	M2XResponse response = m2x.CreateDevice(new DeviceParams {
		name = "[Device name]",
		visibility = M2XVisibility.Public,
	}).Result;

	DeviceDetails deviceDetails = response.Json<DeviceDetails>();
	M2XDevice device = m2x.Device(deviceDetails.id);

	M2XStream stream = device.Stream("[Stream name]");
	response = stream.Update(new StreamParams {
		type = M2XStreamType.Numeric,
		unit = new StreamUnit { label = "points", symbol = "pt" },
	}).Result;

	response = stream.UpdateValue(new StreamValue { value = "10" }).Result;
```

Tests project
==========================

The [ATTM2X.Tests](https://github.com/attm2x/m2x-dot-net/blob/master/ATTM2X/ATTM2X.Tests/M2XClientTests.cs) included in the ATTM2X solution have examples for most M2X API methods.

 In order to run these tests you should place your M2X Account Master API Key into the [configuration file](https://github.com/attm2x/m2x-dot-net/blob/master/ATTM2X/ATTM2X.Tests/App.config).

Versioning
==========================

This library aims to adhere to [Semantic Versioning 2.0.0](http://semver.org/). As a summary, given a version number MAJOR.MINOR.PATCH:

MAJOR will increment when backwards-incompatible changes are introduced to the client.
MINOR will increment when backwards-compatible functionality is added.
PATCH will increment with backwards-compatible bug fixes.
Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.

Note: the client version does not necessarily reflect the version used in the AT&T M2X API.

2.0.0 version of the library is server only.
2.1.0+ version is universal/portable.

License
==========================

This library is provided under the MIT license. See [LICENSE](https://github.com/attm2x/m2x-dot-net/blob/master/LICENSE) for applicable terms.
