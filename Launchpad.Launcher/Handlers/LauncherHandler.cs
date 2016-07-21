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

				Thread t = new Thread(() => this.Patch.UpdateModule(EModule.Launcher));
				t.Start();
			}
			catch (IOException ioex)
			{
				Log.Warn("The launcher update failed (IOException): " + ioex.Message);
			}
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
				ChangelogDownloadFinishedArgs.HTML = Patch.GetChangelogSource();
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
				string updateScriptPath = GetUpdateScriptPath();
				string updateScriptSource = GetUpdateScriptSource();

				File.WriteAllText(updateScriptPath, updateScriptSource);

				ProcessStartInfo updateShellProcess = new ProcessStartInfo
				{
					FileName = updateScriptPath,
					UseShellExecute = false,
					RedirectStandardOutput = false,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				return updateShellProcess;
			}
			catch (IOException ioex)
			{
				Log.Warn("Failed to create update script (IOException): " + ioex.Message);

				return null;
			}
		}

		/// <summary>
		/// Extracts the bundled update script and populates the variables in it
		/// with the data needed for the update procedure.
		/// </summary>
		private static string GetUpdateScriptSource()
		{
			// Load the script from the embedded resources
			Assembly localAssembly = Assembly.GetExecutingAssembly();

			string scriptSource = "";
			string resourceName = GetUpdateScriptResourceName();
			using (Stream resourceStream = localAssembly.GetManifestResourceStream(resourceName))
			{
				if (resourceStream != null)
				{
					using (StreamReader reader = new StreamReader(resourceStream))
					{
						scriptSource = reader.ReadToEnd();
					}
				}
			}

			// Replace the variables in the script with actual data
			const string TempDirectoryVariable = "%temp%";
			const string LocalInstallDirectoryVariable = "%localDir%";
			const string LocalExecutableName = "%launchpadExecutable%";

			string transientScriptSource = scriptSource;

			transientScriptSource = transientScriptSource.Replace(TempDirectoryVariable, Path.GetTempPath());
			transientScriptSource = transientScriptSource.Replace(LocalInstallDirectoryVariable, ConfigHandler.GetLocalDir());
			transientScriptSource = transientScriptSource.Replace(LocalExecutableName, Path.GetFileName(localAssembly.Location));

			return transientScriptSource;
		}

		/// <summary>
		/// Gets the name of the embedded update script.
		/// </summary>
		private static string GetUpdateScriptResourceName()
		{
			if (ChecksHandler.IsRunningOnUnix())
			{
				return "Launchpad.Launcher.Resources.launchpad_update.sh";
			}
			else
			{
				 return "Launchpad.Launcher.Resources.launchpad_update.bat";
			}
		}

		/// <summary>
		/// Gets the name of the embedded update script.
		/// </summary>
		private static string GetUpdateScriptPath()
		{
			if (ChecksHandler.IsRunningOnUnix())
			{
				return $@"{Path.GetTempPath()}launchpad_update.sh";
			}
			else
			{
				 return $@"{Path.GetTempPath()}launchpad_update.bat";
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

