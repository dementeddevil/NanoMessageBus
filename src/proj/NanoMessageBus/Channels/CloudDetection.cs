namespace NanoMessageBus.Channels
{
	using System.Net;

	internal static class CloudDetection
	{
		public static string DetectFacility()
		{
			return DownloadMetadata("placement/availability-zone");
		}
		public static string DetectMachineId()
		{
			return DownloadMetadata("instance-id");
		}
		private static string DownloadMetadata(string name)
		{
			try
			{
				using (var client = new WebClient())
					return client.DownloadString("http://169.254.169.254/latest/meta-data/" + name);
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
