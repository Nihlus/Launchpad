using Gtk;
using Notifications;
using System;
using WebKit;
using Launchpad.Launcher.Events.Arguments;

namespace Launchpad.Launcher
{
    [CLSCompliant(false)]
    public partial class MainWindow : Window
	{        
		/// <summary>
		/// The checks handler reference.
		/// </summary>
		ChecksHandler Checks = new ChecksHandler ();

		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// The launcher handler. Allows updating the launcher and loading the changelog
		/// </summary>
		LauncherHandler Launcher = new LauncherHandler ();

		/// <summary>
		/// The game handler. Allows updating, installing and repairing the game.
		/// </summary>
		GameHandler Game = new GameHandler();

		/// <summary>
		/// The changelog browser.
		/// </summary>
		WebView Browser = new WebView ();

        /// <summary>
        /// The current mode that the launcher is in. Determines what the primary button does when pressed.
        /// </summary>
        ELauncherMode Mode = ELauncherMode.Invalid;
		//this section sends some anonymous usage stats back home. If you don't want to do this for your game, simply change this boolean to false.
		readonly bool bSendAnonStats = true;

        public MainWindow () : 
				base(WindowType.Toplevel)
		{		
			//initialize localization
			Mono.Unix.Catalog.Init ("Launchpad", "./locale");	

			//Initialize the config files and check values.
			Config.Initialize ();

			//Initialize the GTK UI
			this.Build ();

			//set the window title
			Title = "Launchpad - " + Config.GetGameName ();

			// Configure the WebView for our changelog
			Browser.SetSizeRequest (290, 300);		

			scrolledwindow2.Add (Browser);
			scrolledwindow2.ShowAll ();

			MessageLabel.Text = Mono.Unix.Catalog.GetString ("Idle");

			//First of all, check if we can connect to the FTP server.
			if (!Checks.CanConnectToFTP ())
			{
				MessageDialog dialog = new MessageDialog (
					null, 
					DialogFlags.Modal, 
					MessageType.Warning, 
					ButtonsType.Ok, 
					Mono.Unix.Catalog.GetString ("Failed to connect to the FTP server. Please check your FTP settings."));

				dialog.Run ();
				dialog.Destroy ();
				MessageLabel.Text = Mono.Unix.Catalog.GetString ("Could not connect to server.");
			}
			else
			{
				//if we can connect, proceed with the rest of our checks.
				if (ChecksHandler.IsInitialStartup ())
				{
					MessageDialog shouldInstallHereDialog = new MessageDialog (
						null, 
						DialogFlags.Modal, 
						MessageType.Question, 
						ButtonsType.OkCancel, 
						String.Format (Mono.Unix.Catalog.GetString (
						"This appears to be the first time you're starting the launcher.\n" +
						"Is this the location where you would like to install the game?" +
						"\n\n{0}"), ConfigHandler.GetLocalDir ()
					));

					if (shouldInstallHereDialog.Run () == (int)ResponseType.Ok)
					{
						shouldInstallHereDialog.Destroy ();
						//yes, install here
						Console.WriteLine ("Installing in current directory.");
						ConfigHandler.CreateUpdateCookie ();
					}
					else
					{
						shouldInstallHereDialog.Destroy ();
						//no, don't install here
						Console.WriteLine ("Exiting...");
						Environment.Exit (0);
					}

				} 
				
				if (bSendAnonStats)
				{
					StatsHandler.SendUsageStats ();
				}
				else
				{
					Notification noUsageStatsNotification = new Notification ();

					noUsageStatsNotification.IconName = Stock.DialogWarning;
					noUsageStatsNotification.Urgency = Urgency.Normal;
					noUsageStatsNotification.Summary = Mono.Unix.Catalog.GetString ("Launchpad - Warning");
					noUsageStatsNotification.Body = Mono.Unix.Catalog.GetString ("Anonymous useage stats are not enabled.");

					noUsageStatsNotification.Show ();
				}


				//Start loading the changelog asynchronously
				Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;
				Launcher.LoadChangelog ();

				//if the launcher does not need an update at this point, we can continue checks for the game
				if (!Checks.IsLauncherOutdated ())
				{
					if (Checks.IsManifestOutdated ())
					{					
						Launcher.DownloadManifest ();
					}

					if (!Checks.IsGameInstalled ())
					{
						//if the game is not installed, offer to install it
						Console.WriteLine ("Not installed.");
						SetLauncherMode (ELauncherMode.Install, false);
					}
					else
					{
						//if the game is installed (which it should be at this point), check if it needs to be updated
						if (Checks.IsGameOutdated())
						{
							//if it does, offer to update it
							Console.WriteLine ("Game is outdated or not installed");
							SetLauncherMode (ELauncherMode.Update, false);
						}
						else
						{
							//if not, enable launching the game
							SetLauncherMode (ELauncherMode.Launch, false);
						}
					}
				}
                else
                {
                    //the launcher was outdated.
                    SetLauncherMode (ELauncherMode.Update, false);
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
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Installing...");
					}
					else
					{
						PrimaryButton.Sensitive = true;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Install");
					}	
					break;
				}
				case ELauncherMode.Update:
				{
					if (bInProgress)
					{
						PrimaryButton.Sensitive = false;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Updating...");
					}
					else
					{
						PrimaryButton.Sensitive = true;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Update");
					}					
					break;
				}					
				case ELauncherMode.Repair:
				{
					if (bInProgress)
					{
						PrimaryButton.Sensitive = false;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Repairing...");
					}
					else
					{
						PrimaryButton.Sensitive = true;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Repair");
					}	
					break;
				}					
				case ELauncherMode.Launch:
				{
					if (bInProgress)
					{
						PrimaryButton.Sensitive = false;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Launching...");
					}
					else
					{
						PrimaryButton.Sensitive = true;
						PrimaryButton.Label = Mono.Unix.Catalog.GetString("Launch");
					}	
					break;
				}					
				default:
				{
					throw new ArgumentOutOfRangeException ("newMode", "Invalid mode was passed to SetLauncherMode");
				}
			}
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}

		/// <summary>
		/// Opens the settings editor, which allows the user to change the FTP and game settings.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnSettingsActionActivated (object sender, EventArgs e)
		{
			SettingsDialog LauncherSettings = new SettingsDialog ();
			LauncherSettings.Run ();

			//set the window title, if it changed.
			Title = "Launchpad - " + Config.GetGameName ();
		}		

		/// <summary>
		/// Runs a game repair, no matter what the state the installation is in.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnRepairGameActionActivated(object sender, EventArgs e)
		{			
			SetLauncherMode (ELauncherMode.Repair, false);

			// Simulate a button press from the user.
			OnPrimaryButtonClicked (this, EventArgs.Empty);

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
		protected void OnPrimaryButtonClicked (object sender, EventArgs e)
		{
			switch (Mode)
			{
				case ELauncherMode.Repair:
				{
					//bind events for UI updating					
					Game.ProgressChanged += OnGameDownloadProgressChanged;
					Game.GameRepairFinished += OnRepairFinished;
					Game.GameRepairFailed += OnGameRepairFailed;

				    if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
				    {
					    //Repair the game asynchronously
                        SetLauncherMode(ELauncherMode.Repair, true);
					    Game.RepairGame ();                        
				    }	
				    else
				    {
					    Notification noProvide = new Notification ();
					    noProvide.IconName = Stock.DialogError;
						noProvide.Summary = Mono.Unix.Catalog.GetString ("Launchpad - Platform not provided!");
						noProvide.Body = Mono.Unix.Catalog.GetString ("The server does not provide the game for the selected platform.");
					    noProvide.Show();

						SetLauncherMode (ELauncherMode.Install, false);
				    }
					break;
				}
				case ELauncherMode.Install:
				{
					//bind events for UI updating					
					Game.ProgressChanged += OnGameDownloadProgressChanged;
                    Game.GameDownloadFinished += OnGameDownloadFinished;
					Game.GameDownloadFailed += OnGameDownloadFailed;
						
					//check for a .provides file in the platform directory on the server
					//if there is none, the server does not provide a game for that platform
					if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
					{
						//install the game asynchronously
                        SetLauncherMode(ELauncherMode.Install, true);
						Game.InstallGame ();                        
					}	
					else
					{
						Notification noProvide = new Notification ();
						noProvide.IconName = Stock.DialogError;
						noProvide.Summary = Mono.Unix.Catalog.GetString ("Launchpad - Platform not provided!");
						noProvide.Body = Mono.Unix.Catalog.GetString ("The server does not provide the game for the selected platform.");
						noProvide.Show();

						SetLauncherMode (ELauncherMode.Install, false);
					}
					break;
				}
				case ELauncherMode.Update:
				{
					if (Checks.IsLauncherOutdated())
					{				
                        SetLauncherMode(ELauncherMode.Update, true);
						Launcher.UpdateLauncher ();                        
					}
					else
					{					
						//bind events for UI updating
						Game.GameUpdateFinished += OnGameUpdateFinished;
						Game.ProgressChanged += OnGameDownloadProgressChanged;
						Game.GameUpdateFailed += OnGameUpdateFailed;

						//update the game asynchronously
						if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
						{
							//install the game asynchronously
                            SetLauncherMode(ELauncherMode.Update, true);
							Game.UpdateGame ();                            
						}	
						else
						{
							Notification noProvide = new Notification ();
							noProvide.IconName = Stock.DialogError;
							noProvide.Summary = Mono.Unix.Catalog.GetString ("Launchpad - Platform not provided!");
							noProvide.Body = Mono.Unix.Catalog.GetString ("The server does not provide the game for the selected platform.");
							noProvide.Show();

							SetLauncherMode (ELauncherMode.Install, false);
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
					SetLauncherMode (ELauncherMode.Launch, true);
					Game.LaunchGame ();
					
					break;
				}
				default:
				{
					Console.WriteLine ("No functionality for this mode.");
					break;
				}
			}
		}

        /// <summary>
        /// Updates the web browser with the asynchronously loaded changelog from the server.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The arguments containing the HTML from the server.</param>
        protected void OnChangelogDownloadFinished(object sender, GameDownloadFinishedEventArgs e)
        {
            //Take the resulting HTML string from the changelog download and send it to the changelog browser
            Application.Invoke(delegate
            {
                Browser.LoadHtmlString(e.Result, e.ResultType);
            });
        }

        /// <summary>
        /// Warns the user when the game fails to launch, and offers to attempt a repair.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty event args.</param>
		private void OnGameLaunchFailed(object sender, EventArgs e)
		{
			Notification launchFailed = new Notification ();
			launchFailed.IconName = Stock.DialogError;
			launchFailed.Summary = Mono.Unix.Catalog.GetString ("Launchpad - Failed to launch the game!");
			launchFailed.Body = Mono.Unix.Catalog.GetString ("The game failed to launch. Try repairing the installation.");
			launchFailed.Show();

			SetLauncherMode (ELauncherMode.Repair, false);
		}

        /// <summary>
        /// Provides alternatives when the game fails to download, either through an update or through an installation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the type of failure that occurred.</param>
		private void OnGameDownloadFailed(object sender, GameDownloadFailedEventArgs e)
		{
			ELauncherMode parsedMode;
			if (Enum.TryParse(e.ResultType, out parsedMode))
			{
				switch(parsedMode)
				{
					case ELauncherMode.Install:
					{
						Console.WriteLine (e.Metadata);
						MessageLabel.Text = e.Metadata;
						break;
					}
					case ELauncherMode.Update:
					{
						Console.WriteLine (e.Metadata);
						MessageLabel.Text = e.Metadata;
						break;
					}
					case ELauncherMode.Repair:
					{
						Console.WriteLine (e.Metadata);
						MessageLabel.Text = e.Metadata;
						break;
					}
					default:
					{
						break;
					}
				}

				SetLauncherMode (parsedMode, false);
			}
            else
            {
                //if we can't parse the result for some reason, offer to repair the installation.
                SetLauncherMode(ELauncherMode.Repair, false);
            }
		}

        /// <summary>
        /// Updates the progress bar and progress label during installations, repairs and updates.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the progress values and current filename.</param>
        protected void OnGameDownloadProgressChanged(object sender, FileDownloadProgressChangedEventArgs e)
        {
            Application.Invoke(delegate
            {

				string progressbarText = String.Format(Mono.Unix.Catalog.GetString ("Downloading file {0}: {1} of {2} bytes."),
                                                       System.IO.Path.GetFileNameWithoutExtension(e.FileName),
                                                       e.DownloadedBytes.ToString(),
                                                       e.TotalBytes.ToString());
                progressbar2.Text = progressbarText;
                progressbar2.Fraction = (double)e.DownloadedBytes / (double)e.TotalBytes;

            });
        }

        /// <summary>
        /// Allows the user to launch or repair the game once installation finishes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the result of the download.</param>
        protected void OnGameDownloadFinished(object sender, GameDownloadFinishedEventArgs e)
        {
            if (e != null)
            {				
                if (e.Result == "1") //there was an error
                {
                    MessageLabel.Text = Mono.Unix.Catalog.GetString("Game download failed. Are you missing the manifest?");

                    Notification failedNot = new Notification();
                    failedNot.IconName = Stock.DialogError;
                    failedNot.Summary = Mono.Unix.Catalog.GetString("Launchpad - Error");
                    failedNot.Body = Mono.Unix.Catalog.GetString("Game download failed. Are you missing the manifest?");

                    failedNot.Show();

					ELauncherMode parsedMode;
                    if (Enum.TryParse(e.ResultType, out parsedMode))
                    {
                        SetLauncherMode(parsedMode, false);
                    }
                    else
                    {
                        SetLauncherMode(ELauncherMode.Repair, false);
                    }                                        
                }
                else //the game has finished downloading, and we should be OK to launch
                {
                    MessageLabel.Text = Mono.Unix.Catalog.GetString("Idle");
                    progressbar2.Text = "";

                    Notification completedNot = new Notification();
                    completedNot.IconName = Stock.Info;
                    completedNot.Summary = Mono.Unix.Catalog.GetString("Launchpad - Info");
                    completedNot.Body = Mono.Unix.Catalog.GetString("Game download finished. Play away!");

                    completedNot.Show();

                    SetLauncherMode(ELauncherMode.Launch, false);
                }
            }           
        }

		/// <summary>
		/// Passes the repair failed event to a generic handler
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the type of failure that occured</param>
		private void OnGameRepairFailed(object sender, GameRepairFailedEventArgs e)
		{
			GameDownloadFailedEventArgs args = new GameDownloadFailedEventArgs ();
			args.Metadata = e.Metadata;
			args.Result = e.Result;
			args.ResultType = "Repair";

			OnGameDownloadFailed (sender, args);	
		}

		/// <summary>
		/// Passes the update finished event to a generic handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the result of the download.</param>
		protected void OnGameUpdateFinished(object sender, GameUpdateFinishedEventArgs e)
		{
			GameDownloadFinishedEventArgs args = new GameDownloadFinishedEventArgs ();
			args.Metadata = e.Metadata;
			args.Result = e.Result;
			args.ResultType = e.ResultType;

			OnGameDownloadFinished (sender, args);
		}

		/// <summary>
		/// Passes the update failed event to a generic handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">Contains the type of failure that occurred.</param>
		protected void OnGameUpdateFailed(object sender, GameUpdateFailedEventArgs e)
		{
			GameDownloadFailedEventArgs args = new GameDownloadFailedEventArgs ();
			args.Metadata = e.Metadata;
			args.Result = e.Result;
			args.ResultType = "Update";

			OnGameDownloadFailed (sender, args);
		}

        /// <summary>
        /// Alerts the user that a repair action has finished.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty arguments.</param>
        private void OnRepairFinished(object sender, EventArgs e)
        {
            Notification repairComplete = new Notification();
            repairComplete.IconName = Stock.Info;
			repairComplete.Summary = Mono.Unix.Catalog.GetString ("Launchpad - Game repair finished");
			repairComplete.Body = Mono.Unix.Catalog.GetString ("Launchpad has finished repairing the game installation. Play away!");
            repairComplete.Show();

            progressbar2.Text = "";

			SetLauncherMode (ELauncherMode.Launch, false);
        }

		private void OnGameExited(object sender, GameExitEventArgs e)
		{
			if (e.ExitCode != 0)
			{
				MessageDialog crashDialog = new MessageDialog (
					this, 
					DialogFlags.Modal, 
					MessageType.Question, 
					ButtonsType.YesNo, 
					String.Format (Mono.Unix.Catalog.GetString (
						"Whoops! The game appears to have crashed.\n" +
						"Would you like the launcher to verify the installation?"
					)));

				if (crashDialog.Run() == (int)ResponseType.Yes)
				{
					SetLauncherMode (ELauncherMode.Repair, false);
				}
				else
				{
					SetLauncherMode (ELauncherMode.Launch, false);
				}
			}
			else
			{
				SetLauncherMode (ELauncherMode.Launch, false);
			}
		}
	}
}

