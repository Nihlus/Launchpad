using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

/*
 * This class has a lot of async stuff going on. It handles installing the game
 * and updating it when it needs to.
 * Since this class starts new threads in which it does the larger computations,
 * there must be no useage of UI code in this class. Keep it clean!
 * 
 */

namespace Launchpad_Launcher
{
	public sealed class GameHandler
	{
		public delegate void ProgressChangedEventHandler(object sender, ProgressEventArgs e);
		/// <summary>
		/// Occurs when progress changed.
		/// </summary>
		public event ProgressChangedEventHandler ProgressChanged;

		public delegate void DownloadFinishedEventHandler(object sender, DownloadFinishedEventArgs e);
		/// <summary>
		/// Occurs when download finishes.
		/// </summary>
		public event DownloadFinishedEventHandler DownloadFinished;

		private ProgressEventArgs ProgressArgs;
		private DownloadFinishedEventArgs DownloadFinishedArgs;

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
					FTP.DownloadFTPFile (RemotePath, LocalPath, false);
				}

				//we've finished the download, so empty the cookie
				File.WriteAllText (Config.GetInstallCookie (), String.Empty);

				//raise the finished event
				OnDownloadFinished ();			
			}
			catch (Exception ex)
			{
				Console.WriteLine ("InstallGameAsync(): " + ex.Message);
				DownloadFinishedArgs.Value = "1";
				DownloadFinishedArgs.URL = "Install";

				OnDownloadFinished ();
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
			//check all local file MD5s against latest manifest. Download broken files.
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

				Process.Start (gameStartInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine ("LaunchGame(): " + ex.Message);
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
	}
}

