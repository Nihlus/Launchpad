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
		public event ChangelogDownloadFinishedEventHandler ChangelogDownloadFinished;

		private readonly ChangelogDownloadFinishedEventArgs ChangelogDownloadFinishedArgs = new ChangelogDownloadFinishedEventArgs();

		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

		//TODO: Run asynchronously
		/// <summary>
		/// Updates the launcher synchronously.
		/// </summary>
		public void UpdateLauncher()
		{
			try
			{				
				PatchProtocolHandler Patch = Config.GetPatchProtocol();
				Patch.DownloadLauncher();
				
				ProcessStartInfo script = CreateUpdateScript();

				Process.Start(script);
				Environment.Exit(0);
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in UpdateLauncher(): " + ioex.Message);
			}
		}

		/// <summary>
		/// Gets the changelog from the server asynchronously.
		/// </summary>
		public void LoadChangelog()
		{
			Thread t = new Thread(LoadChangelogAsync);
			t.Start();
		}

		private void LoadChangelogAsync()
		{
			FTPProtocolHandler FTP = new FTPProtocolHandler();

			//load the HTML from the server as a string
			string content = FTP.ReadFTPFile(Config.GetChangelogURL());
					
			ChangelogDownloadFinishedArgs.HTML = content;
			ChangelogDownloadFinishedArgs.URL = Config.GetChangelogURL();

			OnChangelogDownloadFinished();
		}

		/// <summary>
		/// Creates the update script on disk.
		/// </summary>
		/// <returns>ProcessStartInfo for the update script.</returns>
		private static ProcessStartInfo CreateUpdateScript()
		{
			try
			{
				//maintain the executable name if it was renamed to something other than 'Launchpad' 
				string assemblyPath = Assembly.GetEntryAssembly().Location;
				string executableName = Path.GetFileName(assemblyPath);

				if (ChecksHandler.IsRunningOnUnix())
				{
					//creating a .sh script
					string scriptPath = String.Format(@"{0}launchpadupdate.sh", 
						                    Path.GetTempPath());


					using (FileStream updateScript = File.Create(scriptPath))
					{
						using (TextWriter tw = new StreamWriter(updateScript))
						{							
							string copyCom = String.Format("cp -rf {0} {1}", 
								                 Path.GetTempPath() + "launchpad/*",
								                 ConfigHandler.GetLocalDir());

							string delCom = String.Format("rm -rf {0}", 
								                Path.GetTempPath() + "launchpad");

							string dirCom = String.Format("cd {0}", ConfigHandler.GetLocalDir());
							string launchCom = String.Format(@"nohup ./{0} &", executableName);
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
					ProcessStartInfo updateShellProcess = new ProcessStartInfo();
									
					updateShellProcess.FileName = scriptPath;
					updateShellProcess.UseShellExecute = false;
					updateShellProcess.RedirectStandardOutput = false;
					updateShellProcess.WindowStyle = ProcessWindowStyle.Hidden;

					return updateShellProcess;
				}
				else
				{
					//creating a .bat script
					string scriptPath = String.Format(@"{0}launchpadupdate.bat", 
						                    Path.GetTempPath());

					using (FileStream updateScript = File.Create(scriptPath))
					{
						using (TextWriter tw = new StreamWriter(updateScript))
						{
							//write commands to the script
							//wait three seconds, then copy the new executable
							tw.WriteLine(String.Format(@"timeout 3 & xcopy /e /s /y ""{0}\launchpad"" ""{1}"" && rmdir /s /q {0}\launchpad", 
									Path.GetTempPath(), 
									ConfigHandler.GetLocalDir()));

							//then start the new executable
							tw.WriteLine(String.Format(@"start {0}", executableName));
							tw.Close();
						}
					}

					ProcessStartInfo updateBatchProcess = new ProcessStartInfo();

					updateBatchProcess.FileName = scriptPath;
					updateBatchProcess.UseShellExecute = true;
					updateBatchProcess.RedirectStandardOutput = false;
					updateBatchProcess.WindowStyle = ProcessWindowStyle.Hidden;

					return updateBatchProcess;
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in CreateUpdateScript(): " + ioex.Message);

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
	}

	/*
		Launcher-specific events
	*/
	public delegate void ChangelogDownloadFinishedEventHandler(object sender,ChangelogDownloadFinishedEventArgs e);

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

