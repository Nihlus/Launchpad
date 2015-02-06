using System;
using System.Threading;

namespace Launchpad_Launcher
{
	public class LauncherHandler
	{
		public delegate void ProgressChangedEventHandler(object sender, ProgressEventArgs e);
		public event ProgressChangedEventHandler ProgressChanged;

		public delegate void DownloadFinishedEventHandler(object sender, EventArgs e);
		public event DownloadFinishedEventHandler DownloadFinished;

		private ProgressEventArgs ProgressArgs;

		public LauncherHandler ()
		{

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
				DownloadFinished (this, EventArgs.Empty);
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
	}
}

