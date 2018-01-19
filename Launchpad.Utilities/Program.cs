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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using GLib;
using Gtk;
using Launchpad.Utilities.Handlers;
using Launchpad.Utilities.Utility.Events;
using Launchpad.Utilities.Interface;
using Launchpad.Utilities.Options;
using log4net;
using Launchpad.Common.Enums;

namespace Launchpad.Utilities
{
	internal static class Program
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		private static async Task Main(string[] args)
		{
			var options = new CLIOptions();
			Parser.Default.ParseArguments<CLIOptions>(args)
				.WithParsed(r => options = r);

			if (options.RunBatchProcessing)
			{
				if (string.IsNullOrEmpty(options.TargetDirectory) || options.ManifestType == EManifestType.Unknown)
				{
					Log.Error("Target directory not set, or manifest type not set.");
					return;
				}

				// At this point, the options should be valid. Run batch processing.
				if (Directory.Exists(options.TargetDirectory))
				{
					Log.Info("Generating manifest...");

					var manifestGenerationHandler = new ManifestGenerationHandler();

					var progressReporter = new Progress<ManifestGenerationProgressChangedEventArgs>
					(
						e => Log.Info($"Processed file {e.Filepath} : {e.Hash} : {e.Filesize}")
					);

					await manifestGenerationHandler.GenerateManifestAsync
					(
						options.TargetDirectory,
						options.ManifestType,
						progressReporter,
						CancellationToken.None
					);

					Log.Info("Generation finished.");
				}
				else
				{
					Log.Error("The selected directory did not exist.");
				}
			}
			else if (string.IsNullOrEmpty(options.TargetDirectory) && options.ManifestType == EManifestType.Unknown)
			{
				// Run a GTK UI instead of batch processing
				Application.Init();
				SynchronizationContext.SetSynchronizationContext(new GLibSynchronizationContext());

				var win = new MainWindow();
				win.Show();
				Application.Run();
			}
		}
	}
}
