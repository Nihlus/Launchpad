using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

using Launchpad.Utilities.Handlers;
using Launchpad.Utilities.Events.Arguments;

[assembly: CLSCompliant (true)]
namespace Launchpad.Utilities
{
	static class Program
	{
		static readonly string BatchSwitch = "-b";
		static readonly string DirectorySwitch = "-d";

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main (string[] args)
		{
			List<string> Arguments = new List<string> (args);
			if (args.Length > 0)
			{
				if (Arguments.Contains (BatchSwitch))
				{	
					// Don't load the UI - instead, run the manifest generation directly
					Console.WriteLine ("[Info]: Running in batch mode.");

					if (Arguments.Contains (DirectorySwitch))
					{
						if (Arguments.IndexOf (DirectorySwitch) != args.Length - 1)
						{
							string TargetDirectory = Arguments [(Arguments.IndexOf (DirectorySwitch) + 1)];
							Console.WriteLine (TargetDirectory);

							if (Directory.Exists (TargetDirectory))
							{
								Console.WriteLine ("[Info]: Generating manifest...");

								ManifestHandler Manifest = new ManifestHandler (TargetDirectory);

								Manifest.ManifestGenerationProgressChanged += OnProgressChanged;
								Manifest.ManifestGenerationFinished += OnGenerationFinished;

								Manifest.GenerateManifest ();
							}
							else
							{
								Console.WriteLine ("[Warning]: The '-d' directory switch must be followed by a valid directory.");
							}
						}
						else
						{
							Console.WriteLine ("[Warning]: The '-d' directory switch must be followed by a valid directory.");
						}
					}
					else
					{
						Console.WriteLine ("[Warning]: No directory provided for batch mode, using working directory.");
						Console.WriteLine ("[Info]: Generating manifest...");

						ManifestHandler Manifest = new ManifestHandler (Directory.GetCurrentDirectory ());

						Manifest.ManifestGenerationProgressChanged += OnProgressChanged;
						Manifest.ManifestGenerationFinished += OnGenerationFinished;

						Manifest.GenerateManifest ();
					}
				}
				else
				{
					Console.WriteLine ("[Info]: Run the program with -b to enable batch mode. Use -d <directory> to select the target directory, or omit it to use the working directory.");
				}
			}
			else
			{
				if (ChecksHandler.IsRunningOnUnix ())
				{
					// run a GTK UI instead of WinForms
					Gtk.Application.Init ();

					MainWindow win = new MainWindow ();
					win.Show ();
					Gtk.Application.Run ();
				}
				else
				{
					// run a WinForms UI instead of GTK
					System.Windows.Forms.Application.EnableVisualStyles ();
					System.Windows.Forms.Application.SetCompatibleTextRenderingDefault (false);
					System.Windows.Forms.Application.Run (new MainForm ());
				}
			}
		}

		private static void OnProgressChanged (object sender, ManifestGenerationProgressChangedEventArgs e)
		{
			Console.WriteLine (String.Format ("[Info]: Processed file {0} : {1} : {2}", e.Filepath, e.MD5, e.Filesize.ToString ()));
		}

		private static void OnGenerationFinished (object sender, EventArgs e)
		{
			Console.WriteLine ("[Info]: Generation finished.");
		}
	}
}
