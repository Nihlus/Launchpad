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
	[CLSCompliant(false)]
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
		private static readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./locale");

		public MainWindow()
			: base(Gtk.WindowType.Toplevel)
		{
			// Initialize the GTK UI
			this.Build();

			// Bind the handler events
			Game.ProgressChanged += OnModuleInstallationProgressChanged;
			Game.GameDownloadFinished += OnGameDownloadFinished;
			Game.GameDownloadFailed += OnGameDownloadFailed;
			Game.GameLaunchFailed += OnGameLaunchFailed;
            Game.GameExited += OnGameExited;

			Launcher.LauncherDownloadProgressChanged += OnModuleInstallationProgressChanged;
			Launcher.LauncherDownloadFinished += OnLauncherDownloadFinished;
			Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;


			// Set the initial launcher mode
			SetLauncherMode(ELauncherMode.Inactive, false);

			// Set the window title
			Title = LocalizationCatalog.GetString("Launchpad - ") + Config.GetGameName();

			// Create a new changelog widget, and add it to the scrolled window
			this.Browser = new Changelog(this.ScrolledBrowserWindow);
			ScrolledBrowserWindow.ShowAll();

			IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");

			// First of all, check if we can connect to the patching service.
			if (!Checks.CanPatch())
			{
				MessageDialog dialog = new MessageDialog(
					                       null,
					                       DialogFlags.Modal,
					                       MessageType.Warning,
					                       ButtonsType.Ok,
					                       LocalizationCatalog.GetString("Failed to connect to the patch server. Please check your settings."));

				dialog.Run();

				dialog.Destroy();
				IndicatorLabel.Text = LocalizationCatalog.GetString("Could not connect to server.");
				refreshAction1.Sensitive = false;
			}
			else
			{
				// TODO: Load this asynchronously
				// Load the game banner (if there is one)
				if (Config.GetPatchProtocol().CanProvideBanner())
				{
					using (MemoryStream bannerStream = new MemoryStream())
					{
						// Fetch the banner from the server
						Config.GetPatchProtocol().GetBanner().Save(bannerStream, ImageFormat.Png);

						// Load the image into a pixel buffer
						bannerStream.Position = 0;
						this.GameBanner.Pixbuf = new Pixbuf(bannerStream);
					}
				}

				// If we can connect, proceed with the rest of our checks.
				if (ChecksHandler.IsInitialStartup())
				{
					Log.Info("This instance is the first start of the application in this folder.");

					MessageDialog shouldInstallHereDialog = new MessageDialog(
						                                        null,
						                                        DialogFlags.Modal,
						                                        MessageType.Question,
						                                        ButtonsType.OkCancel,
						                                        LocalizationCatalog.GetString(
								                                        "This appears to be the first time you're starting the launcher.\n" +
								                                        "Is this the location where you would like to install the game?") +
								                                        $"\n\n{ConfigHandler.GetLocalDir()}"
						                                        );

					if (shouldInstallHereDialog.Run() == (int)ResponseType.Ok)
					{
						shouldInstallHereDialog.Destroy();

						// Yes, install here
						Log.Info("User accepted installation in this directory. Installing in current directory.");

						ConfigHandler.CreateUpdateCookie();
					}
					else
					{
						shouldInstallHereDialog.Destroy();

						// No, don't install here
						Log.Info("User declined installation in this directory. Exiting...");
						Environment.Exit(2);
					}
				}

				if (Config.ShouldAllowAnonymousStats())
				{
					StatsHandler.SendUsageStats();
				}

				// Load the changelog. Try a direct URL first, and a protocol-specific
				// implementation after.
				if (Launcher.CanAccessStandardChangelog())
				{
					Browser.Navigate(Config.GetChangelogURL());
				}
				else
				{
					Launcher.LoadFallbackChangelog();
				}

				// If the launcher does not need an update at this point, we can continue checks for the game
				if (!Checks.IsLauncherOutdated())
				{
					if (!Checks.IsGameInstalled())
					{
						// If the game is not installed, offer to install it
						Log.Info("The game has not yet been installed.");
						SetLauncherMode(ELauncherMode.Install, false);
					}
					else
					{
						// If the game is installed (which it should be at this point), check if it needs to be updated
						if (Checks.IsGameOutdated())
						{
							// If it does, offer to update it
							Log.Info($"The game is outdated. \n\tLocal version: {Config.GetLocalGameVersion()}");
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
				else
				{
					// The launcher was outdated.
					Log.Info($"The launcher is outdated. \n\tLocal version: {Config.GetLocalLauncherVersion()}");
					SetLauncherMode(ELauncherMode.Update, false);
				}
			}
		}

		/// <summary>
		/// Sets the launcher mode and updates UI elements to match
		/// </summary>
		/// <param name="newMode">New mode.</param>
		/// <param name="bInProgress">If set to <c>true</c>, the selected mode is in progress.</param>
		private void SetLauncherMode(ELauncherMode newMode, bool bInProgress)
		{
			// Set the global launcher mode
			Mode = newMode;

			// Set the UI elements to match
			switch (newMode)
			{
				case ELauncherMode.Install:
					{
						if (bInProgress)
						{
							PrimaryButton.Sensitive = false;
							PrimaryButton.Label = LocalizationCatalog.GetString("Installing...");
						}
						else
						{
							PrimaryButton.Sensitive = true;
							PrimaryButton.Label = LocalizationCatalog.GetString("Install");
						}
						break;
					}
				case ELauncherMode.Update:
					{
						if (bInProgress)
						{
							PrimaryButton.Sensitive = false;
							PrimaryButton.Label = LocalizationCatalog.GetString("Updating...");
						}
						else
						{
							PrimaryButton.Sensitive = true;
							PrimaryButton.Label = LocalizationCatalog.GetString("Update");
						}
						break;
					}
				case ELauncherMode.Repair:
					{
						if (bInProgress)
						{
							PrimaryButton.Sensitive = false;
							PrimaryButton.Label = LocalizationCatalog.GetString("Repairing...");
						}
						else
						{
							PrimaryButton.Sensitive = true;
							PrimaryButton.Label = LocalizationCatalog.GetString("Repair");
						}
						break;
					}
				case ELauncherMode.Launch:
					{
						if (bInProgress)
						{
							PrimaryButton.Sensitive = false;
							PrimaryButton.Label = LocalizationCatalog.GetString("Launching...");
						}
						else
						{
							PrimaryButton.Sensitive = true;
							PrimaryButton.Label = LocalizationCatalog.GetString("Launch");
						}
						break;
					}
				case ELauncherMode.Inactive:
					{
						PrimaryButton.Sensitive = false;
						PrimaryButton.Label = LocalizationCatalog.GetString("Inactive");
						break;
					}
				default:
					{
						throw new ArgumentOutOfRangeException(nameof(newMode), "Invalid mode was passed to SetLauncherMode");
					}
			}
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		private void OnDeleteEvent(object sender, DeleteEventArgs a)
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
			switch (Mode)
			{
				case ELauncherMode.Repair:
				{
					if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
					{
						// Repair the game asynchronously
						SetLauncherMode(ELauncherMode.Repair, true);
						Game.VerifyGame();
					}
					else
					{
						IndicatorLabel.Text =
							LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
						MainProgressBar.Text = "";

						Log.Info($"The server does not provide files for platform \"{ConfigHandler.GetCurrentPlatform()}\". " +
							"A .provides file must be present in the platforms' root directory.");

						SetLauncherMode(ELauncherMode.Install, false);
					}
					break;
				}
				case ELauncherMode.Install:
				{
					//check for a .provides file in the platform directory on the server
					//if there is none, the server does not provide a game for that platform
					if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
					{
						//install the game asynchronously
						SetLauncherMode(ELauncherMode.Install, true);
						Game.InstallGame();
					}
					else
					{
						IndicatorLabel.Text =
							LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
						MainProgressBar.Text = "";

						Log.Info($"The server does not provide files for platform \"{ConfigHandler.GetCurrentPlatform()}\". " +
														"A .provides file must be present in the platforms' root directory.");

						SetLauncherMode(ELauncherMode.Install, false);
					}
					break;
				}
				case ELauncherMode.Update:
				{
					if (Checks.IsLauncherOutdated())
					{
						SetLauncherMode(ELauncherMode.Update, true);
						Launcher.UpdateLauncher();
					}
					else
					{
						// Uppdate the game asynchronously
						if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
						{
							// Install the game asynchronously
							SetLauncherMode(ELauncherMode.Update, true);
							Game.UpdateGame();
						}
						else
						{
							IndicatorLabel.Text = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
							MainProgressBar.Text = "";

							Log.Info($"The server does not provide files for platform \"{ConfigHandler.GetCurrentPlatform()}\". " +
															"A .provides file must be present in the platforms' root directory.");

							SetLauncherMode(ELauncherMode.Install, false);
						}
					}
					break;
				}
				case ELauncherMode.Launch:
				{
					IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");
					MainProgressBar.Text = "";

					// Events such as LaunchFailed can fire before this has finished
					// Thus, we set the mode before the actual launching of the game.
					SetLauncherMode(ELauncherMode.Launch, true);
					Game.LaunchGame();

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
					Browser.LoadHTML(e.HTML, e.URL);
				});
		}

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
			IndicatorLabel.Text = LocalizationCatalog.GetString("The game failed to launch. Try repairing the installation.");
			MainProgressBar.Text = "";

			SetLauncherMode(ELauncherMode.Repair, false);
		}

		//TODO: Rework
		/// <summary>
		/// Provides alternatives when the game fails to download, either through an update or through an installation.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the type of failure that occurred.</param>
		private void OnGameDownloadFailed(object sender, EventArgs e)
		{
			switch (Mode)
			{
				case ELauncherMode.Install:
					{
						break;
					}
				case ELauncherMode.Update:
					{
						break;
					}
				case ELauncherMode.Repair:
					{
						break;
					}
				default:
					{
						SetLauncherMode(ELauncherMode.Repair, false);
						break;
					}
			}

			SetLauncherMode(Mode, false);
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
					MainProgressBar.Text = e.ProgressBarMessage;
					IndicatorLabel.Text = e.IndicatorLabelMessage;
					MainProgressBar.Fraction = e.ProgressFraction;
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
					IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");

					switch (Mode)
					{
						case ELauncherMode.Install:
						{
							MainProgressBar.Text = LocalizationCatalog.GetString("Installation finished");
							break;
						}
						case ELauncherMode.Update:
						{
							MainProgressBar.Text = LocalizationCatalog.GetString("Update finished");
							break;
						}
						case ELauncherMode.Repair:
						{
							MainProgressBar.Text = LocalizationCatalog.GetString("Repair finished");
							break;
						}
						default:
						{
							MainProgressBar.Text = "";
							break;
						}
					}

					SetLauncherMode(ELauncherMode.Launch, false);
				});
		}

		private void OnGameExited(object sender, GameExitEventArgs e)
		{
			if (e.ExitCode != 0)
			{
				MessageDialog crashDialog = new MessageDialog(
										this,
										DialogFlags.Modal,
										MessageType.Question,
										ButtonsType.YesNo,
										LocalizationCatalog.GetString(
												"Whoops! The game appears to have crashed.\n" +
												"Would you like the launcher to verify the installation?"
											));

				if (crashDialog.Run() == (int)ResponseType.Yes)
				{
					SetLauncherMode(ELauncherMode.Repair, false);
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
		}
	}
}

