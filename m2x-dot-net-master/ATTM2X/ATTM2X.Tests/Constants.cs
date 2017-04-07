namespace ATTM2X.Tests
{
	public static class Constants
	{
		public const string ISO8601_DateDefaultFormat = "yyyy-MM-ddTHH:mm:ss.000Z";
		public const string ISO8601_DateEndFormat = "yyyy-MM-ddTHH:mm:59.999Z";
		public const string ISO8601_DateStartFormat = "yyyy-MM-ddTHH:mm:00.000Z";

		public const string TestDeviceNamePrefix = "*** PLEASE DELETE ME *** Test Auto Created Device";
		public const string TestDeviceDescription = "Test device for library testing of public device endpoint";
		public const string TestDeviceDescriptionSearchStringToMatch = "Test device for library testing ";
		public const string TestMetadataDefaultFieldName = "owner";
		public const string TestMetadataDefaultFieldValue = "The Testing Guy";
		public const string TestStreamName001 = "temp"; // as in temperature :)
		public const string TestStreamName002 = "BM";
		public const double TestDeviceLatitude = 28.375252;
		public const double TestDeviceLongitude = -81.549370;

		public static string M2X_Response_Success_Accepted = $"{{\"status\":\"accepted\"}}";
	}
}