using System;

namespace Launchpad_Launcher
{
	public class GameHandler
	{
		public delegate void ProgressChangedEventHandler(object sender, ProgressEventArgs e);
		public event ProgressChangedEventHandler ProgressChanged;

		public delegate void DownloadFinishedEventHandler(object sender, EventArgs e);
		public event DownloadFinishedEventHandler DownloadFinished;

		private ProgressEventArgs ProgressArgs;

		public GameHandler ()
		{
		}

		public void DownloadGame()
		{

		}

		public void UpdateGame()
		{

		}

		public void LaunchGame()
		{

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
}

