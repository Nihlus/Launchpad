using System;

namespace Launchpad_Launcher
{
	/// <summary>
	/// Progress event arguments.
	/// </summary>
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

	/// <summary>
	/// Download finished event arguments.
	/// </summary>
	public class DownloadFinishedEventArgs : EventArgs
	{
		public string Result {
			get;
			set;
		}

		public string Type {
			get;
			set;
		}

		public string Metadata {
			get;
			set;
		}

		new public void Empty()
		{
			Result = "";
			Metadata = "";
		}
	}

	/// <summary>
	/// Download failed event arguments.
	/// </summary>
	public class DownloadFailedEventArgs : EventArgs
	{
		public string Result {
			get;
			set;
		}

		public string Type {
			get;
			set;
		}

		public string Metadata {
			get;
			set;
		}

		new public void Empty()
		{
			Result = "";
			Metadata = "";
		}
	}
}

