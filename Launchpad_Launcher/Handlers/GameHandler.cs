using System;

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

		public delegate void DownloadFinishedEventHandler(object sender, EventArgs e);
		public event DownloadFinishedEventHandler DownloadFinished;

		private ProgressEventArgs ProgressArgs;

		public GameHandler ()
		{
		}

		public void DownloadGame()
		{

		}
		private void DownloadGameAsync()
		{

		}

		public void UpdateGame()
		{

		}
		private void UpdateGameAsync()
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

