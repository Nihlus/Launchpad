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
using Gtk;

using Launchpad.Utilities.Utility.Events;
using NGettext;

namespace Launchpad.Utilities.UnixUI
{
	[CLSCompliant(false)]
	public partial class MainWindow : Gtk.Window
	{
		/// <summary>
		/// The localization catalog.
		/// </summary>
		private readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./locale");

		public MainWindow()
			: base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			fileChooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			progressLabel.Text = LocalizationCatalog.GetString("Idle");
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			Application.Quit();
			a.RetVal = true;
		}

		protected void OnGenerateManifestButtonClicked(object sender, EventArgs e)
		{
			generateManifestButton.Sensitive = false;

			string TargetDirectory = fileChooser.CurrentFolder;

			ManifestHandler Manifest = new ManifestHandler(TargetDirectory);

			Manifest.ManifestGenerationProgressChanged += OnGenerateManifestProgressChanged;
			Manifest.ManifestGenerationFinished += OnGenerateManifestFinished;

			Manifest.GenerateManifest();
		}

		/// <summary>
		/// Updates the UI when a file is entered into the manifest.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Arguments containing information about the entered file.</param>
		protected void OnGenerateManifestProgressChanged(object sender, ManifestGenerationProgressChangedEventArgs e)
		{
			Application.Invoke(delegate
				{
					string progressString = LocalizationCatalog.GetString("{0} : {1} out of {2}");
					progressLabel.Text = String.Format(progressString, e.Filepath, e.CompletedFiles, e.TotalFiles);

					progressbar.Fraction = (double)e.CompletedFiles / (double)e.TotalFiles;
				});
		}

		/// <summary>
		/// Updates the UI when the manifest is complete.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Empty arguments</param>
		protected void OnGenerateManifestFinished(object sender, EventArgs e)
		{
			Application.Invoke(delegate
				{
					progressLabel.Text = LocalizationCatalog.GetString("Finished");
					generateManifestButton.Sensitive = true;
				});
		}
	}
}

