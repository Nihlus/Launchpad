using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using Launchpad.Launcher.Events.Arguments;
using Launchpad.Launcher.Events.Delegates;

/*
 * This class has a lot of async stuff going on. It handles updating the launcher
 * and loading the changelog from the server.
 * Since this class starts new threads in which it does the larger computations,
 * there must be no useage of UI code in this class. Keep it clean!
 * 
 */

namespace Launchpad.Launcher
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
		/// Occurs when changelog download progress changes.
		/// </summary>
		public event ChangelogProgressChangedEventHandler ChangelogProgressChanged;
		/// <summary>
		/// Occurs when changelog download finishes.
		/// </summary>
		public event ChangelogDownloadFinishedEventHandler ChangelogDownloadFinished;

		/// <summary>
		/// The progress arguments object. Is updated during file download operations.
		/// </summary>
		private FileDownloadProgressChangedEventArgs ProgressArgs;
		/// <summary>
		/// The download finished arguments object. Is updated once a file download finishes.
		/// </summary>
		private GameDownloadFinishedEventArgs DownloadFinishedArgs;

		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.LauncherHandler"/> class.
		/// </summary>
		public LauncherHandler ()
		{
			ProgressArgs = new FileDownloadProgressChangedEventArgs ();
			DownloadFinishedArgs = new GameDownloadFinishedEventArgs ();
		}

		//TODO: Update this function to handle DLLs as well. May have to implement a full-blown
		//manifest system here as well.

		/// <summary>
		/// Updates the launcher synchronously.
		/// </summary>
		public void UpdateLauncher()
		{
			try
			{
				FTPHandler FTP = new FTPHandler ();

                //crawl the server for all of the files in the /launcher/bin directory.
                List<string> remotePaths = FTP.GetFilePaths(Config.GetLauncherBinariesURL(), true);

                //download all of them
                foreach (string path in remotePaths)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(path))
                        {
                            string Local = String.Format("{0}launchpad{1}{2}",
                                             ConfigHandler.GetTempDir(),
                                             Path.DirectorySeparatorChar,
                                             path);

                            string Remote = String.Format("{0}{1}",
                                                Config.GetLauncherBinariesURL(),
                                                path);

                            if (!Directory.Exists(Local))
                            {
                                Directory.CreateDirectory(Directory.GetParent(Local).ToString());
                            }

                            FTP.DownloadFTPFile(Remote, Local, false);
                        }                        
                    }
                    catch (WebException wex)
                    {
                        Console.WriteLine("WebException in UpdateLauncher(): " + wex.Message);
                    }
                }
				
                //TODO: Make the script copy recursively
				ProcessStartInfo script = CreateUpdateScript ();

				Process.Start(script);
				Environment.Exit(0);
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in UpdateLauncher(): " + ioex.Message);
			}
		}

		/// <summary>
		/// Downloads the manifest.
		/// </summary>
		public void DownloadManifest()
		{
			Stream manifestStream = null;														
			try
			{
				FTPHandler FTP = new FTPHandler ();

				string remoteChecksum = FTP.GetRemoteManifestChecksum ();
				string localChecksum = "";

				string RemoteURL = Config.GetManifestURL ();
				string LocalPath = ConfigHandler.GetManifestPath ();

				if (File.Exists(ConfigHandler.GetManifestPath()))
				{
					manifestStream = File.OpenRead (ConfigHandler.GetManifestPath ());
                    localChecksum = MD5Handler.GetFileHash(manifestStream);

					if (!(remoteChecksum == localChecksum))
					{
						//Copy the old manifest so that we can compare them when updating the game
						File.Copy(LocalPath, LocalPath + ".old", true);

						FTP.DownloadFTPFile (RemoteURL, LocalPath, false);
					}
				}
				else
				{
					FTP.DownloadFTPFile (RemoteURL, LocalPath, false);
				}						
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in DownloadManifest(): " + ioex.Message);
			}
			finally
			{
				if (manifestStream != null)
				{
					manifestStream.Close ();
				}
			}
		}

		/// <summary>
		/// Gets the changelog from the server asynchronously.
		/// </summary>
		public void LoadChangelog()
		{
			Thread t = new Thread (LoadChangelogAsync);
			t.Start ();

		}
		private void LoadChangelogAsync()
		{
			FTPHandler FTP = new FTPHandler ();

			//load the HTML from the server as a string
			string content = FTP.ReadFTPFile (Config.GetChangelogURL ());
            OnChangelogProgressChanged();
					
			DownloadFinishedArgs.Result = content;
			DownloadFinishedArgs.Metadata = Config.GetChangelogURL ();

			OnChangelogDownloadFinished ();
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
				string fullName = Assembly.GetEntryAssembly().Location;
				string executableName = Path.GetFileName(fullName); // should be "Launchpad", unless the user has renamed it

				if (ChecksHandler.IsRunningOnUnix())
				{
					//creating a .sh script
					string scriptPath = String.Format (@"{0}launchpadupdate.sh", 
					                                   ConfigHandler.GetTempDir ()) ;


					FileStream updateScript = File.Create (scriptPath);
					TextWriter tw = new StreamWriter (updateScript);

					//write commands to the script
					//wait five seconds, then copy the new executable
					string copyCom = String.Format ("cp -rf {0} {1}", 
					                                ConfigHandler.GetTempDir() + "launchpad/*",
					                                ConfigHandler.GetLocalDir());

					string delCom = String.Format ("rm -rf {0}", 
													ConfigHandler.GetTempDir() + "launchpad");

					string dirCom = String.Format ("cd {0}", ConfigHandler.GetLocalDir ());
					string launchCom = String.Format (@"nohup ./{0} &", executableName);
					tw.WriteLine (@"#!/bin/sh");
					tw.WriteLine ("sleep 5");
					tw.WriteLine (copyCom);
					tw.WriteLine (delCom); 
					tw.WriteLine (dirCom);
					tw.WriteLine("chmod +x " + executableName);
					tw.WriteLine (launchCom);
					tw.Close();

                    UnixHandler.MakeExecutable(scriptPath);


					//Now create some ProcessStartInfo for this script
					ProcessStartInfo updateShellProcess = new ProcessStartInfo ();
									
					updateShellProcess.FileName = scriptPath;
					updateShellProcess.UseShellExecute = false;
					updateShellProcess.RedirectStandardOutput = false;
					updateShellProcess.WindowStyle = ProcessWindowStyle.Hidden;

					return updateShellProcess;
				}
				else
				{
					//creating a .bat script
					string scriptPath = String.Format (@"{0}launchpadupdate.bat", 
					                                   ConfigHandler.GetTempDir ());

					FileStream updateScript = File.Create(scriptPath);

					TextWriter tw = new StreamWriter(updateScript);

					//write commands to the script
					//wait three seconds, then copy the new executable
					tw.WriteLine(String.Format(@"timeout 3 & xcopy /e /s /y ""{0}\launchpad"" ""{1}"" && rmdir /s /q {0}\launchpad", 
					                           ConfigHandler.GetTempDir(), 
					                           ConfigHandler.GetLocalDir()));

					//then start the new executable
					tw.WriteLine(String.Format(@"start {0}", executableName));
					tw.Close();

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
				Console.WriteLine ("IOException in CreateUpdateScript(): " + ioex.Message);

                return null;
			}
		}

		/// <summary>
		/// Raises the changelog progress changed event.
        /// Fires once after the changelog has been downloaded, but the values have not been assigned yet.
		/// </summary>
		private void OnChangelogProgressChanged()
		{
			if (ChangelogProgressChanged != null)
			{
				//raise the event
				ChangelogProgressChanged (this, ProgressArgs);
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
				ChangelogDownloadFinished (this, DownloadFinishedArgs);
			}
		}
	}
}

