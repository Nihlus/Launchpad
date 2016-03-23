//
//  Program.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;

using Launchpad.Utilities.Handlers;
using Launchpad.Utilities.Utility.Events;
using Launchpad.Utilities.UnixUI;

[assembly: CLSCompliant(true)]
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
		static void Main(string[] args)
		{
			List<string> Arguments = new List<string>(args);
			if (args.Length > 0)
			{
				if (Arguments.Contains(BatchSwitch))
				{	
					// Don't load the UI - instead, run the manifest generation directly
					Console.WriteLine("[Info]: Running in batch mode.");

					if (Arguments.Contains(DirectorySwitch))
					{
						if (Arguments.IndexOf(DirectorySwitch) != args.Length - 1)
						{
							string TargetDirectory = Arguments[(Arguments.IndexOf(DirectorySwitch) + 1)];
							Console.WriteLine(TargetDirectory);

							if (Directory.Exists(TargetDirectory))
							{
								Console.WriteLine("[Info]: Generating manifest...");

								ManifestHandler Manifest = new ManifestHandler();

								Manifest.ManifestGenerationProgressChanged += OnProgressChanged;
								Manifest.ManifestGenerationFinished += OnGenerationFinished;

								Manifest.GenerateManifest(TargetDirectory);
							}
							else
							{
								Console.WriteLine("[Warning]: The '-d' directory switch must be followed by a valid directory.");
							}
						}
						else
						{
							Console.WriteLine("[Warning]: The '-d' directory switch must be followed by a valid directory.");
						}
					}
					else
					{
						Console.WriteLine("[Warning]: No directory provided for batch mode, using working directory.");
						Console.WriteLine("[Info]: Generating manifest...");

						ManifestHandler Manifest = new ManifestHandler();

						Manifest.ManifestGenerationProgressChanged += OnProgressChanged;
						Manifest.ManifestGenerationFinished += OnGenerationFinished;

						Manifest.GenerateManifest(Directory.GetCurrentDirectory());
					}
				}
				else
				{
					Console.WriteLine("[Info]: Run the program with -b to enable batch mode. Use -d <directory> to select the target directory, or omit it to use the working directory.");
				}
			}
			else
			{
				// run a GTK UI instead of WinForms
				Gtk.Application.Init();

				MainWindow win = new MainWindow();
				win.Show();
				Gtk.Application.Run();
			}
		}

		private static void OnProgressChanged(object sender, ManifestGenerationProgressChangedEventArgs e)
		{
			Console.WriteLine(String.Format("[Info]: Processed file {0} : {1} : {2}", e.Filepath, e.MD5, e.Filesize));
		}

		private static void OnGenerationFinished(object sender, EventArgs e)
		{
			Console.WriteLine("[Info]: Generation finished.");
		}
	}
}
