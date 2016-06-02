//
//  LauncherHandler.cs
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

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

/*
 * This class has a lot of async stuff going on. It handles updating the launcher
 * and loading the changelog from the server.
 * Since this class starts new threads in which it does the larger computations,
 * there must be no useage of UI code in this class. Keep it clean!
 *
 */
using Launchpad.Launcher.Handlers.Protocols;
using System.Net;
using log4net;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// This class has a lot of async stuff going on. It handles updating the launcher
	/// and loading the changelog from the server.
	/// Since this class starts new threads in which it does the larger computations,
	/// there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class LauncherHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(LauncherHandler));

		public event ChangelogDownloadFinishedEventHandler ChangelogDownloadFinished;
		public event LauncherDownloadFinishedEventHandler LauncherDownloadFinished;
		public event LauncherDownloadProgressChangedEventHandler LauncherDownloadProgressChanged;

		private readonly ChangelogDownloadFinishedEventArgs ChangelogDownloadFinishedArgs = new ChangelogDownloadFinishedEventArgs();
		private readonly PatchProtocolHandler Patch;

		/// <summary>
		/// The config handler reference.
		/// </summary>
		private static readonly ConfigHandler Config = ConfigHandler.Instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad.Launcher.Handlers.LauncherHandler"/> class.
		/// </summary>
		public LauncherHandler()
		{
			Patch = Config.GetPatchProtocol();
		}

		/// <summary>
		/// Updates the launcher asynchronously.
		/// </summary>
		public void UpdateLauncher()
		{
			try
			{
				Log.Info($"Starting update of lancher files using protocol \"{this.Patch.GetType().Name}\"");

				Patch.ModuleDownloadProgressChanged += OnLauncherDownloadProgressChanged;
				Patch.ModuleInstallationFinished += OnLauncherDownloadFinished;

				Thread t = new Thread(UpdateLauncher_Implementation);
				t.Start();
			}
			catch (IOException ioex)
			{
				Log.Warn("The launcher update failed (IOException): " + ioex.Message);
			}
		}

		private void UpdateLauncher_Implementation()
		{
			Patch.UpdateModule(EModule.Launcher);
		}

		/// <summary>
		/// Checks if the launcher can access the standard HTTP changelog.
		/// </summary>
		/// <returns><c>true</c> if the changelog can be accessed; otherwise, <c>false</c>.</returns>
		public bool CanAccessStandardChangelog()
		{
			HttpWebRequest headRequest = (HttpWebRequest)WebRequest.Create(Config.GetChangelogURL());
			headRequest.Method = "HEAD";

			try
			{
				using (HttpWebResponse headResponse = (HttpWebResponse)headRequest.GetResponse())
				{
					return (headResponse.StatusCode == HttpStatusCode.OK);
				}
			}
			catch (WebException wex)
			{
				Log.Warn("Could not access standard changelog (WebException): " + wex.Message);
				return false;
			}
		}

		/// <summary>
		/// Gets the changelog from the server asynchronously.
		/// </summary>
		public void LoadFallbackChangelog()
		{
			Thread t = new Thread(LoadFallbackChangelog_Implementation);
			t.Start();
		}

		private void LoadFallbackChangelog_Implementation()
		{
			if (Patch.CanProvideChangelog())
			{
				ChangelogDownloadFinishedArgs.HTML = Patch.GetChangelog();
				ChangelogDownloadFinishedArgs.URL = Config.GetChangelogURL();
			}

			OnChangelogDownloadFinished();
		}

		/// <summary>
		/// Creates the update script on disk.
		/// </summary>
		/// <returns>ProcessStartInfo for the update script.</returns>
		public static ProcessStartInfo CreateUpdateScript()
		{
			try
			{
				//maintain the executable name if it was renamed to something other than 'Launchpad'
				string assemblyPath = Assembly.GetEntryAssembly().Location;
				string executableName = Path.GetFileName(assemblyPath);

				if (ChecksHandler.IsRunningOnUnix())
				{
					//creating a .sh script
					string scriptPath = $@"{Path.GetTempPath()}launchpadupdate.sh";


					using (FileStream updateScript = File.Create(scriptPath))
					{
						using (TextWriter tw = new StreamWriter(updateScript))
						{
							string copyCom = $"cp -rf {Path.GetTempPath() + "launchpad/launcher/*"} {ConfigHandler.GetLocalDir()}";
							string delCom = $"rm -rf {Path.GetTempPath() + "launchpad"}";
							string dirCom = $"cd {ConfigHandler.GetLocalDir()}";
							string launchCom = $@"nohup ./{executableName} &";

							tw.WriteLine(@"#!/bin/sh");
							tw.WriteLine("sleep 5");
							tw.WriteLine(copyCom);
							tw.WriteLine(delCom);
							tw.WriteLine(dirCom);
							tw.WriteLine("chmod +x " + executableName);
							tw.WriteLine(launchCom);
						}
					}

					UnixHandler.MakeExecutable(scriptPath);


					//Now create some ProcessStartInfo for this script
					ProcessStartInfo updateShellProcess = new ProcessStartInfo
					{
						FileName = scriptPath,
						UseShellExecute = false,
						RedirectStandardOutput = false,
						WindowStyle = ProcessWindowStyle.Hidden
					};

					return updateShellProcess;
				}
				else
				{
					//creating a .bat script
					string scriptPath = $@"{Path.GetTempPath()}launchpadupdate.bat";

					using (FileStream updateScript = File.Create(scriptPath))
					{
						using (TextWriter tw = new StreamWriter(updateScript))
						{
							//write commands to the script
							//wait three seconds, then copy the new executable
							tw.WriteLine(
								$"timeout 3 & xcopy /e /s /y \"{Path.GetTempPath()}\\launchpad\\launcher\" \"{ConfigHandler.GetLocalDir()}\" && rmdir /s /q {Path.GetTempPath()}\\launchpad");

							//then start the new executable
							tw.WriteLine($"start {executableName}");
							tw.Close();
						}
					}

					ProcessStartInfo updateBatchProcess = new ProcessStartInfo
					{
						FileName = scriptPath,
						UseShellExecute = true,
						RedirectStandardOutput = false,
						WindowStyle = ProcessWindowStyle.Hidden
					};

					return updateBatchProcess;
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Failed to create update script (IOException): " + ioex.Message);

				return null;
			}
		}

		/// <summary>
		/// Raises the changelog download finished event.
		/// Fires when the changelog has finished downloading and all values have been assigned.
		/// </summary>
		private void OnChangelogDownloadFinished()
		{
			if (ChangelogDownloadFinished != null)
			{
				//raise the event
				ChangelogDownloadFinished(this, ChangelogDownloadFinishedArgs);
			}
		}

		private void OnLauncherDownloadProgressChanged(object sender, ModuleProgressChangedArgs e)
		{
			if (LauncherDownloadProgressChanged != null)
			{
				LauncherDownloadProgressChanged(sender, e);
			}
		}

		private void OnLauncherDownloadFinished(object sender, ModuleInstallationFinishedArgs e)
		{
			if (LauncherDownloadFinished != null)
			{
				LauncherDownloadFinished(sender, e);
			}
		}
	}

	/*
		Launcher-specific events
	*/
	public delegate void ChangelogDownloadFinishedEventHandler(object sender,ChangelogDownloadFinishedEventArgs e);
	public delegate void LauncherDownloadProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void LauncherDownloadFinishedEventHandler(object sendre,ModuleInstallationFinishedArgs e);

	/*
		Launcher-specific event arguments
	*/
	public class ChangelogDownloadFinishedEventArgs : EventArgs
	{
		public string HTML
		{
			get;
			set;
		}

		public string URL
		{
			get;
			set;
		}
	}
}

