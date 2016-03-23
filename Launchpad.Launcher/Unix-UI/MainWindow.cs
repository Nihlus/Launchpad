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
using Notifications;
using System;
using WebKit;
using NGettext;
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Handlers.Protocols;

namespace Launchpad.Launcher.UnixUI
{
	[CLSCompliant(false)]
	public partial class MainWindow : Window
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

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
		/// The changelog browser.
		/// </summary>
		WebView Browser = new WebView();

		/// <summary>
		/// The current mode that the launcher is in. Determines what the primary button does when pressed.
		/// </summary>
		ELauncherMode Mode = ELauncherMode.Inactive;

		//initialize localization
		private readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./locale");

		public MainWindow()
			: base(WindowType.Toplevel)
		{					
			//Initialize the config files and check values.
			Config.Initialize();

			// The config must be initialized before the handlers can be instantiated
			Checks = new ChecksHandler();
			Launcher = new LauncherHandler();
			Game = new GameHandler();

			//Initialize the GTK UI
			this.Build();

			// Set the initial launcher mode
			SetLauncherMode(ELauncherMode.Inactive, false);

			//set the window title
			Title = "Launchpad - " + Config.GetGameName();

			ScrolledBrowserWindow.Add(Browser);
			ScrolledBrowserWindow.ShowAll();

			IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");

			//First of all, check if we can connect to the FTP server.
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
			}
			else
			{
				//if we can connect, proceed with the rest of our checks.
				if (ChecksHandler.IsInitialStartup())
				{
					MessageDialog shouldInstallHereDialog = new MessageDialog(
						                                        null, 
						                                        DialogFlags.Modal, 
						                                        MessageType.Question, 
						                                        ButtonsType.OkCancel, 
						                                        String.Format(LocalizationCatalog.GetString(
								                                        "This appears to be the first time you're starting the launcher.\n" +
								                                        "Is this the location where you would like to install the game?" +
								                                        "\n\n{0}"), ConfigHandler.GetLocalDir()
						                                        ));

					if (shouldInstallHereDialog.Run() == (int)ResponseType.Ok)
					{
						shouldInstallHereDialog.Destroy();
						//yes, install here
						Console.WriteLine("Installing in current directory.");
						ConfigHandler.CreateUpdateCookie();
					}
					else
					{
						shouldInstallHereDialog.Destroy();
						//no, don't install here
						Console.WriteLine("Exiting...");
						Environment.Exit(0);
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
					Browser.Open(Config.GetChangelogURL());
				}
				else
				{
					Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;
					Launcher.LoadFallbackChangelog();
				}

				// If the launcher does not need an update at this point, we can continue checks for the game
				if (!Checks.IsLauncherOutdated())
				{
					if (!Checks.IsGameInstalled())
					{
						//if the game is not installed, offer to install it
						Console.WriteLine("Not installed.");
						SetLauncherMode(ELauncherMode.Install, false);
					}
					else
					{
						//if the game is installed (which it should be at this point), check if it needs to be updated
						if (Checks.IsGameOutdated())
						{
							//if it does, offer to update it
							Console.WriteLine("Game is outdated or not installed");
							SetLauncherMode(ELauncherMode.Update, false);
						}
						else
						{
							//if not, enable launching the game
							SetLauncherMode(ELauncherMode.Launch, false);
						}
					}
				}
				else
				{
					//the launcher was outdated.
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
			//set the global launcher mode
			Mode = newMode;

			//set the UI elements to match
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
						throw new ArgumentOutOfRangeException("newMode", "Invalid mode was passed to SetLauncherMode");
					}
			}
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

		/// <summary>
		/// Runs a game repair, no matter what the state the installation is in.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnRepairGameActionActivated(object sender, EventArgs e)
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
		protected void OnPrimaryButtonClicked(object sender, EventArgs e)
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
							//Repair the game asynchronously
							SetLauncherMode(ELauncherMode.Repair, true);
							Game.VerifyGame();                        
						}
						else
						{
							Notification noProvide = new Notification();
							noProvide.IconName = Stock.DialogError;
							noProvide.Summary = LocalizationCatalog.GetString("Launchpad - Platform not provided!");
							noProvide.Body = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
							noProvide.Show();

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
							Notification noProvide = new Notification();
							noProvide.IconName = Stock.DialogError;
							noProvide.Summary = LocalizationCatalog.GetString("Launchpad - Platform not provided!");
							noProvide.Body = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
							noProvide.Show();

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
							//bind events for UI updating
							Game.ProgressChanged += OnModuleInstallationProgressChanged;
							Game.GameDownloadFinished += OnGameDownloadFinished;
							Game.GameDownloadFailed += OnGameDownloadFailed;

							//update the game asynchronously
							if (Checks.IsPlatformAvailable(Config.GetSystemTarget()))
							{
								//install the game asynchronously
								SetLauncherMode(ELauncherMode.Update, true);
								Game.UpdateGame();                            
							}
							else
							{
								Notification noProvide = new Notification();
								noProvide.IconName = Stock.DialogError;
								noProvide.Summary = LocalizationCatalog.GetString("Launchpad - Platform not provided!");
								noProvide.Body = LocalizationCatalog.GetString("The server does not provide the game for the selected platform.");
								noProvide.Show();

								SetLauncherMode(ELauncherMode.Install, false);
							}								
						}												
						break;
					}
				case ELauncherMode.Launch:
					{
						Game.GameLaunchFailed += OnGameLaunchFailed;
						Game.GameExited += OnGameExited;

						//events such as LaunchFailed can fire before this has finished
						//thus, we set the mode before the actual launching of the game.
						SetLauncherMode(ELauncherMode.Launch, true);
						Game.LaunchGame();
					
						break;
					}
				default:
					{
						Console.WriteLine("No functionality for this mode.");
						break;
					}
			}
		}

		/// <summary>
		/// Updates the web browser with the asynchronously loaded changelog from the server.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The arguments containing the HTML from the server.</param>
		protected void OnChangelogDownloadFinished(object sender, ChangelogDownloadFinishedEventArgs e)
		{
			//Take the resulting HTML string from the changelog download and send it to the changelog browser
			Application.Invoke(delegate
				{
					Browser.LoadHtmlString(e.HTML, e.URL);
				});
		}

		/// <summary>
		/// Warns the user when the game fails to launch, and offers to attempt a repair.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Empty event args.</param>
		private void OnGameLaunchFailed(object sender, EventArgs e)
		{
			Notification launchFailed = new Notification();
			launchFailed.IconName = Stock.DialogError;
			launchFailed.Summary = LocalizationCatalog.GetString("Launchpad - Failed to launch the game.");
			launchFailed.Body = LocalizationCatalog.GetString("The game failed to launch. Try repairing the installation.");
			launchFailed.Show();

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
		protected void OnModuleInstallationProgressChanged(object sender, ModuleProgressChangedArgs e)
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
		protected void OnGameDownloadFinished(object sender, EventArgs e)
		{
			Application.Invoke(delegate
				{
					IndicatorLabel.Text = LocalizationCatalog.GetString("Idle");
					MainProgressBar.Text = "";

					Notification downloadCompleteNotification = new Notification();
					downloadCompleteNotification.IconName = Stock.Info;

					switch (Mode)
					{
						case ELauncherMode.Install:
							{
								downloadCompleteNotification.Summary = LocalizationCatalog.GetString("Launchpad - Info");
								downloadCompleteNotification.Body = LocalizationCatalog.GetString("Game download finished. Play away!");
								break;
							}
						case ELauncherMode.Repair:
							{
								downloadCompleteNotification.Summary = LocalizationCatalog.GetString("Launchpad - Info");
								downloadCompleteNotification.Body = LocalizationCatalog.GetString("Launchpad has finished repairing the game installation. Play away!");
								break;
							}
						case ELauncherMode.Update:
							{								
								downloadCompleteNotification.Summary = LocalizationCatalog.GetString("Launchpad - Info");
								downloadCompleteNotification.Body = LocalizationCatalog.GetString("Game update finished. Play away!");
								break;
							}
						default:
							{								
								break;
							}
					}

					downloadCompleteNotification.Show();
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
					                            String.Format(LocalizationCatalog.GetString(
							                            "Whoops! The game appears to have crashed.\n" +
							                            "Would you like the launcher to verify the installation?"
						                            )));

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

