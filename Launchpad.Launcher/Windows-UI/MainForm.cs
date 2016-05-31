//
//  MainForm.cs
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
using System.Windows.Forms;
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Handlers.Protocols;
using NGettext;
using System.Diagnostics;
using log4net;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.WindowsUI
{
	internal partial class MainForm : Form
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(MainForm));

		/// <summary>
		/// The config handler reference.
		/// </summary>
		private readonly ConfigHandler Config = ConfigHandler.Instance;

		/// <summary>
		/// The checks handler reference.
		/// </summary>
		private readonly ChecksHandler Checks;

		/// <summary>
		/// The launcher handler. Allows updating the launcher and loading the changelog
		/// </summary>
		private readonly LauncherHandler Launcher;

		/// <summary>
		/// The game handler. Allows updating, installing and repairing the game.
		/// </summary>
		private readonly GameHandler Game;

		/// <summary>
		/// The current mode that the launcher is in. Determines what the primary button does when pressed.
		/// </summary>
		ELauncherMode Mode = ELauncherMode.Inactive;

		// Initialize localization
		private static readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./locale");

		public MainForm()
		{
			InitializeComponent();
			InitializeLocalizedStrings();

			Checks = new ChecksHandler();
			Launcher = new LauncherHandler();
			Game = new GameHandler();

			SetLauncherMode(ELauncherMode.Inactive, false);
			MessageLabel.Text = LocalizationCatalog.GetString("Idle");

			downloadProgressLabel.Text = String.Empty;

			//set the window text to match the game name
			this.Text = "Launchpad - " + Config.GetGameName();

			//first of all, check if we can connect to the FTP server.
			if (!Checks.CanPatch())
			{
				MessageBox.Show(
					this,
					LocalizationCatalog.GetString("Failed to connect to the patch server. Please check your settings."),
					LocalizationCatalog.GetString("Could not connect to server."),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button1);

				MessageLabel.Text = LocalizationCatalog.GetString("Could not connect to server.");
				PrimaryButton.Text = ":(";
				PrimaryButton.Enabled = false;
			}
			else
			{
				//if we can connect, proceed with the rest of our checks.
				if (ChecksHandler.IsInitialStartup())
				{
					Log.Info("This instance is the first start of the application in this folder.");

					DialogResult shouldInstallHere = MessageBox.Show(
						                                 this,
						                                 String.Format(
							                                 LocalizationCatalog.GetString("This appears to be the first time you're starting the launcher.\n" +
								                                 "Is this the location where you would like to install the game?" +
								                                 "\n\n{0}"), ConfigHandler.GetLocalDir()),
						                                 LocalizationCatalog.GetString("Initial startup"),
						                                 MessageBoxButtons.YesNo,
						                                 MessageBoxIcon.Question,
						                                 MessageBoxDefaultButton.Button1);

					if (shouldInstallHere == DialogResult.Yes)
					{
						// Yes, install here
						Log.Info("User accepted installation in this directory. Installing in current directory.");

						ConfigHandler.CreateUpdateCookie();
					}
					else
					{
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
					changelogBrowser.Navigate(Config.GetChangelogURL());
				}
				else
				{
					Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;
					Launcher.LoadFallbackChangelog();
				}

				//Does the launcher need an update?
				if (!Checks.IsLauncherOutdated())
				{
					if (!Checks.IsGameInstalled())
					{
						Log.Info("The game has not yet been installed.");
						SetLauncherMode(ELauncherMode.Install, false);
					}
					else
					{
						if (Checks.IsGameOutdated())
						{
							Log.Info(String.Format("The game is outdated. \n\tLocal version: {0}", Config.GetLocalGameVersion()));
							SetLauncherMode(ELauncherMode.Update, false);
						}
						else
						{
							Log.Info("All checks passed. Game can be launched.");
							SetLauncherMode(ELauncherMode.Launch, false);
						}
					}
				}
				else
				{
					Log.Info(String.Format("The launcher is outdated. \n\tLocal version: {0}", Config.GetLocalLauncherVersion()));
					SetLauncherMode(ELauncherMode.Update, false);
				}
			}
		}

		/// <summary>
		/// Initializes the localized strings for different UI elements.
		/// </summary>
		private void InitializeLocalizedStrings()
		{
			this.MessageLabel.Text = LocalizationCatalog.GetString("Idle");
			this.aboutLink.Text = LocalizationCatalog.GetString("About");
			this.PrimaryButton.Text = LocalizationCatalog.GetString("Inactive");
			this.Text = LocalizationCatalog.GetString("Launchpad - <GameName>");
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
		private void mainButton_Click(object sender, EventArgs e)
		{
			switch (Mode)
			{
				case ELauncherMode.Repair:
					{
						//bind events for UI updating
						Game.ProgressChanged += OnModuleInstallationProgressChanged;
						Game.GameDownloadFinished += OnGameDownloadFinished;
						Game.GameDownloadFailed += OnGameDownloadFailed;

						if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
						{
							//repair the game asynchronously
							Game.VerifyGame();
							SetLauncherMode(ELauncherMode.Repair, true);
						}
						else
						{
							//whoops, the server doesn't provide the game for the platform we requested (usually the on we're running on)
							//alert the user and revert back to the default install mode
							Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Launcher.Resources.RocketIcon.ico");
							if (iconStream != null)
							{
								NotifyIcon platformNotProvidedNotification = new NotifyIcon();
								platformNotProvidedNotification.Icon = new System.Drawing.Icon(iconStream);
								platformNotProvidedNotification.Visible = true;

								platformNotProvidedNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Platform not provided!");
								platformNotProvidedNotification.BalloonTipText = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");

								platformNotProvidedNotification.ShowBalloonTip(10000);
							}
							Log.Info(String.Format("The server does not provide files for platform \"{0}\". A .provides file must be present in the platforms' root directory.",
									ConfigHandler.GetCurrentPlatform()));

							MessageLabel.Text = LocalizationCatalog.GetString("The server does not provide files for the selected platform.");
							SetLauncherMode(ELauncherMode.Install, false);
						}
						break;
					}
				case ELauncherMode.Install:
					{
						//bind events for UI updating
						Game.ProgressChanged += OnModuleInstallationProgressChanged;
						Game.GameDownloadFinished += OnGameDownloadFinished;
						Game.GameDownloadFailed += OnGameDownloadFailed;

						if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
						{
							//install the game asynchronously
							SetLauncherMode(ELauncherMode.Install, true);
							Game.InstallGame();
						}
						else
						{
							//whoops, the server doesn't provide the game for the platform we requested (usually the on we're running on)
							//alert the user and revert back to the default install mode
							Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Launcher.Resources.RocketIcon.ico");
							if (iconStream != null)
							{
								NotifyIcon platformNotProvidedNotification = new NotifyIcon();
								platformNotProvidedNotification.Icon = new System.Drawing.Icon(iconStream);
								platformNotProvidedNotification.Visible = true;

								platformNotProvidedNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Platform not provided!");
								platformNotProvidedNotification.BalloonTipText = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");

								platformNotProvidedNotification.ShowBalloonTip(10000);
							}
							Log.Info(String.Format("The server does not provide files for platform \"{0}\". A .provides file must be present in the platforms' root directory.",
									ConfigHandler.GetCurrentPlatform()));

							MessageLabel.Text = LocalizationCatalog.GetString("The server does not provide files for the selected platform.");
							SetLauncherMode(ELauncherMode.Install, false);
						}

						break;
					}
				case ELauncherMode.Update:
					{
						//bind events for UI updating
						Game.ProgressChanged += OnModuleInstallationProgressChanged;
						Game.GameDownloadFinished += OnGameDownloadFinished;
						Game.GameDownloadFailed += OnGameDownloadFailed;

						if (Checks.IsLauncherOutdated())
						{
							//update the launcher synchronously.
							SetLauncherMode(ELauncherMode.Update, true);
							Launcher.LauncherDownloadFinished += OnLauncherDownloadFinished;
							Launcher.LauncherDownloadProgressChanged += OnModuleInstallationProgressChanged;
							Launcher.UpdateLauncher();
						}
						else
						{
							if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
							{
								//update the game asynchronously
								SetLauncherMode(ELauncherMode.Update, true);
								Game.UpdateGame();
							}
							else
							{
								//whoops, the server doesn't provide the game for the platform we requested (usually the on we're running on)
								//alert the user and revert back to the default install mode
								Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Launcher.Resources.RocketIcon.ico");
								if (iconStream != null)
								{
									NotifyIcon platformNotProvidedNotification = new NotifyIcon();
									platformNotProvidedNotification.Icon = new System.Drawing.Icon(iconStream);
									platformNotProvidedNotification.Visible = true;

									platformNotProvidedNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Platform not provided!");
									platformNotProvidedNotification.BalloonTipText = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");

									platformNotProvidedNotification.ShowBalloonTip(10000);
								}
								Log.Info(String.Format("The server does not provide files for platform \"{0}\". A .provides file must be present in the platforms' root directory.",
										ConfigHandler.GetCurrentPlatform()));

								MessageLabel.Text = LocalizationCatalog.GetString("The server does not provide files for the selected platform.");
								SetLauncherMode(ELauncherMode.Install, false);
							}
						}

						break;
					}
				case ELauncherMode.Launch:
					{
						Game.GameLaunchFailed += OnGameLaunchFailed;
						Game.GameExited += OnGameExited;

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
		/// Sets the launcher mode and updates UI elements to match
		/// </summary>
		/// <param name="newMode">New mode.</param>
		/// <param name="bInProgress">If set to <c>true</c>, the selected mode is in progress.</param>
		private void SetLauncherMode(ELauncherMode newMode, bool bInProgress)
		{
			//set the global launcher mode
			Mode = newMode;

			//set the UI elements to match
			switch (newMode)
			{
				case ELauncherMode.Install:
					{
						if (bInProgress)
						{
							PrimaryButton.Enabled = false;
							PrimaryButton.Text = LocalizationCatalog.GetString("Installing...");
						}
						else
						{
							PrimaryButton.Enabled = true;
							PrimaryButton.Text = LocalizationCatalog.GetString("Install");
						}
						break;
					}
				case ELauncherMode.Update:
					{
						if (bInProgress)
						{
							PrimaryButton.Enabled = false;
							PrimaryButton.Text = LocalizationCatalog.GetString("Updating...");
						}
						else
						{
							PrimaryButton.Enabled = true;
							PrimaryButton.Text = LocalizationCatalog.GetString("Update");
						}
						break;
					}
				case ELauncherMode.Repair:
					{
						if (bInProgress)
						{
							PrimaryButton.Enabled = false;
							PrimaryButton.Text = LocalizationCatalog.GetString("Repairing...");
						}
						else
						{
							PrimaryButton.Enabled = true;
							PrimaryButton.Text = LocalizationCatalog.GetString("Repair");
						}
						break;
					}
				case ELauncherMode.Launch:
					{
						if (bInProgress)
						{
							PrimaryButton.Enabled = false;
							PrimaryButton.Text = LocalizationCatalog.GetString("Launching...");
						}
						else
						{
							PrimaryButton.Enabled = true;
							PrimaryButton.Text = LocalizationCatalog.GetString("Launch");
						}
						break;
					}
				case ELauncherMode.Inactive:
					{
						PrimaryButton.Enabled = false;
						PrimaryButton.Text = LocalizationCatalog.GetString("Inactive");
						break;
					}
				default:
					{
						throw new ArgumentOutOfRangeException("newMode", "Invalid mode was passed to SetLauncherMode");
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
			changelogBrowser.DocumentText = e.HTML;
			changelogBrowser.Url = new Uri(e.URL);
			changelogBrowser.Refresh();
		}

		/// <summary>
		/// Warns the user when the game fails to launch, and offers to attempt a repair.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Empty event args.</param>
		private void OnGameLaunchFailed(object sender, EventArgs e)
		{
			this.Invoke((MethodInvoker)delegate
				{
					Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Launcher.Resources.RocketIcon.ico");
					if (iconStream != null)
					{
						NotifyIcon launchFailedNotification = new NotifyIcon();
						launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
						launchFailedNotification.Visible = true;

						launchFailedNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Failed to launch the game.");
						launchFailedNotification.BalloonTipText = LocalizationCatalog.GetString("The game failed to launch. Try repairing the installation.");

						launchFailedNotification.ShowBalloonTip(10000);
					}

					SetLauncherMode(ELauncherMode.Repair, false);
				});
		}

		/// <summary>
		/// Provides alternatives when the game fails to download, either through an update or through an installation.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the type of failure that occurred.</param>
		private void OnGameDownloadFailed(object sender, EventArgs e)
		{
			this.Invoke((MethodInvoker)delegate
				{
					switch (Mode)
					{
						case ELauncherMode.Install:
							{
								SetLauncherMode(Mode, false);
								break;
							}
						case ELauncherMode.Update:
							{
								SetLauncherMode(Mode, false);
								break;
							}
						case ELauncherMode.Repair:
							{
								SetLauncherMode(Mode, false);
								break;
							}
						default:
							{
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
		protected void OnModuleInstallationProgressChanged(object sender, ModuleProgressChangedArgs e)
		{
			this.Invoke((MethodInvoker)delegate
				{
					MessageLabel.Text = e.IndicatorLabelMessage;
					downloadProgressLabel.Text = e.ProgressBarMessage;

					mainProgressBar.Minimum = 0;
					mainProgressBar.Maximum = 10000;

					double fraction = e.ProgressFraction * 10000;
					// HACK: Clamping the value, it goes bonkers sometimes and explodes into huge values.
					mainProgressBar.Value = ((int)fraction).Clamp(mainProgressBar.Minimum, mainProgressBar.Maximum);
					mainProgressBar.Update();

				});
		}

		protected void OnLauncherDownloadFinished(object sender, ModuleInstallationFinishedArgs e)
		{
			this.Invoke((MethodInvoker)delegate
				{
					if (e.Module == EModule.Launcher)
					{
						ProcessStartInfo script = LauncherHandler.CreateUpdateScript();

						Process.Start(script);
						Application.Exit();
					}
				});
		}

		/// <summary>
		/// Allows the user to launch or repair the game once installation finishes.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the result of the download.</param>
		protected void OnGameDownloadFinished(object sender, EventArgs e)
		{
			this.Invoke((MethodInvoker)delegate
				{
					MessageLabel.Text = LocalizationCatalog.GetString("Idle");
					downloadProgressLabel.Text = "";

					NotifyIcon downloadCompleteNotification = new NotifyIcon();
					downloadCompleteNotification.Visible = true;

					using (Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Launcher.Resources.RocketIcon.ico"))
					{
						if (iconStream != null)
						{
							downloadCompleteNotification.Icon = new System.Drawing.Icon(iconStream);
						}
					}

					switch (Mode)
					{
						case ELauncherMode.Install:
							{
								downloadCompleteNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Info");
								downloadCompleteNotification.BalloonTipText = LocalizationCatalog.GetString("Game download finished. Play away!");
								break;
							}
						case ELauncherMode.Repair:
							{
								downloadCompleteNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Info");
								downloadCompleteNotification.BalloonTipText = LocalizationCatalog.GetString("Launchpad has finished repairing the game installation. Play away!");
								break;
							}
						case ELauncherMode.Update:
							{
								downloadCompleteNotification.BalloonTipTitle = LocalizationCatalog.GetString("Launchpad - Info");
								downloadCompleteNotification.BalloonTipText = LocalizationCatalog.GetString("Game update finished. Play away!");
								break;
							}
						default:
							{
								break;
							}
					}

					downloadCompleteNotification.ShowBalloonTip(10000);
					SetLauncherMode(ELauncherMode.Launch, false);
				});
		}

		/// <summary>
		/// Passes the update finished event to a generic handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the result of the download.</param>
		private void OnGameUpdateFinished(object sender, EventArgs e)
		{
			OnGameDownloadFinished(sender, e);
		}

		private void aboutLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			LaunchpadAboutBox about = new LaunchpadAboutBox();
			about.ShowDialog();
		}

		private void OnGameExited(object sender, GameExitEventArgs e)
		{
			if (e.ExitCode != 0)
			{
				SetLauncherMode(ELauncherMode.Repair, false);
			}
			else
			{
				SetLauncherMode(ELauncherMode.Launch, false);
			}
		}
	}
}
