using System;
using System.IO;
using System.Threading;

/*
 * This class has a lot of async stuff going on. It handles installing the game
 * and updating it when it needs to.
 * Since this class starts new threads in which it does the larger computations,
 * there must be no useage of UI code in this class. Keep it clean!
 * 
 */

namespace Launchpad_Launcher
{
	public class GameHandler
	{
		public delegate void ProgressChangedEventHandler(object sender, ProgressEventArgs e);
		public event ProgressChangedEventHandler ProgressChanged;

		public delegate void DownloadFinishedEventHandler(object sender, DownloadFinishedEventArgs e);
		public event DownloadFinishedEventHandler DownloadFinished;

		private ProgressEventArgs ProgressArgs;
		private DownloadFinishedEventArgs DownloadFinishedArgs;
		//Checks handler
		ChecksHandler Checks = new ChecksHandler ();
		//config handler
		ConfigHandler Config = new ConfigHandler ();

		public GameHandler ()
		{
			ProgressArgs = new ProgressEventArgs ();
			DownloadFinishedArgs = new DownloadFinishedEventArgs ();
		}

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
					TextWriter tw = new StreamWriter(Config.GetInstallCookie ());
					tw.WriteLine (ManifestFiles [i]);
					tw.Close ();

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
					//raise the progress changed event by binding to the 
					//event in the FTP class
					FTP.FileProgressChanged += OnDownloadProgressChanged;

					//make sure we have a game directory to put files in
					Directory.CreateDirectory(Path.GetDirectoryName(LocalPath));
					//now download the file
					FTP.DownloadFTPFile (RemotePath, LocalPath, false);
				}

				//we've finished the download, so empty the cookie
				File.WriteAllText (Config.GetInstallCookie (), String.Empty);

				//raise the finished event
				OnDownloadFinished ();			
			}
			catch (Exception ex)
			{
				Console.WriteLine ("InstallGameAsync(): " + ex.StackTrace);
				DownloadFinishedArgs.Value = "1";
				DownloadFinishedArgs.URL = "Install";

				OnDownloadFinished ();
			}		
		}

		public void UpdateGame()
		{

		}
		private void UpdateGameAsync()
		{

		}

		public void RepairGame()
		{

		}
		private void RepairGameAsync()
		{

		}

		public void LaunchGame()
		{

		}

		protected void OnDownloadProgressChanged(object sender, ProgressEventArgs e)
		{
			ProgressArgs = e;
			OnProgressChanged ();
		}

		protected virtual void OnProgressChanged()
		{
			if (ProgressChanged != null)
			{
				ProgressChanged (this, ProgressArgs);
			}
		}

		protected virtual void OnDownloadFinished()
		{
			if (DownloadFinished != null)
			{
				DownloadFinished (this, DownloadFinishedArgs);
			}
		}
	}
}

