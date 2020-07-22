//
//  MainWindow.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
//

using System;
using System.Text.RegularExpressions;
using Gdk;
using GLib;
using Gtk;

using Launchpad.Common;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using Launchpad.Launcher.Utility.Enums;

using NGettext;
using NLog;
using SixLabors.ImageSharp;
using Application = Gtk.Application;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace Launchpad.Launcher.Interface
{
    /// <summary>
    /// The main UI class for Launchpad. This class acts as a manager for all threaded
    /// actions, such as installing, updating or repairing the game.
    /// </summary>
    public sealed partial class MainWindow : Gtk.Window
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        private readonly LocalVersionService _localVersionService = new LocalVersionService();

        /// <summary>
        /// The configuration instance reference.
        /// </summary>
        private readonly ILaunchpadConfiguration _configuration = ConfigHandler.Instance.Configuration;

        /// <summary>
        /// The checks handler reference.
        /// </summary>
        private readonly ChecksHandler _checks = new ChecksHandler();

        /// <summary>
        /// The launcher handler. Allows updating the launcher and loading the changelog.
        /// </summary>
        private readonly LauncherHandler _launcher = new LauncherHandler();

        /// <summary>
        /// The game handler. Allows updating, installing and repairing the game.
        /// </summary>
        private readonly GameHandler _game = new GameHandler();

        private readonly TagfileService _tagfileService = new TagfileService();

        /// <summary>
        /// The current mode that the launcher is in. Determines what the primary button does when pressed.
        /// </summary>
        private ELauncherMode _mode = ELauncherMode.Inactive;

        /// <summary>
        /// The localization catalog.
        /// </summary>
        private static readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./Content/locale");

        /// <summary>
        /// Whether or not the launcher UI has been initialized.
        /// </summary>
        private bool _isInitialized;

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

            // Bind the handler events
            _game.ProgressChanged += OnModuleInstallationProgressChanged;
            _game.DownloadFinished += OnGameDownloadFinished;
            _game.DownloadFailed += OnGameDownloadFailed;
            _game.LaunchFailed += OnGameLaunchFailed;
            _game.GameExited += OnGameExited;

            _launcher.LauncherDownloadProgressChanged += OnModuleInstallationProgressChanged;
            _launcher.LauncherDownloadFinished += OnLauncherDownloadFinished;

            // Set the initial launcher mode
            SetLauncherMode(ELauncherMode.Inactive, false);

            // Set the window title
            this.Title = LocalizationCatalog.GetString("Launchpad - {0}", _configuration.GameName);
            _statusLabel.Text = LocalizationCatalog.GetString("Idle");
        }

        /// <summary>
        /// Initializes the UI of the launcher, performing varying checks against the patching server.
        /// </summary>
        /// <returns>A task that must be awaited.</returns>
        public Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            // First of all, check if we can connect to the patching service.
            if (!_checks.CanPatch())
            {
                using (var dialog = new MessageDialog
                (
                    this,
                    DialogFlags.Modal,
                    MessageType.Warning,
                    ButtonsType.Ok,
                    LocalizationCatalog.GetString("Failed to connect to the patch server. Please check your settings.")
                ))
                {
                    dialog.Run();
                }

                _statusLabel.Text = LocalizationCatalog.GetString("Could not connect to server.");
                _menuRepairItem.Sensitive = false;
            }
            else
            {
                LoadBanner();

                LoadChangelog();

                // If we can connect, proceed with the rest of our checks.
                if (ChecksHandler.IsInitialStartup())
                {
                    DisplayInitialStartupDialog();
                }

                // If the launcher does not need an update at this point, we can continue checks for the game
                if (!_checks.IsLauncherOutdated())
                {
                    if (!_checks.IsPlatformAvailable(_configuration.SystemTarget))
                    {
                        Log.Info
                        (
                            $"The server does not provide files for platform \"{PlatformHelpers.GetCurrentPlatform()}\". " +
                            "A .provides file must be present in the platforms' root directory."
                        );

                        SetLauncherMode(ELauncherMode.Inactive, false);
                    }
                    else
                    {
                        if (!_checks.IsGameInstalled())
                        {
                            // If the game is not installed, offer to install it
                            Log.Info("The game has not yet been installed.");
                            SetLauncherMode(ELauncherMode.Install, false);

                            // Since the game has not yet been installed, disallow manual repairs
                            _menuRepairItem.Sensitive = false;

                            // and reinstalls
                            _menuReinstallItem.Sensitive = false;
                        }
                        else
                        {
                            // If the game is installed (which it should be at this point), check if it needs to be updated
                            if (_checks.IsGameOutdated())
                            {
                                // If it does, offer to update it
                                Log.Info("The game is outdated.");
                                SetLauncherMode(ELauncherMode.Update, false);
                            }
                            else
                            {
                                // All checks passed, so we can offer to launch the game.
                                Log.Info("All checks passed. Game can be launched.");
                                SetLauncherMode(ELauncherMode.Launch, false);
                            }
                        }
                    }
                }
                else
                {
                    // The launcher was outdated.
                    Log.Info($"The launcher is outdated. \n\tLocal version: {_localVersionService.GetLocalLauncherVersion()}");
                    SetLauncherMode(ELauncherMode.Update, false);
                }
            }

            _isInitialized = true;
            return Task.CompletedTask;
        }

        private void LoadChangelog()
        {
            var protocol = PatchProtocolProvider.GetHandler();
            var markup = protocol.GetChangelogMarkup();

            // Preprocess dot lists
            var dotRegex = new Regex("(?<=^\\s+)\\*", RegexOptions.Multiline);
            markup = dotRegex.Replace(markup, "•");

            // Preprocess line breaks
            var regex = new Regex("(?<!\n)\n(?!\n)(?!  )");
            markup = regex.Replace(markup, string.Empty);

            var startIter = _changelogTextView.Buffer.StartIter;
            _changelogTextView.Buffer.InsertMarkup(ref startIter, markup);
        }

        private void DisplayInitialStartupDialog()
        {
            Log.Info("This instance is the first start of the application in this folder.");

            var text = LocalizationCatalog.GetString
            (
                "This appears to be the first time you're starting the launcher.\n" +
                "Is this the location where you would like to install the game?"
            ) + $"\n\n{DirectoryHelpers.GetLocalLauncherDirectory()}";

            using var shouldInstallHereDialog = new MessageDialog
            (
                this,
                DialogFlags.Modal,
                MessageType.Question,
                ButtonsType.OkCancel,
                text
            );
            if (shouldInstallHereDialog.Run() == (int)ResponseType.Ok)
            {
                // Yes, install here
                Log.Info("User accepted installation in this directory. Installing in current directory.");

                _tagfileService.CreateLauncherTagfile();
            }
            else
            {
                // No, don't install here
                Log.Info("User declined installation in this directory. Exiting...");
                Environment.Exit(2);
            }
        }

        private void LoadBanner()
        {
            var patchHandler = PatchProtocolProvider.GetHandler();

            // Load the game banner (if there is one)
            if (!patchHandler.CanProvideBanner())
            {
                return;
            }

            Task.Factory.StartNew
            (
                () =>
                {
                    // Fetch the banner from the server
                    var bannerImage = patchHandler.GetBanner();

                    bannerImage.Mutate(i => i.Resize(_bannerImage.AllocatedWidth, 0));

                    // Load the image into a pixel buffer
                    return new Pixbuf
                    (
                        Bytes.NewStatic(bannerImage.SavePixelData()),
                        Colorspace.Rgb,
                        true,
                        8,
                        bannerImage.Width,
                        bannerImage.Height,
                        4 * bannerImage.Width
                    );
                }
            )
            .ContinueWith
            (
                async bannerTask => _bannerImage.Pixbuf = await bannerTask
            );
        }

        /// <summary>
        /// Sets the launcher mode and updates UI elements to match.
        /// </summary>
        /// <param name="newMode">The new mode.</param>
        /// <param name="isInProgress">If set to <c>true</c>, the selected mode is in progress.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Will be thrown if the <see cref="ELauncherMode"/> passed to the function is not a valid value.
        /// </exception>
        private void SetLauncherMode(ELauncherMode newMode, bool isInProgress)
        {
            // Set the global launcher mode
            _mode = newMode;

            // Set the UI elements to match
            switch (newMode)
            {
                case ELauncherMode.Install:
                {
                    if (isInProgress)
                    {
                        _mainButton.Sensitive = false;
                        _mainButton.Label = LocalizationCatalog.GetString("Installing...");
                    }
                    else
                    {
                        _mainButton.Sensitive = true;
                        _mainButton.Label = LocalizationCatalog.GetString("Install");
                    }
                    break;
                }
                case ELauncherMode.Update:
                {
                    if (isInProgress)
                    {
                        _mainButton.Sensitive = false;
                        _mainButton.Label = LocalizationCatalog.GetString("Updating...");
                    }
                    else
                    {
                        _mainButton.Sensitive = true;
                        _mainButton.Label = LocalizationCatalog.GetString("Update");
                    }
                    break;
                }
                case ELauncherMode.Repair:
                {
                    if (isInProgress)
                    {
                        _mainButton.Sensitive = false;
                        _mainButton.Label = LocalizationCatalog.GetString("Repairing...");
                    }
                    else
                    {
                        _mainButton.Sensitive = true;
                        _mainButton.Label = LocalizationCatalog.GetString("Repair");
                    }
                    break;
                }
                case ELauncherMode.Launch:
                {
                    if (isInProgress)
                    {
                        _mainButton.Sensitive = false;
                        _mainButton.Label = LocalizationCatalog.GetString("Launching...");
                    }
                    else
                    {
                        _mainButton.Sensitive = true;
                        _mainButton.Label = LocalizationCatalog.GetString("Launch");
                    }
                    break;
                }
                case ELauncherMode.Inactive:
                {
                    _menuRepairItem.Sensitive = false;

                    _mainButton.Sensitive = false;
                    _mainButton.Label = LocalizationCatalog.GetString("Inactive");
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(newMode), "An invalid launcher mode was passed to SetLauncherMode.");
                }
            }

            if (isInProgress)
            {
                _menuRepairItem.Sensitive = false;
                _menuReinstallItem.Sensitive = false;
            }
            else
            {
                _menuRepairItem.Sensitive = true;
                _menuReinstallItem.Sensitive = true;
            }
        }

        /// <summary>
        /// Runs a game repair, no matter what the state the installation is in.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void OnMenuRepairItemActivated(object? sender, EventArgs e)
        {
            SetLauncherMode(ELauncherMode.Repair, false);

            // Simulate a button press from the user.
            _mainButton.Click();
        }

        /// <summary>
        /// Handles switching between different functionality depending on what is visible on the button to the user,
        /// such as installing, updating, repairing, and launching.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty arguments.</param>
        private void OnMainButtonClicked(object? sender, EventArgs e)
        {
            // Drop out if the current platform isn't available on the server
            if (!_checks.IsPlatformAvailable(_configuration.SystemTarget))
            {
                _statusLabel.Text =
                    LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
                _mainProgressBar.Text = string.Empty;

                Log.Info
                (
                    $"The server does not provide files for platform \"{PlatformHelpers.GetCurrentPlatform()}\". " +
                    "A .provides file must be present in the platforms' root directory."
                );

                SetLauncherMode(ELauncherMode.Inactive, false);

                return;
            }

            // else, run the relevant function
            switch (_mode)
            {
                case ELauncherMode.Repair:
                {
                    // Repair the game asynchronously
                    SetLauncherMode(ELauncherMode.Repair, true);
                    _game.VerifyGame();

                    break;
                }
                case ELauncherMode.Install:
                {
                    // Install the game asynchronously
                    SetLauncherMode(ELauncherMode.Install, true);
                    _game.InstallGame();

                    break;
                }
                case ELauncherMode.Update:
                {
                    if (_checks.IsLauncherOutdated())
                    {
                        // Update the launcher asynchronously
                        SetLauncherMode(ELauncherMode.Update, true);
                        _launcher.UpdateLauncher();
                    }
                    else
                    {
                        // Update the game asynchronously
                        SetLauncherMode(ELauncherMode.Update, true);
                        _game.UpdateGame();
                    }

                    break;
                }
                case ELauncherMode.Launch:
                {
                    _statusLabel.Text = LocalizationCatalog.GetString("Idle");
                    _mainProgressBar.Text = string.Empty;

                    SetLauncherMode(ELauncherMode.Launch, true);
                    _game.LaunchGame();

                    break;
                }
                default:
                {
                    Log.Warn("The main button was pressed with an invalid active mode. No functionality has been defined for this mode.");
                    break;
                }
            }
        }

        /// <summary>
        /// Starts the launcher update process when its files have finished downloading.
        /// </summary>
        private static void OnLauncherDownloadFinished(object? sender, EventArgs e)
        {
            Application.Invoke((o, args) =>
            {
                var script = LauncherHandler.CreateUpdateScript();

                Process.Start(script);

                Application.Quit();
            });
        }

        /// <summary>
        /// Warns the user when the game fails to launch, and offers to attempt a repair.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty event args.</param>
        private void OnGameLaunchFailed(object? sender, EventArgs e)
        {
            Application.Invoke((o, args) =>
            {
                _statusLabel.Text = LocalizationCatalog.GetString("The game failed to launch. Try repairing the installation.");
                _mainProgressBar.Text = string.Empty;

                SetLauncherMode(ELauncherMode.Repair, false);
            });
        }

        /// <summary>
        /// Provides alternatives when the game fails to download, either through an update or through an installation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the type of failure that occurred.</param>
        private void OnGameDownloadFailed(object? sender, EventArgs e)
        {
            Application.Invoke((o, args) =>
            {
                switch (_mode)
                {
                    case ELauncherMode.Install:
                    case ELauncherMode.Update:
                    case ELauncherMode.Repair:
                    {
                        // Set the mode to the same as it was, but no longer in progress.
                        // The modes which fall to this case are all capable of repairing an incomplete or
                        // broken install on their own.
                        SetLauncherMode(_mode, false);
                        break;
                    }
                    default:
                    {
                        // Other cases (such as Launch) will go to the default mode of Repair.
                        SetLauncherMode(ELauncherMode.Repair, false);
                        break;
                    }
                }
            });
        }

        /// <summary>
        /// Updates the progress bar and progress label during installations, repairs and updates.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the progress values and current filename.</param>
        private void OnModuleInstallationProgressChanged(object? sender, ModuleProgressChangedArgs e)
        {
            Application.Invoke((o, args) =>
            {
                _mainProgressBar.Text = e.ProgressBarMessage;
                _statusLabel.Text = e.IndicatorLabelMessage;
                _mainProgressBar.Fraction = e.ProgressFraction;
            });
        }

        /// <summary>
        /// Allows the user to launch or repair the game once installation finishes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the result of the download.</param>
        private void OnGameDownloadFinished(object? sender, EventArgs e)
        {
            Application.Invoke((o, args) =>
            {
                _statusLabel.Text = LocalizationCatalog.GetString("Idle");

                switch (_mode)
                {
                    case ELauncherMode.Install:
                    {
                        _mainProgressBar.Text = LocalizationCatalog.GetString("Installation finished");
                        break;
                    }
                    case ELauncherMode.Update:
                    {
                        _mainProgressBar.Text = LocalizationCatalog.GetString("Update finished");
                        break;
                    }
                    case ELauncherMode.Repair:
                    {
                        _mainProgressBar.Text = LocalizationCatalog.GetString("Repair finished");
                        break;
                    }
                    default:
                    {
                        _mainProgressBar.Text = string.Empty;
                        break;
                    }
                }

                SetLauncherMode(ELauncherMode.Launch, false);
            });
        }

        /// <summary>
        /// Handles offering of repairing the game to the user should the game exit
        /// with a bad exit code.
        /// </summary>
        private void OnGameExited(object? sender, int exitCode)
        {
            Application.Invoke((o, args) =>
            {
                if (exitCode != 0)
                {
                    using var crashDialog = new MessageDialog
                    (
                        this,
                        DialogFlags.Modal,
                        MessageType.Question,
                        ButtonsType.YesNo,
                        LocalizationCatalog.GetString
                        (
                            "Whoops! The game appears to have crashed.\n" +
                            "Would you like the launcher to verify the installation?"
                        )
                    );
                    if (crashDialog.Run() == (int)ResponseType.Yes)
                    {
                        SetLauncherMode(ELauncherMode.Repair, false);
                        _mainButton.Click();
                    }
                    else
                    {
                        SetLauncherMode(ELauncherMode.Launch, false);
                    }
                }
                else
                {
                    SetLauncherMode(ELauncherMode.Launch, false);
                }
            });
        }

        /// <summary>
        /// Handles starting of a reinstallation procedure as requested by the user.
        /// </summary>
        private void OnReinstallGameActionActivated(object? sender, EventArgs e)
        {
            using var reinstallConfirmDialog = new MessageDialog
            (
                this,
                DialogFlags.Modal,
                MessageType.Question,
                ButtonsType.YesNo,
                LocalizationCatalog.GetString
                (
                    "Reinstalling the game will delete all local files and download the entire game again.\n" +
                    "Are you sure you want to reinstall the game?"
                )
            );

            if (reinstallConfirmDialog.Run() != (int)ResponseType.Yes)
            {
                return;
            }

            SetLauncherMode(ELauncherMode.Install, true);
            _game.ReinstallGame();
        }
    }
}
