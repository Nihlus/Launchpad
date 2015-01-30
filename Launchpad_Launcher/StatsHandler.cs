using System;
using System.Net;

namespace Launchpad_Launcher
{
	public class StatsHandler
	{
		public StatsHandler ()
		{

		}

		public void SendUseageStats(string guid, string version, string gameName, bool officialUpdates)
		{
			try
			{
				string baseURL = "http://directorate.asuscomm.com/launchpad/stats.php?";
				string formattedURL = String.Format(baseURL + "guid={0}&launcherVersion={1}&gameName={2}&officialUpdates={3}",
				                                    guid,
				                                    version,
				                                    gameName,
				                                    officialUpdates.ToString()
				                                    );

				WebRequest getRequest;
				getRequest = WebRequest.Create(formattedURL);
				getRequest.GetResponse();
			}
			catch (Exception ex)
			{
				Console.WriteLine (ex.StackTrace);
			}


		}
	}
}

