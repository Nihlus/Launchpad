using System;
using System.Threading;

/*
 * This class has a lot of async stuff going on. It handles updating the launcher
 * and loading the changelog from the server.
 * Since this class starts new threads in which it does the larger computations,
 * there must be no useage of UI code in this class. Keep it clean!
 * 
 */

namespace Launchpad_Launcher
{
	public class LauncherHandler
	{

		public delegate void ChangelogProgressChangedEventHandler(object sender, ProgressEventArgs e);
		public event ChangelogProgressChangedEventHandler ChangelogProgressChanged;

		public delegate void ChangelogDownloadFinishedEventHandler (object sender, DownloadFinishedEventArgs e);
		public event ChangelogDownloadFinishedEventHandler ChangelogDownloadFinished;


		private ProgressEventArgs ProgressArgs;
		private DownloadFinishedEventArgs DownloadFinishedArgs;

		ConfigHandler Config = new ConfigHandler ();

		public LauncherHandler ()
		{
			ProgressArgs = new ProgressEventArgs ();
			DownloadFinishedArgs = new DownloadFinishedEventArgs ();
		}

		/// <summary>
		/// Updates the launcher synchronously.
		/// </summary>
		public void UpdateLauncher()
		{

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
			string content = FTP.ReadFTPFile (Config.GetChangelogURL ());

			DownloadFinishedArgs.Value = content;
			DownloadFinishedArgs.URL = Config.GetChangelogURL ();

			OnChangelogDownloadFinished ();
		}

		protected virtual void OnChangelogProgressChanged()
		{
			if (ChangelogProgressChanged != null)
			{
				//raise the event
				ChangelogProgressChanged (this, ProgressArgs);
			}
		}

		protected virtual void OnChangelogDownloadFinished()
		{
			if (ChangelogDownloadFinished != null)
			{
				//raise the event
				ChangelogDownloadFinished (this, DownloadFinishedArgs);
			}
		}
	}

	public class ProgressEventArgs : EventArgs
	{
		public int DownloadedBytes {
			get;
			set;
		}

		public int TotalBytes {
			get;
			set;
		}

		public int DownloadedFiles {
			get;
			set;
		}

		public int TotalFiles {
			get;
			set;
		}

		public string Filename {
			get;
			set;
		}

		new public void Empty()
		{
			DownloadedBytes = 0;
			TotalBytes = 0;
			DownloadedFiles = 0;
			TotalFiles = 0;
			Filename = "";
		}
	}

	public class DownloadFinishedEventArgs : EventArgs
	{
		public string Value {
			get;
			set;
		}

		public string URL {
			get;
			set;
		}

		new public void Empty()
		{
			Value = "";
		}
	}
}

