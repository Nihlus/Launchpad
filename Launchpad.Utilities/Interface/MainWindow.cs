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

		public MainWindow()
			: base(WindowType.Toplevel)
		{
			Build();

			this.Manifest.ManifestGenerationProgressChanged += OnGenerateManifestProgressChanged;
			this.Manifest.ManifestGenerationFinished += OnGenerateManifestFinished;

			this.fileChooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			this.fileChooser.SelectMultiple = false;

			this.progressLabel.Text = this.LocalizationCatalog.GetString("Idle");
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		private static void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			Application.Quit();
			a.RetVal = true;
		}

		private void OnGenerateGameManifestButtonClicked(object sender, EventArgs e)
		{
			this.generateGameManifestButton.Sensitive = false;
			this.generateLaunchpadManifestButton.Sensitive = false;

			string targetDirectory = this.fileChooser.Filename;

			if (!Directory.GetFiles(targetDirectory).Any(s => s.Contains("GameVersion.txt")))
			{
				MessageDialog dialog = new MessageDialog(this,
					DialogFlags.Modal,
					MessageType.Question,
					ButtonsType.YesNo, this.LocalizationCatalog.GetString("No GameVersion.txt file could be found in the target directory. This file is required.\n" +
												  "Would you like to add one? The version will be \"1.0.0\"."));

				if (dialog.Run() == (int) ResponseType.Yes)
				{
					string gameVersionPath = $"{targetDirectory}{SysPath.DirectorySeparatorChar}GameVersion.txt";
					File.WriteAllText(gameVersionPath, new Version("1.0.0").ToString());

					dialog.Destroy();
				}
				else
				{
					dialog.Destroy();
					return;
				}
			}

			this.Manifest.GenerateManifest(targetDirectory, EManifestType.Game);
		}

		private void OnGenerateLaunchpadManifestButtonClicked(object sender, EventArgs e)
		{
			this.generateGameManifestButton.Sensitive = false;
			this.generateLaunchpadManifestButton.Sensitive = false;

			string targetDirectory = this.fileChooser.Filename;

			this.Manifest.GenerateManifest(targetDirectory, EManifestType.Launchpad);

		}

		/// <summary>
		/// Updates the UI when a file is entered into the manifest.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Arguments containing information about the entered file.</param>
		private void OnGenerateManifestProgressChanged(object sender, ManifestGenerationProgressChangedEventArgs e)
		{
			Application.Invoke(delegate
				{
					string progressString = this.LocalizationCatalog.GetString("{0} : {1} out of {2}");
					this.progressLabel.Text = string.Format(progressString, e.Filepath, e.CompletedFiles, e.TotalFiles);

					this.progressbar.Fraction = e.CompletedFiles / (double)e.TotalFiles;
				});
		}

		/// <summary>
		/// Updates the UI when the manifest is complete.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Empty arguments</param>
		private void OnGenerateManifestFinished(object sender, EventArgs e)
		{
			Application.Invoke(delegate
				{
					this.progressLabel.Text = this.LocalizationCatalog.GetString("Finished");
					this.generateGameManifestButton.Sensitive = true;
					this.generateLaunchpadManifestButton.Sensitive = true;
				});
		}
	}
}

