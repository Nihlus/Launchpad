using System;
using System.Net;

namespace Launchpad.Launcher
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
				string formattedURL = String.Format(baseURL + "guid={0}&launcherVersion={1}&gameName={2}&systemType={3}&officialUpdates={4}",
				                                    Config.GetGUID(),
				                                    Config.GetLocalLauncherVersion(),
				                                    Config.GetGameName(),
                                                    Config.GetSystemTarget().ToString(),
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

