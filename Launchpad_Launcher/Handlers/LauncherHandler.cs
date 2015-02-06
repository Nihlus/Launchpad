using System;
using System.Threading;

namespace Launchpad_Launcher
{
	public class LauncherHandler
	{
		public delegate void ProgressChangedEventHandler(object sender, ProgressEventArgs e);
		public event ProgressChangedEventHandler LauncherProgressChanged;

		public delegate void DownloadFinishedEventHandler (object sender, DownloadFinishedEventArgs e);
		public event DownloadFinishedEventHandler LauncherDownloadFinished;

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
		/// Updates the launcher asynchronously.
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

		protected virtual void OnLauncherProgressChanged()
		{
			if (LauncherProgressChanged != null)
			{
				//raise the event
				LauncherProgressChanged (this, ProgressArgs);
			}
		}

		protected virtual void OnLauncherDownloadFinished()
		{
			if (LauncherDownloadFinished != null)
			{
				//raise the event
				LauncherDownloadFinished (this, DownloadFinishedArgs);
			}
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
				Console.WriteLine (DownloadFinishedArgs.Value);
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

		public void Empty()
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

		public void Empty()
		{
			Value = "";
		}
	}
}

