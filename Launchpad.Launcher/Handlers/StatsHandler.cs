//
//  StatsHandler.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Net;
using log4net;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// Anonymous stat sending handler.
	/// </summary>
	internal static class StatsHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(StatsHandler));

		/// <summary>
		/// The config handler reference.
		/// </summary>
		private static readonly ConfigHandler Config = ConfigHandler.Instance;

		private const string BASE_URL = "http://directorate.asuscomm.com/launchpad/stats.php?";

		/// <summary>
		/// Sends the usage stats to the official launchpad server.
		/// </summary>
		public static void SendUsageStats()
		{
			try
			{
				string formattedURL = $"{BASE_URL}guid={Config.GetGameGUID()}" +
				                      $"&launcherVersion={Config.GetLocalLauncherVersion()}" +
				                      $"&gameName={Config.GetGameName()}" +
				                      $"&systemType={Config.GetSystemTarget()}" +
				                      $"&officialUpdates={Config.GetDoOfficialUpdates()}" +
									  $"&installguid={Config.GetInstallGUID()}";


				WebRequest sendStatsRequest = WebRequest.Create(formattedURL);
				sendStatsRequest.GetResponse();
			}
			catch (WebException wex)
			{
				Log.Warn("Could not send usage stats (WebException): " + wex.Message);
			}
		}
	}
}

