using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace Launchpad
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
		public event LaunchpadEventDelegates.GameProgressChangedEventHandler ProgressChanged;
		/// <summary>
			/// Occurs when download finishes.
			/// </summary>
		public event LaunchpadEventDelegates.GameDownloadFinishedEventHandler GameDownloadFinished;
		/// <summary>
		/// Occurs when game update finished.
		/// </summary>
		public event LaunchpadEventDelegates.GameUpdateFinishedEventHandler GameUpdateFinished;
		/// <summary>
		/// Occurs when game verification finishes.
		/// </summary>
		public event LaunchpadEventDelegates.GameRepairFinishedEventHandler GameRepairFinished;


		/// <summary>
		/// Occurs when the download failed.
		/// </summary>
		public event LaunchpadEventDelegates.GameDownloadFailedEventHander GameDownloadFailed;
		/// <summary>
		/// Occurs when game update failed.
		/// </summary>
		public event LaunchpadEventDelegates.GameUpdateFailedEventHandler GameUpdateFailed;
		/// <summary>
		/// Occurs when game repair failed.
		/// </summary>
		public event LaunchpadEventDelegates.GameRepairFailedEventHandler GameRepairFailed;
		/// <summary>
		/// Occurs when game launch failed.
		/// </summary>
		public event LaunchpadEventDelegates.GameLaunchFailedEventHandler GameLaunchFailed;
			
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


		/// <summary>
		/// The checks handler reference
		/// </summary>
		private ChecksHandler Checks = new ChecksHandler ();
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

				//create the .install file to mark that an installation has begun
				File.Create (Config.GetInstallCookie ()).Close();

				//write the current file progress to the install cookie
				TextWriter tw = new StreamWriter(Config.GetInstallCookie ());
				tw.WriteLine ("START");
				tw.Close ();

				//raise the progress changed event by binding to the 
				//event in the FTP class
				FTP.FileProgressChanged += OnDownloadProgressChanged;

				string lastDownloadedFile = File.ReadAllText (Config.GetInstallCookie ());
				string[] manifestEntries = File.ReadAllLines (Config.GetManifestPath ());

				//in order to be able to resume downloading, we check if there is a file
				//stored in the install cookie.
				int line = 0;

				if (!Checks.IsInstallCookieEmpty())
				{
					//loop through all the lines in the manifest until we encounter
					//a line which matches the one in the install cookie
					for (int i = 0; i < manifestEntries.Length; ++i)
					{
						if (lastDownloadedFile == manifestEntries[i])
						{
							line = i;
						}
					}
				}

				//then, start downloading files from that line. If no line was found, we start 
				//at 0.
				for (int i = line; i < manifestEntries.Length; ++i)
				{
					//download the file
					//this is the first substring in the manifest line, delimited by :
					string[] elements = manifestEntries [i].Split (':');
					string relativeFileName = elements[0];

					string remotePath = String.Format ("{0}/game/{1}{2}{3}", 
					                                   Config.GetFTPUrl (), 
					                                   Config.GetSystemTarget(), 
					                                   System.IO.Path.DirectorySeparatorChar,
					                                   relativeFileName);

					string localPath = String.Format ("{0}{1}{2}", 
					                                  Config.GetGamePath (true),
					                                  System.IO.Path.DirectorySeparatorChar, 
					                                  relativeFileName);

					Directory.CreateDirectory(Directory.GetParent(localPath).ToString());

					//write the current file progress to the install cookie
					TextWriter textWriterProgress = new StreamWriter(Config.GetInstallCookie ());
					textWriterProgress.WriteLine (manifestEntries [i]);
					textWriterProgress.Close ();

					if (File.Exists(localPath))
					{
						FileInfo fileInfo = new FileInfo(localPath);
						long manifestFileLength = 0;
						long.TryParse(elements[2], out manifestFileLength);
						if (fileInfo.Length == manifestFileLength)
						{
							//should resume download here

							//whoa, why is there a file here? Is it correct?
							MD5Handler MD5 = new MD5Handler();
							string localHash = MD5.GetFileHash(File.OpenRead(localPath));
							string manifestHash = elements[1];

							if (localHash == manifestHash)
							{
								//apparently we already had the proper version of this file. 
								//Moving on!
								continue;
							}
						}
					}


					//make sure we have a game directory to put files in
					Directory.CreateDirectory(Path.GetDirectoryName(localPath));
					//now download the file
					OnProgressChanged();
					fileReturn = FTP.DownloadFTPFile (remotePath, localPath, false);

					//if we're dealing with a file that should be executable, 
					bool bFileIsGameExecutable = (Path.GetFileName(localPath).EndsWith(".exe")) || (Path.GetFileNameWithoutExtension(localPath) == Config.GetGameName());

					if (Checks.IsRunningOnUnix() && bFileIsGameExecutable)
					{
						UnixHandler Unix = new UnixHandler();

						Unix.MakeExecutable(localPath);
					}
				}

				//we've finished the download, so empty the cookie
				File.WriteAllText (Config.GetInstallCookie (), String.Empty);

				//raise the finished event
				OnGameDownloadFinished ();			
			}
			catch (Exception ex)
			{
				Console.WriteLine ("InstallGameAsync(): " + ex.Message);

				DownloadFailedArgs.Result = "1";
				DownloadFailedArgs.Type = "Install";
				DownloadFinishedArgs.Metadata = fileReturn;

				OnGameDownloadFailed ();
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
			//check all local files against the manifest for file size changes.
			//if the file is missing or the wrong size, download it.
			//better system - compare old & new manifests for changes and download those?
			List<string> manifestItems = new List<string> ();
			List<string> oldManifestItems = new List<string> ();
			List<string> localFiles = new List<string> ();

			string manifestPath = Config.GetManifestPath ();
			string oldManifestPath = Config.GetManifestPath () + ".old";

			try
			{
				//fill our manifest lists
				manifestItems = new List<string> (File.ReadAllLines(manifestPath));

				//if we have an old manifest, load it
				if (File.Exists(oldManifestPath))
				{
					oldManifestItems = new List<string> (File.ReadAllLines(oldManifestPath));
				}
				//first check old manifest against new manifest, download anything that isn't exactly the same as before

				FTPHandler FTP = new FTPHandler();
				FTP.FileProgressChanged += OnDownloadProgressChanged;
				FTP.FileDownloadFinished += OnFileDownloadFinished;

				foreach (string item in manifestItems)
				{
					if (!oldManifestItems.Contains(item))
					{
						//download
						string ManifestFileName = (item.Split (':'))[0];

						string RemotePath = String.Format ("{0}/game/{1}{2}{3}", 
						                                   Config.GetFTPUrl (), 
						                                   Config.GetSystemTarget(), 
						                                   System.IO.Path.DirectorySeparatorChar,
						                                   ManifestFileName);

						string LocalPath = String.Format ("{0}{1}{2}", 
						                                  Config.GetGamePath (true),
						                                  System.IO.Path.DirectorySeparatorChar, 
						                                  ManifestFileName);

						OnProgressChanged();

						Directory.CreateDirectory(Directory.GetParent(LocalPath).ToString());

						FTP.DownloadFTPFile(RemotePath, LocalPath, false);
					}
				}

				OnGameUpdateFinished();
			}
			catch (Exception ex)
			{
				Console.WriteLine ("UpdateGameAsync(): " + ex.Message);
				OnGameUpdateFailed ();
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
			string fileReturn = "";
			try
			{
				//check all local file MD5s against latest manifest. Download broken files.
				FTPHandler FTP = new FTPHandler ();
				MD5Handler MD5 = new MD5Handler ();

				//bind our events
				FTP.FileProgressChanged += OnDownloadProgressChanged;


				//first, verify that the manifest is correct.
				string localsum = MD5.GetFileHash(File.OpenRead(Config.GetManifestPath ()));
				string remotesum = FTP.GetRemoteManifestChecksum ();

				//if it is not, download a new copy.
				if (!(localsum == remotesum))
				{
					LauncherHandler Launcher = new LauncherHandler ();
					Launcher.DownloadManifest ();
				}

				string[] entries = File.ReadAllLines (Config.GetManifestPath ());

				ProgressArgs.TotalFiles = entries.Length;
			
				int i = 0;
				foreach (string entry in entries)
				{
					string[] elements = entry.Split(':');

					string relativeFilePath = elements[0];
					string completeFilePath = Config.GetGamePath(true) + relativeFilePath;
					string manifestMD5 = elements [1];
					//string size = elements [2];

					ProgressArgs.Filename = Path.GetFileName(completeFilePath);

					string RemotePath = String.Format ("{0}/game/{1}{2}{3}", 
					                                   Config.GetFTPUrl (), 
					                                   Config.GetSystemTarget(), 
					                                   System.IO.Path.DirectorySeparatorChar,
					                                   relativeFilePath);

					string LocalPath = String.Format ("{0}{1}{2}", 
					                                  Config.GetGamePath (true),
					                                  System.IO.Path.DirectorySeparatorChar, 
					                                  relativeFilePath);

					Directory.CreateDirectory(Directory.GetParent(LocalPath).ToString());

					if (!File.Exists(completeFilePath))
					{
						//download the file, since it was missing
						OnProgressChanged ();
						fileReturn = FTP.DownloadFTPFile (RemotePath, LocalPath, false);
					}
					else
					{
						string fileMD5 = MD5.GetFileHash (File.OpenRead (completeFilePath));
						if (fileMD5 != manifestMD5)
						{
							Console.WriteLine (fileMD5 + ":" + manifestMD5);
							//download the file, since it was broken
							OnProgressChanged ();
							fileReturn = FTP.DownloadFTPFile (RemotePath, LocalPath, false);
						}
					}

					//if we're dealing with a file that should be executable, 
					bool bFileIsGameExecutable = (Path.GetFileName(LocalPath).EndsWith(".exe")) || (Path.GetFileNameWithoutExtension(LocalPath) == Config.GetGameName());

					if (Checks.IsRunningOnUnix() && bFileIsGameExecutable)
					{
						UnixHandler Unix = new UnixHandler();

						//if we couldn't set the execute bit on the executable, raise an exception, since we won't be able to launch the game
						if (!Unix.MakeExecutable(LocalPath))
						{
							throw new Exception("[LPAD001]: Could not set the execute bit on the game executable.");
						}
					}

					++i;
					ProgressArgs.DownloadedFiles = i;
					OnProgressChanged ();
				}
				OnGameRepairFinished ();
			}
			catch (Exception ex)
			{
				Console.WriteLine ("RepairGameAsync(): " + ex.Message);

				DownloadFailedArgs.Result = "1";
				DownloadFailedArgs.Type = "Repair";
				DownloadFailedArgs.Metadata = fileReturn;

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

				Process game = new Process();
				game.EnableRaisingEvents = true;

				game.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) 
				{
					Console.WriteLine (e.Data);
				};

				game.Exited += delegate(object sender, EventArgs e) 
				{
					Console.WriteLine("Game exited.");
				};


				Console.WriteLine (gameStartInfo.FileName);
				game = Process.Start(gameStartInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine ("LaunchGame(): " + ex.Message);
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
	}
}

