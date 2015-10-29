using System;
using System.IO;

namespace Launchpad.Utilities.Events.Arguments
{
	public class ManifestGenerationProgressChangedEventArgs : EventArgs
	{
		public string Filepath
		{
			get;
			set;
		}

		public int TotalFiles
		{
			get;
			set;
		}

		public int CompletedFiles
		{
			get;
			set;
		}

		public string MD5
		{
			get;
			set;
		}

		public long Filesize
		{
			get;
			set;
		}

		new public void Empty ()
		{
			Filepath = null;
			TotalFiles = 0;
			CompletedFiles = 0;
			MD5 = "";
			Filesize = 0;
		}
	}
}

