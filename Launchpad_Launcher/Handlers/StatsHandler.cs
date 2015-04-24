using System;
using System.Net;

namespace Launchpad
{
	/// <summary>
	/// Anonymous stat sending handler.
	/// </summary>
	internal static class StatsHandler
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		static ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// Sends the usage stats to the official launchpad server.
		/// </summary>
		static public void SendUsageStats()
		{
			WebRequest sendStatsRequest = null;
			try
			{
				string baseURL = "http://directorate.asuscomm.com/launchpad/stats.php?";
				string formattedURL = String.Format(baseURL + "guid={0}&launcherVersion={1}&gameName={2}&officialUpdates={3}",
				                                    Config.GetGUID(),
				                                    Config.GetLocalLauncherVersion(),
				                                    Config.GetGameName(),
				                                    Config.GetDoOfficialUpdates().ToString()
				                                    );


				sendStatsRequest = WebRequest.Create(formattedURL);
				sendStatsRequest.GetResponse();                            
			}
			catch (WebException wex)
			{
				Console.WriteLine ("WebException in SendUsageStats(): " + wex.Message);
			}
			finally
			{
				sendStatsRequest.Abort();   
			}
		}
	}
}

