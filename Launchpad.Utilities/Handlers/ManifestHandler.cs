using System;
using Launchpad.Utilities.Events.Delegates;
using Launchpad.Utilities.Events.Arguments;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Launchpad.Utilities.Handlers;

namespace Launchpad.Utilities
{
	public class ManifestHandler
	{
		public event ManifestGenerationProgressChangedEventHandler ManifestGenerationProgressChanged;
		public event ManifestGenerationFinishedEventHandler ManifestGenerationFinished;

		ManifestGenerationProgressChangedEventArgs ProgressArgs;

		private readonly string TargetPath;

		public ManifestHandler (string InTargetPath)
		{
			ProgressArgs = new ManifestGenerationProgressChangedEventArgs ();
			TargetPath = InTargetPath;
		}

		public void GenerateManifest ()
		{			
			Thread t = new Thread (GenerateManifestAsync);
			t.Start ();
		}

		private void GenerateManifestAsync ()
		{
			string parentDirectory = Directory.GetParent (TargetPath).ToString ();
			string manifestPath = String.Format (@"{0}{1}LauncherManifest.txt", parentDirectory, Path.DirectorySeparatorChar);
			string manifestChecksumPath = String.Format (@"{0}{1}LauncherManifest.checksum", parentDirectory, Path.DirectorySeparatorChar);


			if (File.Exists (manifestPath))
			{
				//create a new empty file and close it (effectively deleting the old manifest)
				File.Create (manifestPath).Close ();
			}

			TextWriter tw = new StreamWriter (manifestPath);

			string[] files = Directory.GetFiles (TargetPath, "*", SearchOption.AllDirectories);                
			int completedFiles = 0;

			IEnumerable<string> enumeratedFiles = Directory
				.EnumerateFiles (TargetPath, "*", SearchOption.AllDirectories);

			foreach (string file in enumeratedFiles)
			{
				if (file != null)
				{
                    // Skip debugging files during test cycles.
                    if (!file.EndsWith(".pdb"))
                    {
                        FileStream fileStream = File.OpenRead(file);
                        var skipDirectory = TargetPath;

                        int fileAmount = files.Length;
                        string currentFile = file.Substring(skipDirectory.Length);

                        //get file size on disk
                        FileInfo Info = new FileInfo(file);
                        long fileSize = Info.Length;

                        string hash = MD5Handler.GetFileHash(fileStream);
                        string manifestLine = String.Format(@"{0}:{1}:{2}", file.Substring(skipDirectory.Length), hash, fileSize.ToString());

                        if (fileStream != null)
                        {
                            fileStream.Close();
                        }

                        completedFiles++;


                        tw.WriteLine(manifestLine);
                        ProgressArgs.Filepath = currentFile;
                        ProgressArgs.TotalFiles = fileAmount;
                        ProgressArgs.CompletedFiles = completedFiles;
                        ProgressArgs.MD5 = hash;
                        ProgressArgs.Filesize = fileSize;

                        OnManifestGenerationProgressChanged();
                    }
				}
			}
			tw.Close ();


			//create a manifest checksum file.
			string manifestHash = MD5Handler.GetFileHash (File.OpenRead (manifestPath));

			FileStream checksumStream = File.Create (manifestChecksumPath);

			TextWriter tw2 = new StreamWriter (checksumStream);
			tw2.WriteLine (manifestHash);
			tw2.Close ();

			OnManifestGenerationFinished ();
		}

		private void OnManifestGenerationProgressChanged ()
		{
			if (ManifestGenerationProgressChanged != null)
			{
				ManifestGenerationProgressChanged (this, ProgressArgs);
			}	
		}

		private void OnManifestGenerationFinished ()
		{
			if (ManifestGenerationFinished != null)
			{
				ManifestGenerationFinished (this, EventArgs.Empty);
			}
		}
	}
}

