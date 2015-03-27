using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace Launchpad_Launcher
{
	/// <summary>
	///  This class has a lot of async stuff going on. It handles installing the game
	///  and updating it when it needs to.
	///	 Since this class starts new threads in which it does the larger computations,
	///	 there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	public sealed class GameHandler
	{
		public delegate void ProgressChangedEventHandler(object sender, ProgressEventArgs e);
		/// <summary>
		/// Occurs when progress changed.
		/// </summary>
		public event ProgressChangedEventHandler ProgressChanged;

		public delegate void DownloadFinishedEventHandler (object sender, DownloadFinishedEventArgs e);
		/// <summary>
		/// Occurs when download finishes.
		/// </summary>
		public event DownloadFinishedEventHandler DownloadFinished;

		public delegate void VerificationFinishedEventHandler (object sender, EventArgs e);
		/// <summary>
		/// Occurs when game verification finishes.
		/// </summary>
		public event VerificationFinishedEventHandler VerificationFinished;

		public delegate void GameLaunchFailedEventHandler (object sender, EventArgs e);
		/// <summary>
		/// Occurs when game launch failed.
		/// </summary>
		public event GameLaunchFailedEventHandler GameLaunchFailed;

		public delegate void GameDownloadFailedEventHander (object sender, DownloadFailedEventArgs e);
		/// <summary>
		/// Occurs when the download failed.
		/// </summary>
		public event GameDownloadFailedEventHander GameDownloadFailed;


		/// <summary>
		/// The progress arguments object. Is updated during file download operations.
		/// </summary>
		private ProgressEventArgs ProgressArgs;
		/// <summary>
		/// The download finished arguments object. Is updated once a file download finishes.
		/// </summary>
		private DownloadFinishedEventArgs DownloadFinishedArgs;
		/// <summary>
		/// The download failed arguments.
		/// </summary>
		private DownloadFailedEventArgs DownloadFailedArgs;


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
			ProgressArgs = new ProgressEventArgs ();
			DownloadFinishedArgs = new DownloadFinishedEventArgs ();
			DownloadFailedArgs = new DownloadFailedEventArgs ();
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

				string LastFile = File.ReadAllText (Config.GetInstallCookie ());
				string[] ManifestFiles = File.ReadAllLines (Config.GetManifestPath ());

				//in order to be able to resume downloading, we check if there is a file
				//stored in the install cookie.
				int line = 0;

				if (Checks.IsInstallCookieEmpty())
				{
					//loop through all the lines in the manifest until we encounter
					//a line which matches the one in the install cookie
					for (int i = 0; i < ManifestFiles.Length; ++i)
					{
						if (LastFile == ManifestFiles[i])
						{
							line = i;
						}
					}
				}

				//then, start downloading files from that line. If no line was found, we start 
				//at 0.
				for (int i = line; i < ManifestFiles.Length; ++i)
				{
					//download the file
					//this is the first substring in the manifest line, delimited by :
					string ManifestFileName = (ManifestFiles [i].Split (':'))[0];

					string RemotePath = String.Format ("{0}/game/{1}{2}", 
					                                   Config.GetFTPUrl (), 
					                                   Config.GetSystemTarget(), 
					                                   ManifestFileName);

					string LocalPath = String.Format ("{0}{1}{2}", 
					                                  Config.GetGamePath (),
					                                  System.IO.Path.DirectorySeparatorChar, 
					                                  ManifestFileName);

					//write the current file progress to the install cookie
					TextWriter twp = new StreamWriter(Config.GetInstallCookie ());
					twp.WriteLine (ManifestFiles [i]);
					twp.Close ();

					if (File.Exists(LocalPath))
					{
						//whoa, why is there a file here? Is it correct?
						MD5Handler MD5 = new MD5Handler();
						string localHash = MD5.GetFileHash(File.OpenRead(LocalPath));
						string manifestHash = (ManifestFiles [i].Split (':'))[1];
						if (localHash == manifestHash)
						{
							//apparently we already had the proper version of this file. 
							//Moving on!
							continue;
						}
					}


					//make sure we have a game directory to put files in
					Directory.CreateDirectory(Path.GetDirectoryName(LocalPath));
					//now download the file
					OnProgressChanged();
					fileReturn = FTP.DownloadFTPFile (RemotePath, LocalPath, false);

					//if we're dealing with a file that should be executable, 
					bool bFileIsGameExecutable = (Path.GetFileName(LocalPath).EndsWith(".exe")) || (Path.GetFileNameWithoutExtension(LocalPath) == Config.GetGameName());

					if (Checks.IsRunningOnUnix() && bFileIsGameExecutable)
					{
						UnixHandler Unix = new UnixHandler();

						Unix.MakeExecutable(LocalPath);
					}
				}

				//we've finished the download, so empty the cookie
				File.WriteAllText (Config.GetInstallCookie (), String.Empty);

				//raise the finished event
				OnDownloadFinished ();			
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

					string filepath = Config.GetGamePath() + elements [0];
					string manifestMD5 = elements [1];
					//string size = elements [2];

					ProgressArgs.Filename = Path.GetFileName(filepath);

					string RemotePath = String.Format ("{0}/game/{1}{2}", 
					                                   Config.GetFTPUrl (), 
					                                   Config.GetSystemTarget(), 
					                                   elements[0]);

					string LocalPath = String.Format ("{0}{1}{2}", 
					                                  Config.GetGamePath (),
					                                  System.IO.Path.DirectorySeparatorChar, 
					                                  elements[0]);

					if (!File.Exists(filepath))
					{
						//download the file, since it was missing
						OnProgressChanged ();
						fileReturn = FTP.DownloadFTPFile (RemotePath, LocalPath, false);
					}
					else
					{
						string fileMD5 = MD5.GetFileHash (File.OpenRead (filepath));
						if (fileMD5 != manifestMD5)
						{
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
				OnVerificationFinished ();
			}
			catch (Exception ex)
			{
				Console.WriteLine ("RepairGameAsync(): " + ex.Message);

				DownloadFailedArgs.Result = "1";
				DownloadFailedArgs.Type = "Repair";
				DownloadFailedArgs.Metadata = fileReturn;

				OnGameDownloadFailed ();
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
				gameStartInfo.FileName = Config.GetGameExecutable ();
				if (!File.Exists(Config.GetGameExecutable()))
				{
					throw new FileNotFoundException();
				}

				Process.Start (gameStartInfo);
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
		private void OnDownloadProgressChanged(object sender, ProgressEventArgs e)
		{
			ProgressArgs = e;
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
		private void OnDownloadFinished()
		{
			if (DownloadFinished != null)
			{
				DownloadFinished (this, DownloadFinishedArgs);
			}
		}

		private void OnVerificationFinished()
		{
			if (VerificationFinished != null)
			{
				VerificationFinished (this, EventArgs.Empty);
			}
		}

		private void OnGameLaunchFailed()
		{
			if (GameLaunchFailed != null)
			{
				GameLaunchFailed (this, EventArgs.Empty);
			}
		}

		private void OnGameDownloadFailed()
		{
			if (GameDownloadFailed != null)
			{
				GameDownloadFailed (this, DownloadFailedArgs);
			}
		}
	}
}

