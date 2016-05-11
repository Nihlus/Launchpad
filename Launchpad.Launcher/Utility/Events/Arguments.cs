using System;

namespace Launchpad.Launcher.Events.Arguments
{	
	//FTP arguments for progress events
	/// <summary>
	/// Progress event arguments.
	/// </summary>
    public class FileDownloadProgressChangedEventArgs : EventArgs
	{
		public long DownloadedBytes 
		{
			get;
			set;
		}

        public long TotalBytes 
		{
			get;
			set;
		}

		public int DownloadedFiles 
		{
			get;
			set;
		}

		public int TotalFiles 
		{
			get;
			set;
		}

		public string FileName 
		{
			get;
			set;
		}

		new public void Empty()
		{
			DownloadedBytes = 0;
			TotalBytes = 0;
			DownloadedFiles = 0;
			TotalFiles = 0;
			FileName = "";
		}
	}

    public class FileDownloadFinishedEventArgs : EventArgs
	{
		public int Result
		{
			get;
			set;
		}

		public string FileName 
		{
			get;
			set;
		}
	}

	//Game arguments for success events
	// * Download
	// * Update
	// * Repair

	/// <summary>
	/// Download finished event arguments.
	/// </summary>
    public class GameDownloadFinishedEventArgs : EventArgs
	{
		public string Result {
			get;
			set;
		}

		public string ResultType 
        {
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

    public class GameUpdateFinishedEventArgs : EventArgs
	{
		public string Result {
			get;
			set;
		}

		public string ResultType {
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

    public class GameRepairFinishedEventArgs : EventArgs
	{
		public string Result {
			get;
			set;
		}

		public string ResultType {
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

	//Game arguments for failure events
	// * Download
	// * Update
	// * Repair
	// * Launch

	/// <summary>
	/// Download failed event arguments.
	/// </summary>
    public class GameDownloadFailedEventArgs : EventArgs
	{
		public string Result {
			get;
			set;
		}

		public string ResultType {
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

    public class GameUpdateFailedEventArgs : EventArgs
	{
		public string Result
		{
			get;
			set;
		}

		public string Metadata
		{
			get;
			set;
		}
	}

    public class GameRepairFailedEventArgs : EventArgs
	{
		public string Result
		{
			get;
			set;
		}

		public string Metadata
		{
			get;
			set;
		}
	}

    public class GameLaunchFailedEventArgs : EventArgs
	{
		public string Result
		{
			get;
			set;
		}

		public string Metadata
		{
			get;
			set;
		}
	}

	public class GameExitEventArgs : EventArgs
	{
		public string GameName
		{
			get;
			set;
		}

		public int ExitCode
		{
			get;
			set;
		}
	}
}

