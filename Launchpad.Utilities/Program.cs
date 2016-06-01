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
using log4net;

[assembly: CLSCompliant(true)]
namespace Launchpad.Utilities
{
	static class Program
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		public const string BatchSwitch = "-b";
		public const string DirectorySwitch = "-d";
		public const string ManifestTypeSwitch = "-m";

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
					Log.Info("Running in batch mode.");

					EManifestType ManifestType = EManifestType.Game;
					if (Arguments.Contains(ManifestTypeSwitch))
					{
						if (Arguments.IndexOf(ManifestTypeSwitch) != args.Length - 1)
						{
							string targetManifest = Arguments[(Arguments.IndexOf(ManifestTypeSwitch) + 1)];

							if (EManifestType.Game.ToString() == targetManifest)
							{
								ManifestType = EManifestType.Game;
							}
							else if (EManifestType.Launchpad.ToString() == targetManifest)
							{
								ManifestType = EManifestType.Launchpad;
							}
							else
							{
								Log.Warn("The '-m' manifest switch must be followed by either 'Game' or 'Launcher'.");
							}
						}
						else
						{
							Log.Warn("The '-m' manifest switch must be followed by either 'Game' or 'Launcher'.");
						}
					}

					if (Arguments.Contains(DirectorySwitch))
					{
						if (Arguments.IndexOf(DirectorySwitch) != args.Length - 1)
						{
							string TargetDirectory = Arguments[(Arguments.IndexOf(DirectorySwitch) + 1)].TrimEnd(Path.DirectorySeparatorChar);
							Log.Info("Target: " + TargetDirectory);

							if (Directory.Exists(TargetDirectory))
							{
								Log.Info("Generating manifest...");

								ManifestHandler Manifest = new ManifestHandler();

								Manifest.ManifestGenerationProgressChanged += OnProgressChanged;
								Manifest.ManifestGenerationFinished += OnGenerationFinished;

								Manifest.GenerateManifest(TargetDirectory, ManifestType);
							}
							else
							{
								Log.Warn("The '-d' directory switch must be followed by a valid directory.");
							}
						}
						else
						{
							Log.Warn("The '-d' directory switch must be followed by a valid directory.");
						}
					}
					else
					{
						Log.Warn("No directory provided for batch mode, using working directory.");
						Log.Info("Generating manifest...");

						ManifestHandler Manifest = new ManifestHandler();

						Manifest.ManifestGenerationProgressChanged += OnProgressChanged;
						Manifest.ManifestGenerationFinished += OnGenerationFinished;

						Manifest.GenerateManifest(Directory.GetCurrentDirectory(), ManifestType);
					}
				}
				else
				{
					Log.Info("Run the program with -b to enable batch mode. Use -d <directory> to select the target directory, or omit it to use the working directory. Use -m [Game|Launcher] to select the type of manifest (default is Game).");
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
			Log.Info($"Processed file {e.Filepath} : {e.MD5} : {e.Filesize}");
		}

		private static void OnGenerationFinished(object sender, EventArgs e)
		{
			Log.Info("Generation finished.");
		}
	}
}
