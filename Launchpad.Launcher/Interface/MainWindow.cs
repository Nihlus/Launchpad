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

using Gtk;
using System;
using NGettext;
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Handlers.Protocols;
using System.Diagnostics;
using System.IO;
using Gdk;
using System.Drawing.Imaging;
using log4net;
using Launchpad.Launcher.Interface.ChangelogBrowser;

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
		private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindow));

		/// <summary>
		/// The config handler reference.
		/// </summary>
		private readonly ConfigHandler Config = ConfigHandler.Instance;

		/// <summary>
		/// The checks handler reference.
		/// </summary>
		private readonly ChecksHandler Checks = new ChecksHandler();

		/// <summary>
		/// The launcher handler. Allows updating the launcher and loading the changelog
		/// </summary>
		private readonly LauncherHandler Launcher = new LauncherHandler();

		/// <summary>
		/// The game handler. Allows updating, installing and repairing the game.
		/// </summary>
		private readonly GameHandler Game = new GameHandler();

		/// <summary>
		/// The changelog browser.
		/// </summary>
		private readonly Changelog Browser;

		/// <summary>
		/// The current mode that the launcher is in. Determines what the primary button does when pressed.
		/// </summary>
		private ELauncherMode Mode = ELauncherMode.Inactive;

		/// <summary>
		/// The localization catalog.
		/// </summary>
		private static readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./Content/locale");

		/// <summary>
		/// Whether or not the launcher UI has been initialized.
		/// </summary>
		private bool IsInitialized;

		/// <summary>
		/// Creates a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		public MainWindow()
			: base(Gtk.WindowType.Toplevel)
		{
			// Initialize the GTK UI
			Build();

			// Bind the handler events
			this.Game.ProgressChanged += OnModuleInstallationProgressChanged;
			this.Game.DownloadFinished += OnGameDownloadFinished;
			this.Game.DownloadFailed += OnGameDownloadFailed;
			this.Game.LaunchFailed += OnGameLaunchFailed;
			this.Game.GameExited += OnGameExited;

			this.Launcher.LauncherDownloadProgressChanged += OnModuleInstallationProgressChanged;
			this.Launcher.LauncherDownloadFinished += OnLauncherDownloadFinished;
			this.Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;

			// Set the initial launcher mode
			SetLauncherMode(ELauncherMode.Inactive, false);

			// Set the window title
			this.Title = LocalizationCatalog.GetString("Launchpad - {0}", this.Config.GetGameName());

			// Create a new changelog widget, and add it to the scrolled window
			this.Browser = new Changelog(this.BrowserWindow);
			this.BrowserWindow.ShowAll();

			this.IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");

			Initialize();
		}

		/// <summary>
		/// Initializes the UI of the launcher, performing varying checks against the patching server.
		/// </summary>
		public void Initialize()
		{
			if (this.IsInitialized)
			{
				return;
			}

			// First of all, check if we can connect to the patching service.
			if (!this.Checks.CanPatch())
			{
				MessageDialog dialog = new MessageDialog
				(
					this,
					DialogFlags.Modal,
					MessageType.Warning,
					ButtonsType.Ok,
					LocalizationCatalog.GetString("Failed to connect to the patch server. Please check your settings.")
				);

				dialog.Run();

				dialog.Destroy();
				this.IndicatorLabel.Text = LocalizationCatalog.GetString("Could not connect to server.");
				this.RepairGameAction.Sensitive = false;
			}
			else
			{
				// TODO: Load this asynchronously
				// Load the game banner (if there is one)
				if (this.Config.GetPatchProtocol().CanProvideBanner())
				{
					using (MemoryStream bannerStream = new MemoryStream())
					{
						// Fetch the banner from the server
						this.Config.GetPatchProtocol().GetBanner().Save(bannerStream, ImageFormat.Png);

						// Load the image into a pixel buffer
						bannerStream.Position = 0;
						this.GameBanner.Pixbuf = new Pixbuf(bannerStream);
					}
				}

				// If we can connect, proceed with the rest of our checks.
				if (ChecksHandler.IsInitialStartup())
				{
					Log.Info("This instance is the first start of the application in this folder.");

					MessageDialog shouldInstallHereDialog = new MessageDialog
					(
						this,
						DialogFlags.Modal,
						MessageType.Question,
						ButtonsType.OkCancel,
						LocalizationCatalog.GetString
						(
							"This appears to be the first time you're starting the launcher.\n" +
							"Is this the location where you would like to install the game?"
						) + $"\n\n{ConfigHandler.GetLocalDir()}"
					);

					if (shouldInstallHereDialog.Run() == (int) ResponseType.Ok)
					{
						shouldInstallHereDialog.Destroy();

						// Yes, install here
						Log.Info("User accepted installation in this directory. Installing in current directory.");

						ConfigHandler.CreateLauncherCookie();
					}
					else
					{
						shouldInstallHereDialog.Destroy();

						// No, don't install here
						Log.Info("User declined installation in this directory. Exiting...");
						Environment.Exit(2);
					}
				}

				if (this.Config.ShouldAllowAnonymousStats())
				{
					StatsHandler.SendUsageStats();
				}

				// Load the changelog. Try a direct URL first, and a protocol-specific
				// implementation after.
				if (LauncherHandler.CanAccessStandardChangelog())
				{
					this.Browser.Navigate(this.Config.GetChangelogURL());
				}
				else
				{
					this.Launcher.LoadFallbackChangelog();
				}

				// If the launcher does not need an update at this point, we can continue checks for the game
				if (!this.Checks.IsLauncherOutdated())
				{
					if (!this.Checks.IsPlatformAvailable(this.Config.GetSystemTarget()))
					{
						Log.Info($"The server does not provide files for platform \"{ConfigHandler.GetCurrentPlatform()}\". " +
						         "A .provides file must be present in the platforms' root directory.");

						SetLauncherMode(ELauncherMode.Inactive, false);
					}
					else
					{
						if (!this.Checks.IsGameInstalled())
						{
							// If the game is not installed, offer to install it
							Log.Info("The game has not yet been installed.");
							SetLauncherMode(ELauncherMode.Install, false);

							// Since the game has not yet been installed, disallow manual repairs
							this.RepairGameAction.Sensitive = false;

							// and reinstalls
							this.ReinstallGameAction.Sensitive = false;
						}
						else
						{
							// If the game is installed (which it should be at this point), check if it needs to be updated
							if (this.Checks.IsGameOutdated())
							{
								// If it does, offer to update it
								Log.Info($"The game is outdated. \n\tLocal version: {this.Config.GetLocalGameVersion()}");
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
					Log.Info($"The launcher is outdated. \n\tLocal version: {this.Config.GetLocalLauncherVersion()}");
					SetLauncherMode(ELauncherMode.Update, false);
				}
			}

			this.IsInitialized = true;
		}

		/// <summary>
		/// Sets the launcher mode and updates UI elements to match
		/// </summary>
		/// <param name="newMode">The new mode.</param>
		/// <param name="bInProgress">If set to <c>true</c>, the selected mode is in progress.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the <see cref="ELauncherMode"/> passed to the function is not a valid value.
		/// </exception>
		private void SetLauncherMode(ELauncherMode newMode, bool bInProgress)
		{
			// Set the global launcher mode
			this.Mode = newMode;

			// Set the UI elements to match
			switch (newMode)
			{
				case ELauncherMode.Install:
				{
					if (bInProgress)
					{
						this.PrimaryButton.Sensitive = false;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Installing...");
					}
					else
					{
						this.PrimaryButton.Sensitive = true;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Install");
					}
					break;
				}
				case ELauncherMode.Update:
				{
					if (bInProgress)
					{
						this.PrimaryButton.Sensitive = false;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Updating...");
					}
					else
					{
						this.PrimaryButton.Sensitive = true;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Update");
					}
					break;
				}
				case ELauncherMode.Repair:
				{
					if (bInProgress)
					{
						this.PrimaryButton.Sensitive = false;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Repairing...");
					}
					else
					{
						this.PrimaryButton.Sensitive = true;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Repair");
					}
					break;
				}
				case ELauncherMode.Launch:
				{
					if (bInProgress)
					{
						this.PrimaryButton.Sensitive = false;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Launching...");
					}
					else
					{
						this.PrimaryButton.Sensitive = true;
						this.PrimaryButton.Label = LocalizationCatalog.GetString("Launch");
					}
					break;
				}
				case ELauncherMode.Inactive:
				{
					this.RepairGameAction.Sensitive = false;

					this.PrimaryButton.Sensitive = false;
					this.PrimaryButton.Label = LocalizationCatalog.GetString("Inactive");
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(newMode), "An invalid launcher mode was passed to SetLauncherMode.");
				}
			}

			if (bInProgress)
			{
				this.RepairGameAction.Sensitive = false;
				this.ReinstallGameAction.Sensitive = false;
			}
			else
			{
				this.RepairGameAction.Sensitive = true;
				this.ReinstallGameAction.Sensitive = true;
			}
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

		/// <summary>
		/// Runs a game repair, no matter what the state the installation is in.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void OnRepairGameActionActivated(object sender, EventArgs e)
		{
			SetLauncherMode(ELauncherMode.Repair, false);

			// Simulate a button press from the user.
			OnPrimaryButtonClicked(this, EventArgs.Empty);

		}

		/// <summary>
		/// Handles switching between different functionalities depending on what is visible on the button to the user, such as
		/// * Installing
		/// * Updating
		/// * Repairing
		/// * Launching
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Empty arguments.</param>
		private void OnPrimaryButtonClicked(object sender, EventArgs e)
		{
			// Drop out if the current platform isn't available on the server
			if (!this.Checks.IsPlatformAvailable(this.Config.GetSystemTarget()))
			{
				this.IndicatorLabel.Text =
					LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
				this.MainProgressBar.Text = "";

				Log.Info($"The server does not provide files for platform \"{ConfigHandler.GetCurrentPlatform()}\". " +
				         "A .provides file must be present in the platforms' root directory.");

				SetLauncherMode(ELauncherMode.Inactive, false);

				return;
			}

			// else, run the relevant function
			switch (this.Mode)
			{
				case ELauncherMode.Repair:
				{
					// Repair the game asynchronously
					SetLauncherMode(ELauncherMode.Repair, true);
					this.Game.VerifyGame();

					break;
				}
				case ELauncherMode.Install:
				{
					// Install the game asynchronously
					SetLauncherMode(ELauncherMode.Install, true);
					this.Game.InstallGame();

					break;
				}
				case ELauncherMode.Update:
				{
					if (this.Checks.IsLauncherOutdated())
					{
						// Update the launcher asynchronously
						SetLauncherMode(ELauncherMode.Update, true);
						this.Launcher.UpdateLauncher();
					}
					else
					{
						// Update the game asynchronously
						SetLauncherMode(ELauncherMode.Update, true);
						this.Game.UpdateGame();
					}

					break;
				}
				case ELauncherMode.Launch:
				{
					this.IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");
					this.MainProgressBar.Text = "";

					SetLauncherMode(ELauncherMode.Launch, true);
					this.Game.LaunchGame();

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
		/// Updates the web browser with the asynchronously loaded changelog from the server.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The arguments containing the HTML from the server.</param>
		private void OnChangelogDownloadFinished(object sender, ChangelogDownloadFinishedEventArgs e)
		{
			//Take the resulting HTML string from the changelog download and send it to the changelog browser
			Application.Invoke(delegate
			{
				this.Browser.LoadHTML(e.HTML, e.URL);
			});
		}

		/// <summary>
		/// Starts the launcher update process when its files have finished downloading.
		/// </summary>
		private static void OnLauncherDownloadFinished(object sender, ModuleInstallationFinishedArgs e)
		{
			Application.Invoke(delegate
			{
				if (e.Module == EModule.Launcher)
				{
					ProcessStartInfo script = LauncherHandler.CreateUpdateScript();

					Process.Start(script);
					Application.Quit();
				}
			});
		}

		/// <summary>
		/// Warns the user when the game fails to launch, and offers to attempt a repair.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Empty event args.</param>
		private void OnGameLaunchFailed(object sender, EventArgs e)
		{
			this.IndicatorLabel.Text = LocalizationCatalog.GetString("The game failed to launch. Try repairing the installation.");
			this.MainProgressBar.Text = "";

			SetLauncherMode(ELauncherMode.Repair, false);
		}

		/// <summary>
		/// Provides alternatives when the game fails to download, either through an update or through an installation.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the type of failure that occurred.</param>
		private void OnGameDownloadFailed(object sender, EventArgs e)
		{
			switch (this.Mode)
			{
				case ELauncherMode.Install:
				case ELauncherMode.Update:
				case ELauncherMode.Repair:
				{
					// Set the mode to the same as it was, but no longer in progress.
					// The modes which fall to this case are all capable of repairing an incomplete or
					// broken install on their own.
					SetLauncherMode(this.Mode, false);
					break;
				}
				default:
				{
					// Other cases (such as Launch) will go to the default mode of Repair.
					SetLauncherMode(ELauncherMode.Repair, false);
					break;
				}
			}
		}

		/// <summary>
		/// Updates the progress bar and progress label during installations, repairs and updates.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the progress values and current filename.</param>
		private void OnModuleInstallationProgressChanged(object sender, ModuleProgressChangedArgs e)
		{
			Application.Invoke(delegate
			{
				this.MainProgressBar.Text = e.ProgressBarMessage;
				this.IndicatorLabel.Text = e.IndicatorLabelMessage;
				this.MainProgressBar.Fraction = e.ProgressFraction;
			});
		}

		/// <summary>
		/// Allows the user to launch or repair the game once installation finishes.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the result of the download.</param>
		private void OnGameDownloadFinished(object sender, EventArgs e)
		{
			Application.Invoke(delegate
			{
				this.IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");

				switch (this.Mode)
				{
					case ELauncherMode.Install:
					{
						this.MainProgressBar.Text = LocalizationCatalog.GetString("Installation finished");
						break;
					}
					case ELauncherMode.Update:
					{
						this.MainProgressBar.Text = LocalizationCatalog.GetString("Update finished");
						break;
					}
					case ELauncherMode.Repair:
					{
						this.MainProgressBar.Text = LocalizationCatalog.GetString("Repair finished");
						break;
					}
					default:
					{
						this.MainProgressBar.Text = "";
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
		private void OnGameExited(object sender, GameExitEventArgs e)
		{
			if (e.ExitCode != 0)
			{
				MessageDialog crashDialog = new MessageDialog
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
					OnPrimaryButtonClicked(this, EventArgs.Empty);
				}
				else
				{
					SetLauncherMode(ELauncherMode.Launch, false);
				}
				crashDialog.Destroy();
			}
			else
			{
				SetLauncherMode(ELauncherMode.Launch, false);
			}
		}

		/// <summary>
		/// Handles starting of a reinstallation procedure as requested by the user.
		/// </summary>
		private void OnReinstallGameActionActivated(object sender, EventArgs e)
		{
			MessageDialog reinstallConfirmDialog = new MessageDialog
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

			if (reinstallConfirmDialog.Run() == (int) ResponseType.Yes)
			{
				SetLauncherMode(ELauncherMode.Install, true);
				this.Game.ReinstallGame();
			}
			reinstallConfirmDialog.Destroy();
		}
	}
}