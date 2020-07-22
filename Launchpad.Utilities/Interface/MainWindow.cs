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
        private readonly ManifestGenerationHandler _manifest = new ManifestGenerationHandler();

        /// <summary>
        /// The localization catalog.
        /// </summary>
        private readonly ICatalog _localizationCatalog = new Catalog("Launchpad", "./Content/locale");

        private readonly IProgress<ManifestGenerationProgressChangedEventArgs> _progressReporter;

        private CancellationTokenSource _tokenSource;

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

            this._progressReporter = new Progress<ManifestGenerationProgressChangedEventArgs>
            (
                e =>
                {
                    var progressString = this._localizationCatalog.GetString("Hashing {0} : {1} out of {2}");
                    this._statusLabel.Text = string.Format(progressString, e.Filepath, e.CompletedFiles, e.TotalFiles);

                    this._mainProgressBar.Fraction = e.CompletedFiles / (double)e.TotalFiles;
                }
            );

            this._folderChooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            this._folderChooser.SelectMultiple = false;

            this._statusLabel.Text = this._localizationCatalog.GetString("Idle");
        }

        private async void OnGenerateGameManifestButtonClicked(object sender, EventArgs e)
        {
            var targetDirectory = this._folderChooser.Filename;

            if (!Directory.GetFiles(targetDirectory).Any(s => s.Contains("GameVersion.txt")))
            {
                using var dialog = new MessageDialog
                (
                    this,
                    DialogFlags.Modal,
                    MessageType.Question,
                    ButtonsType.YesNo,
                    this._localizationCatalog.GetString
                    (
                        "No GameVersion.txt file could be found in the target directory. This file is required.\n" +
                        "Would you like to add one? The version will be \"1.0.0\"."
                    )
                );

                if (dialog.Run() == (int) ResponseType.Yes)
                {
                    var gameVersionPath = SysPath.Combine(targetDirectory, "GameVersion.txt");
                    await File.WriteAllTextAsync(gameVersionPath, new Version("1.0.0").ToString());
                }
                else
                {
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
            this._tokenSource = new CancellationTokenSource();

            this._generateGameManifestButton.Sensitive = false;
            this._generateLaunchpadManifestButton.Sensitive = false;

            var targetDirectory = this._folderChooser.Filename;

            try
            {
                await this._manifest.GenerateManifestAsync
                (
                    targetDirectory,
                    manifestType,
                    this._progressReporter,
                    this._tokenSource.Token
                );

                this._statusLabel.Text = this._localizationCatalog.GetString("Finished");
            }
            catch (TaskCanceledException)
            {
                this._statusLabel.Text = this._localizationCatalog.GetString("Cancelled");
                this._mainProgressBar.Fraction = 0;
            }

            this._generateGameManifestButton.Sensitive = true;
            this._generateLaunchpadManifestButton.Sensitive = true;
        }
    }
}
