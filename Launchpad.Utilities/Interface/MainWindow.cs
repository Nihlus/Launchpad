//
//  MainWindow.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Launchpad.Common.Enums;
using Launchpad.Utilities.Handlers;
using Launchpad.Utilities.Utility.Events;
using NGettext;
using SysPath = System.IO.Path;

namespace Launchpad.Utilities.Interface
{
	public partial class MainWindow : Window
	{
		/// <summary>
		/// The manifest generation handler.
		/// </summary>
		private readonly ManifestGenerationHandler Manifest = new ManifestGenerationHandler();

		/// <summary>
		/// The localization catalog.
		/// </summary>
		private readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./Content/locale");

		private readonly IProgress<ManifestGenerationProgressChangedEventArgs> ProgressReporter;

		private CancellationTokenSource TokenSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		/// <param name="builder">The UI builder.</param>
		/// <param name="handle">The native handle of the window.</param>
		private MainWindow(Builder builder, IntPtr handle)
			: base(handle)
		{
			builder.Autoconnect(this);

			BindUIEvents();

			this.ProgressReporter = new Progress<ManifestGenerationProgressChangedEventArgs>
			(
				e =>
				{
					var progressString = this.LocalizationCatalog.GetString("Hashing {0} : {1} out of {2}");
					this.StatusLabel.Text = string.Format(progressString, e.Filepath, e.CompletedFiles, e.TotalFiles);

					this.MainProgressBar.Fraction = e.CompletedFiles / (double)e.TotalFiles;
				}
			);

			this.FolderChooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			this.FolderChooser.SelectMultiple = false;

			this.StatusLabel.Text = this.LocalizationCatalog.GetString("Idle");
		}

		private async void OnGenerateGameManifestButtonClicked(object sender, EventArgs e)
		{
			var targetDirectory = this.FolderChooser.Filename;

			if (!Directory.GetFiles(targetDirectory).Any(s => s.Contains("GameVersion.txt")))
			{
				var dialog = new MessageDialog
				(
					this,
					DialogFlags.Modal,
					MessageType.Question,
					ButtonsType.YesNo,
					this.LocalizationCatalog.GetString
					(
						"No GameVersion.txt file could be found in the target directory. This file is required.\n" +
						"Would you like to add one? The version will be \"1.0.0\"."
					)
				);

				if (dialog.Run() == (int) ResponseType.Yes)
				{
					var gameVersionPath = SysPath.Combine(targetDirectory, "GameVersion.txt");
					File.WriteAllText(gameVersionPath, new Version("1.0.0").ToString());

					dialog.Destroy();
				}
				else
				{
					dialog.Destroy();
					return;
				}
			}

			await GenerateManifestAsync(EManifestType.Game);
		}

		private async void OnGenerateLaunchpadManifestButtonClicked(object sender, EventArgs e)
		{
			await GenerateManifestAsync(EManifestType.Launchpad);
		}

		private async Task GenerateManifestAsync(EManifestType manifestType)
		{
			this.TokenSource = new CancellationTokenSource();

			this.GenerateGameManifestButton.Sensitive = false;
			this.GenerateLaunchpadManifestButton.Sensitive = false;

			var targetDirectory = this.FolderChooser.Filename;

			try
			{
				await this.Manifest.GenerateManifestAsync
				(
					targetDirectory,
					manifestType,
					this.ProgressReporter,
					this.TokenSource.Token
				);

				this.StatusLabel.Text = this.LocalizationCatalog.GetString("Finished");
			}
			catch (TaskCanceledException)
			{
				this.StatusLabel.Text = this.LocalizationCatalog.GetString("Cancelled");
				this.MainProgressBar.Fraction = 0;
			}

			this.GenerateGameManifestButton.Sensitive = true;
			this.GenerateLaunchpadManifestButton.Sensitive = true;
		}
	}
}
