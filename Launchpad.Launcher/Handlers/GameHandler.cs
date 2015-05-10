using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Launchpad.Launcher.Events.Arguments;
using Launchpad.Launcher.Events.Delegates;

namespace Launchpad.Launcher
{
	/// <summary>
	///  This class has a lot of async stuff going on. It handles installing the game
	///  and updating it when it needs to.
	///	 Since this class starts new threads in which it does the larger computations,
	///	 there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class GameHandler
	{
		/// <summary>
		/// Occurs when progress changed.
		/// </summary>
		public event GameProgressChangedEventHandler ProgressChanged;
		/// <summary>
			/// Occurs when download finishes.
			/// </summary>
		public event GameDownloadFinishedEventHandler GameDownloadFinished;
		/// <summary>
		/// Occurs when game update finished.
		/// </summary>
		public event GameUpdateFinishedEventHandler GameUpdateFinished;
		/// <summary>
		/// Occurs when game verification finishes.
		/// </summary>
		public event GameRepairFinishedEventHandler GameRepairFinished;


		/// <summary>
		/// Occurs when the download failed.
		/// </summary>
		public event GameDownloadFailedEventHander GameDownloadFailed;
		/// <summary>
		/// Occurs when game update failed.
		/// </summary>
		public event GameUpdateFailedEventHandler GameUpdateFailed;
		/// <summary>
		/// Occurs when game repair failed.
		/// </summary>
		public event GameRepairFailedEventHandler GameRepairFailed;
		/// <summary>
		/// Occurs when game launch failed.
		/// </summary>
		public event GameLaunchFailedEventHandler GameLaunchFailed;

		public event GameExitEventHandler GameExited;
			
		//Progress event arguments
		/// <summary>
		/// The progress arguments object. Is updated during file download operations.
		/// </summary>
		private FileDownloadProgressChangedEventArgs ProgressArgs;

		//Success event arguments
		/// <summary>
		/// The download finished arguments object. Is updated once a file download finishes.
		/// </summary>
		private GameDownloadFinishedEventArgs DownloadFinishedArgs;
		/// <summary>
		/// The update finished arguments.
		/// </summary>
		private GameUpdateFinishedEventArgs UpdateFinishedArgs;
		/// <summary>
		/// The repair finished arguments.
		/// </summary>
		private GameRepairFinishedEventArgs RepairFinishedArgs;

		//Failure event arguments
		/// <summary>
		/// The download failed arguments.                
		private GameDownloadFailedEventArgs DownloadFailedArgs;
		/// <summary>
		/// The update failed arguments.
		/// </summary>
		private GameUpdateFailedEventArgs UpdateFailedArgs;
		/// <summary>
		/// The repaired failed arguments.
		/// </summary>
		private GameRepairFailedEventArgs RepairFailedArgs;
		/// <summary>
		/// The launch failed arguments.
		/// </summary>
		private GameLaunchFailedEventArgs LaunchFailedArgs;

		private GameExitEventArgs GameExitArgs;


		/// <summary>
		/// The config handler reference.
		/// </summary>
		private ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.GameHandler"/> class.
		/// </summary>
		public GameHandler ()
		{
			ProgressArgs = new FileDownloadProgressChangedEventArgs ();
			DownloadFinishedArgs = new GameDownloadFinishedEventArgs ();
			UpdateFinishedArgs = new GameUpdateFinishedEventArgs ();
			RepairFinishedArgs = new GameRepairFinishedEventArgs ();


			DownloadFailedArgs = new GameDownloadFailedEventArgs ();
			UpdateFailedArgs = new GameUpdateFailedEventArgs ();
			RepairFailedArgs = new GameRepairFailedEventArgs ();
			LaunchFailedArgs = new GameLaunchFailedEventArgs ();

			GameExitArgs = new GameExitEventArgs ();
		}

		/// <summary>
		/// Starts an asynchronous game installation task.
		/// </summary>
		public void InstallGame()
		{
			Thread t = new Thread (InstallGameAsync);
			t.Start ();
		}
		private void InstallGameAsync()
		{
			//This value is filled with either a path to the last downloaded file, or with an exception message
			//this message is used in the main UI to determine how it responds to a failed download.
			string fileReturn = "";
			try
			{
				FTPHandler FTP = new FTPHandler ();
				ManifestHandler manifestHandler = new ManifestHandler();
				List<ManifestEntry> Manifest = manifestHandler.Manifest;

				//create the .install file to mark that an installation has begun
				//if it exists, do nothing.
                ConfigHandler.CreateInstallCookie();


				//raise the progress changed event by binding to the 
				//event in the FTP class
				FTP.FileProgressChanged += OnDownloadProgressChanged;

				//in order to be able to resume downloading, we check if there is an entry
				//stored in the install cookie.
				ManifestEntry lastDownloadedFile = null;
				string installCookiePath = ConfigHandler.GetInstallCookiePath ();

				//attempt to parse whatever is inside the install cookie
				if (ManifestEntry.TryParse(File.ReadAllText (installCookiePath), out lastDownloadedFile))
				{
					//loop through all the entries in the manifest until we encounter
					//an entry which matches the one in the install cookie

					foreach (ManifestEntry Entry in Manifest)
					{
						if (lastDownloadedFile == Entry)
						{
							//remove all entries before the one we were last at.
							Manifest.RemoveRange(0, Manifest.IndexOf(Entry));
						}
					}
				}

				//then, start downloading the entries that remain in the manifest.
				foreach (ManifestEntry Entry in Manifest)
				{
					string RemotePath = String.Format ("{0}{1}", 
					                                   Config.GetGameURL (true), 
					                                   Entry.RelativePath);

					string LocalPath = String.Format ("{0}{1}{2}", 
					                                  Config.GetGamePath (true),
					                                  System.IO.Path.DirectorySeparatorChar, 
					                                  Entry.RelativePath);

					//make sure we have a game directory to put files in
					Directory.CreateDirectory(Path.GetDirectoryName(LocalPath));

					//write the current file progress to the install cookie
					TextWriter textWriterProgress = new StreamWriter(ConfigHandler.GetInstallCookiePath ());
					textWriterProgress.WriteLine (Entry.ToString());
					textWriterProgress.Close ();

					if (File.Exists(LocalPath))
					{
						FileInfo fileInfo = new FileInfo(LocalPath);
						if (fileInfo.Length != Entry.Size)
						{
							//Resume the download of this partial file.
							OnProgressChanged();
							fileReturn = FTP.DownloadFTPFile(RemotePath, LocalPath, fileInfo.Length, false);

							//Now verify the file
							string localHash = MD5Handler.GetFileHash(File.OpenRead(LocalPath));

							if (localHash != Entry.Hash)
							{
								Console.WriteLine ("InstallGameAsync: Resumed file hash was invalid, downloading fresh copy from server.");
								OnProgressChanged();
								fileReturn = FTP.DownloadFTPFile(RemotePath, LocalPath, false);
							}
						}									
					}
                    else
                    {
                        //no file, download it
                        OnProgressChanged();
                        fileReturn = FTP.DownloadFTPFile(RemotePath, LocalPath, false);
                    }					

					if (ChecksHandler.IsRunningOnUnix())
					{
						//if we're dealing with a file that should be executable, 
						string gameName = Config.GetGameName();
						bool bFileIsGameExecutable = (Path.GetFileName(LocalPath).EndsWith(".exe")) || (Path.GetFileNameWithoutExtension(LocalPath) == gameName);
						if (bFileIsGameExecutable)
						{
							//set the execute bits
							UnixHandler.MakeExecutable(LocalPath);
						}					
					}
				}

				//we've finished the download, so empty the cookie
				File.WriteAllText (ConfigHandler.GetInstallCookiePath (), String.Empty);

				//raise the finished event
				OnGameDownloadFinished ();	

				//clear out the event handler
				FTP.FileProgressChanged -= OnDownloadProgressChanged;
			}
	        catch (IOException ioex)
            {
                Console.WriteLine("IOException in InstallGameAsync(): " + ioex.Message);

                DownloadFailedArgs.Result = "1";
                DownloadFailedArgs.ResultType = "Install";
                DownloadFinishedArgs.Metadata = fileReturn;

                OnGameDownloadFailed();
            }
		}

		/// <summary>
		/// Starts an asynchronous game update task.
		/// </summary>
		public void UpdateGame()
		{
			Thread t = new Thread (UpdateGameAsync);
			t.Start ();
		}
		private void UpdateGameAsync()
		{
			ManifestHandler manifestHandler = new ManifestHandler ();

			//check all local files against the manifest for file size changes.
			//if the file is missing or the wrong size, download it.
			//better system - compare old & new manifests for changes and download those?
			List<ManifestEntry> Manifest = manifestHandler.Manifest;
			List<ManifestEntry> OldManifest = manifestHandler.OldManifest;

			try
			{
				//Check old manifest against new manifest, download anything that isn't exactly the same as before
				FTPHandler FTP = new FTPHandler();
				FTP.FileProgressChanged += OnDownloadProgressChanged;
				FTP.FileDownloadFinished += OnFileDownloadFinished;

				foreach (ManifestEntry Entry in Manifest)
				{
					if (!OldManifest.Contains(Entry))
					{
                        string RemotePath = String.Format("{0}{1}",
                                                       	  Config.GetGameURL(true),
                                                          Entry.RelativePath);

						string LocalPath = String.Format ("{0}{1}", 
						                                  Config.GetGamePath (true),
						                                  Entry.RelativePath);

						Directory.CreateDirectory(Directory.GetParent(LocalPath).ToString());

						OnProgressChanged();
						FTP.DownloadFTPFile(RemotePath, LocalPath, false);
					}
				}

				OnGameUpdateFinished();

				//clear out the event handlers
				FTP.FileProgressChanged -= OnDownloadProgressChanged;
				FTP.FileDownloadFinished -= OnFileDownloadFinished;
			}
            catch (IOException ioex)
            {
                Console.WriteLine("IOException in UpdateGameAsync(): " + ioex.Message);
                OnGameUpdateFailed();
            }
		}

		/// <summary>
		/// Starts an asynchronous game repair task.
		/// </summary>
		public void RepairGame()
		{
			Thread t = new Thread (RepairGameAsync);
			t.Start ();
		}
		private void RepairGameAsync()
		{
			//This value is filled with either a path to the last downloaded file, or with an exception message
			//this message is used in the main UI to determine how it responds to a failed download.
			string repairMetadata = "";
			try
			{
				//check all local file MD5s against latest manifest. Resume partial files, download broken files.
				FTPHandler FTP = new FTPHandler ();

				//bind event handlers
				FTP.FileProgressChanged += OnDownloadProgressChanged;

				//first, verify that the manifest is correct.
				string LocalManifestHash = MD5Handler.GetFileHash(File.OpenRead(ConfigHandler.GetManifestPath ()));
				string RemoteManifestHash = FTP.GetRemoteManifestChecksum ();

				//if it is not, download a new copy.
				if (!(LocalManifestHash == RemoteManifestHash))
				{
					LauncherHandler Launcher = new LauncherHandler ();
					Launcher.DownloadManifest ();
				}

				//then, begin repairing the game
				ManifestHandler manifestHandler = new ManifestHandler ();
				List<ManifestEntry> Manifest = manifestHandler.Manifest;			

				ProgressArgs.TotalFiles = Manifest.Count;
			
				int i = 0;
				foreach (ManifestEntry Entry in Manifest)
				{				


                    string RemotePath = String.Format("{0}{1}",
                                                       Config.GetGameURL(true),
                                                       Entry.RelativePath);

					string LocalPath = String.Format ("{0}{1}", 
					                                  Config.GetGamePath (true),
					                                  Entry.RelativePath);

					ProgressArgs.FileName = Path.GetFileName(LocalPath);

					//make sure the directory for the file exists
					Directory.CreateDirectory(Directory.GetParent(LocalPath).ToString());

					if (File.Exists(LocalPath))
					{

						FileInfo fileInfo = new FileInfo(LocalPath);
						if (fileInfo.Length != Entry.Size)
						{
							//Resume the download of this partial file.
							OnProgressChanged();
							repairMetadata = FTP.DownloadFTPFile(RemotePath, LocalPath, fileInfo.Length, false);

							//Now verify the file
							string localHash = MD5Handler.GetFileHash(File.OpenRead(LocalPath));

							if (localHash != Entry.Hash)
							{
								Console.WriteLine ("RepairGameAsync: Resumed file hash was invalid, downloading fresh copy from server.");

								//download the file, since it was broken
								OnProgressChanged ();
								repairMetadata = FTP.DownloadFTPFile (RemotePath, LocalPath, false);
							}
						}					
					}
					else
					{
						//download the file, since it was missing
						OnProgressChanged ();
						repairMetadata = FTP.DownloadFTPFile (RemotePath, LocalPath, false);
					}

					if (ChecksHandler.IsRunningOnUnix())
					{
						//if we're dealing with a file that should be executable, 
						string gameName = Config.GetGameName();
						bool bFileIsGameExecutable = (Path.GetFileName(LocalPath).EndsWith(".exe")) || (Path.GetFileNameWithoutExtension(LocalPath) == gameName);
						if (bFileIsGameExecutable)
						{
							//set the execute bits.																
							UnixHandler.MakeExecutable(LocalPath);
						}
					}

					++i;
					ProgressArgs.DownloadedFiles = i;
					OnProgressChanged ();
				}

				OnGameRepairFinished ();

				//clear out the event handler
				FTP.FileProgressChanged -= OnDownloadProgressChanged;
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in RepairGameAsync(): " + ioex.Message);

				DownloadFailedArgs.Result = "1";
				DownloadFailedArgs.ResultType = "Repair";
				DownloadFailedArgs.Metadata = repairMetadata;

				OnGameRepairFailed ();
			}
		}

		/// <summary>
		/// Launches the game.
		/// </summary>
		public void LaunchGame()
		{
			//start new process of the game executable
			try
			{
				ProcessStartInfo gameStartInfo = new ProcessStartInfo ();
				gameStartInfo.UseShellExecute = false;
				gameStartInfo.FileName = Config.GetGameExecutable ();
				GameExitArgs.GameName = Config.GetGameName ();

				Process game = Process.Start (gameStartInfo);
				game.EnableRaisingEvents = true;

				game.Exited += delegate(object sender, EventArgs e) 
				{					
					GameExitArgs.ExitCode = game.ExitCode;
					OnGameExited ();
				};					
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in LaunchGame(): " + ioex.Message);
				OnGameLaunchFailed ();
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
			OnProgressChanged ();
		}

		private void OnFileDownloadFinished(object sender, FileDownloadFinishedEventArgs e)
		{
			OnProgressChanged ();
		}

		/// <summary>
		/// Raises the progress changed event.
		/// </summary>
		private void OnProgressChanged()
		{
			if (ProgressChanged != null)
			{
				ProgressChanged (this, ProgressArgs);
			}
		}

		/// <summary>
		/// Raises the download finished event.
		/// </summary>
		private void OnGameDownloadFinished()
		{
			if (GameDownloadFinished != null)
			{
				GameDownloadFinished (this, DownloadFinishedArgs);
			}
		}

		private void OnGameUpdateFinished()
		{
			if (GameUpdateFinished != null)
			{
				GameUpdateFinished (this, UpdateFinishedArgs);
			}
		}

		private void OnGameRepairFinished()
		{
			if (GameRepairFinished != null)
			{
				GameRepairFinished (this, RepairFinishedArgs);
			}
		}

		private void OnGameLaunchFailed()
		{
			if (GameLaunchFailed != null)
			{
				GameLaunchFailed (this, LaunchFailedArgs);
			}
		}

		private void OnGameDownloadFailed()
		{
			if (GameDownloadFailed != null)
			{
				GameDownloadFailed (this, DownloadFailedArgs);
			}
		}

		private void OnGameUpdateFailed()
		{
			if (GameUpdateFailed != null)
			{
				GameUpdateFailed (this, UpdateFailedArgs);
			}
		}

		private void OnGameRepairFailed()
		{
			if (GameRepairFailed != null)
			{
				GameRepairFailed (this, RepairFailedArgs);
			}
		}

		private void OnGameExited()
		{
			if (GameExited != null)
			{
				GameExited (this, GameExitArgs);
			}
		}
	}	   
}

