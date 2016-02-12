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
        /// Occurs when progress changed.
        /// </summary>
        public event GameProgressChangedEventHandler ProgressChanged;
        /// <summary>
        /// Occurs when game update finished.
        /// </summary>
        public event GameUpdateFinishedEventHandler GameUpdateFinished;
        /// <summary>
        /// Occurs when game update failed.
        /// </summary>
        public event GameUpdateFailedEventHandler GameUpdateFailed;
        /// <summary>
        /// The update failed arguments.
        /// </summary>
        private GameUpdateFailedEventArgs UpdateFailedArgs;
        /// <summary>
        /// Occurs when changelog download progress changes.
        /// </summary>
        public event ChangelogProgressChangedEventHandler ChangelogProgressChanged;
		/// <summary>
		/// Occurs when changelog download finishes.
		/// </summary>
		public event ChangelogDownloadFinishedEventHandler ChangelogDownloadFinished;
        /// <summary>
        /// The update finished arguments.
        /// </summary>
        private GameUpdateFinishedEventArgs UpdateFinishedArgs;
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

        /// <summary>
        /// Starts an asynchronous game update task.
        /// </summary>
        public void UpdateLauncher()
        {
            Thread t = new Thread(UpdateLauncherAsync);
            t.Start();
        }


        private void OnGameUpdateFinished()
        {
            if (GameUpdateFinished != null)
            {
                GameUpdateFinished(this, UpdateFinishedArgs);
            }
        }

        private void OnGameUpdateFailed()
        {
            if (GameUpdateFailed != null)
            {
                GameUpdateFailed(this, UpdateFailedArgs);
            }
        }

        /// <summary>
        /// Raises the download progress changed event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void OnDownloadProgressChanged(object sender, FileDownloadProgressChangedEventArgs e)
        {
            ProgressArgs = e;
            OnProgressChanged();
        }

        private void OnFileDownloadFinished(object sender, FileDownloadFinishedEventArgs e)
        {
            OnProgressChanged();
        }

        /// <summary>
        /// Raises the progress changed event.
        /// </summary>
        private void OnProgressChanged()
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, ProgressArgs);
            }
        }


        private void UpdateLauncherAsync()
        {
            ManifestHandler manifestHandler = new ManifestHandler();

            //check all local files against the manifest for file size changes.
            //if the file is missing or the wrong size, download it.
            //better system - compare old & new manifests for changes and download those?
            List<ManifestEntry> Manifest = manifestHandler.Manifest;
            List<ManifestEntry> OldManifest = manifestHandler.OldManifest;

            try
            {
                //Check old manifest against new manifest, download anything that isn't exactly the same as before
                HTTPHandler HTTP = new HTTPHandler();
                HTTP.FileProgressChanged += OnDownloadProgressChanged;
                HTTP.FileDownloadFinished += OnFileDownloadFinished;

                foreach (ManifestEntry Entry in Manifest)
                {
                    if (!OldManifest.Contains(Entry))
                    {
                        string RemotePath = String.Format("{0}{1}",
                                                                  Config.GetGameURL(true),
                                                                  Entry.RelativePath);

                        string LocalPath = String.Format("{0}{1}",
                                               Config.GetGamePath(true),
                                               Entry.RelativePath);

                        Directory.CreateDirectory(Directory.GetParent(LocalPath).ToString());

                        OnProgressChanged();
                        HTTP.DownloadHTTPFile(RemotePath, LocalPath, false);
                    }
                }

                OnGameUpdateFinished();

                //clear out the event handlers
                HTTP.FileProgressChanged -= OnDownloadProgressChanged;
                HTTP.FileDownloadFinished -= OnFileDownloadFinished;
            }
            catch (IOException ioex)
            {
                Console.WriteLine("IOException in UpdateGameAsync(): " + ioex.Message);
                OnGameUpdateFailed();
            }
        }



        //TODO: Update this function to handle DLLs as well. May have to implement a full-blown
        //manifest system here as well.

        /// <summary>
        /// Downloads the manifest.
        /// </summary>
        public void DownloadManifest( string WhichManifest )
		{
			Stream manifestStream = null;														
			try
			{
				HTTPHandler HTTP = new HTTPHandler ();

				string remoteChecksum = HTTP.GetRemoteManifestChecksum ( WhichManifest );
				string localChecksum = "";

				string RemoteURL = Config.GetManifestURL ( WhichManifest );
				string LocalPath = ConfigHandler.GetManifestPath ();

				if (File.Exists(ConfigHandler.GetManifestPath()))
				{
					manifestStream = File.OpenRead (ConfigHandler.GetManifestPath ());
                    localChecksum = MD5Handler.GetFileHash(manifestStream);

					if (!(remoteChecksum == localChecksum))
					{
						//Copy the old manifest so that we can compare them when updating the game
						File.Copy(LocalPath, LocalPath + ".old", true);

						HTTP.DownloadHTTPFile (RemoteURL, LocalPath, false);
					}
				}
				else
				{
					HTTP.DownloadHTTPFile (RemoteURL, LocalPath, false);
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
			HTTPHandler HTTP = new HTTPHandler ();

			//load the HTML from the server as a string
			string content = HTTP.ReadHTTPFile (Config.GetChangelogURL ());
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

