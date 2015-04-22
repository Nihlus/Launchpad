using System;
using System.IO;
using Gtk;
using WebKit;
using Notifications;

namespace Launchpad
{
    public partial class MainWindow : Gtk.Window
	{
		/// <summary>
		/// Does the launcher need an update?
		/// </summary>
		bool bLauncherNeedsUpdate = false;

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

        public MainWindow () : 
				base(Gtk.WindowType.Toplevel)
		{		
			this.Build ();

			//Initialize the config files and check values.
			Config.Initialize ();

			// Configure the WebView for our changelog
			Browser.SetSizeRequest (290, 300);

			scrolledwindow2.Add (Browser);
			scrolledwindow2.ShowAll ();

			MessageLabel.Text = "Idle";
			//First of all, check if we can connect to the FTP server.
			if (!Checks.CanConnectToFTP ())
			{
				MessageDialog dialog = new MessageDialog (
					null, 
					DialogFlags.Modal, 
					MessageType.Warning, 
					ButtonsType.Ok, 
					"Failed to connect to the FTP server. Please check your FTP settings.");

				dialog.Run ();
				dialog.Destroy ();
				MessageLabel.Text = "No FTP connection.";
			}
			else
			{
				//if we can connect, proceeed with the rest of our checks.
				if (ChecksHandler.IsInitialStartup ())
				{
					MessageDialog shouldInstallHereDialog = new MessageDialog (
						null, 
						DialogFlags.Modal, 
						MessageType.Question, 
						ButtonsType.OkCancel, 
						String.Format (
						"This appears to be the first time you're starting the launcher." +
						"Is this the location where you would like to install the game?" +
						"\n\n{0}", ConfigHandler.GetLocalDir ()
					));

					if (shouldInstallHereDialog.Run () == (int)Gtk.ResponseType.Ok)
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
						Gtk.Application.Quit ();
					}

				} 

				//this section sends some anonymous useage stats back home. 
				//If you don't want to do this for your game, simply change this boolean to false.
				bool bSendAnonStats = false;
				if (bSendAnonStats)
				{
					Console.WriteLine ("Sending anonymous useage stats to hambase 1 :) Thanks!");
					StatsHandler.SendUsageStats ();
				}
				else
				{
					Notification noStatsNot = new Notification ();

					noStatsNot.IconName = Stock.DialogWarning;
					noStatsNot.Urgency = Urgency.Normal;
					noStatsNot.Summary = "Launchpad - Warning";
					noStatsNot.Body = "Anonymous useage stats are not enabled.";

					noStatsNot.Show ();
				}

				//check if the launcher is outdated
				if (Checks.IsLauncherOutdated ())
				{
					Console.WriteLine ("Launcher outdated.");
					PrimaryButton.Sensitive = true;
					PrimaryButton.Label = "Update";
					bLauncherNeedsUpdate = true;
					//Download the new launcher
				}

				//Start loading the changelog asynchronously
				Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;
				Launcher.LoadChangelog ();

				//if the launcher does not need an update at this point, we can continue checks for the game
				if (!bLauncherNeedsUpdate)
				{
					if (Checks.IsManifestOutdated ())
					{					
						Launcher.DownloadManifest ();
					}

					if (!Checks.IsGameInstalled ())
					{
						//if the game is not installed, offer to install it
						Console.WriteLine ("Not installed.");
						PrimaryButton.Sensitive = true;
						PrimaryButton.Label = "Install";
					}
					else
					{
						//if the game is installed (which it should be at this point), check if it needs to be updated
						if (Checks.IsGameOutdated())
						{
							//if it does, offer to update it
							Console.WriteLine ("Game is outdated or not installed");
							PrimaryButton.Sensitive = true;
							PrimaryButton.Label = "Update";
						}
						else
						{
							//if not, enable launching the game
							PrimaryButton.Sensitive = true;
							PrimaryButton.Label = "Launch";
						}
					}
				}
			}
			Console.WriteLine ("REMEMBER: TURN ON ANON STATS BEFORE RELEASE");
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Gtk.Application.Quit ();
			a.RetVal = true;
		}

		/// <summary>
		/// Opens the settings editor, which allows the user to change the FTP and game settings.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnSettingsActionActivated (object sender, EventArgs e)
		{
			SettingsDialog Settings = new SettingsDialog ();
			Settings.Run ();
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
			string Mode = PrimaryButton.Label;
			switch (Mode)
			{
				case "Repair":
				{
					Console.WriteLine ("Repairing installation...");
					//bind events for UI updating					
					Game.ProgressChanged += OnGameDownloadProgressChanged;
					Game.GameRepairFinished += OnRepairFinished;
					Game.GameDownloadFailed += OnGameDownloadFailed;

				    if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
				    {
					    //install the game asynchronously
					    Game.RepairGame ();
				    }	
				    else
				    {
					    Notification noProvide = new Notification ();
					    noProvide.IconName = Stock.DialogError;
					    noProvide.Summary = "Launchpad - Platform not provided!";
					    noProvide.Body = "The server does not provide the game for the selected platform.";
					    noProvide.Show();

					    PrimaryButton.Label = "Install";
					    PrimaryButton.Sensitive = true;
				    }

					break;
				}
				case "Install":
				{
					Console.WriteLine ("Installing game...");
					PrimaryButton.Label = "Installing...";
					PrimaryButton.Sensitive = false;
					//bind events for UI updating
					Game.GameDownloadFinished += OnGameDownloadFinished;
					Game.ProgressChanged += OnGameDownloadProgressChanged;
					Game.GameDownloadFailed += OnGameDownloadFailed;
						
					//check for a .provides file in the platform directory on the server
					//if there is none, the server does not provide a game for that platform
					if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
					{
						//install the game asynchronously
						Game.InstallGame ();
					}	
					else
					{
						Notification noProvide = new Notification ();
						noProvide.IconName = Stock.DialogError;
						noProvide.Summary = "Launchpad - Platform not provided!";
						noProvide.Body = "The server does not provide the game for the selected platform.";
						noProvide.Show();

						PrimaryButton.Label = "Install";
						PrimaryButton.Sensitive = true;
					}
					break;
				}
				case "Update":
				{
					if (bLauncherNeedsUpdate)
					{
						Console.WriteLine ("Updating launcher...");
						PrimaryButton.Label = "Updating...";
						PrimaryButton.Sensitive = false;

						Launcher.UpdateLauncher ();
					}
					else
					{
						Console.WriteLine ("Updating game...");
						PrimaryButton.Label = "Updating...";
						PrimaryButton.Sensitive = false;

						//bind events for UI updating
						Game.GameDownloadFinished += OnGameDownloadFinished;
						Game.ProgressChanged += OnGameDownloadProgressChanged;
						Game.GameDownloadFailed += OnGameDownloadFailed;

						//update the game asynchronously
						if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
						{
							//install the game asynchronously
							Game.UpdateGame ();
						}	
						else
						{
							Notification noProvide = new Notification ();
							noProvide.IconName = Stock.DialogError;
							noProvide.Summary = "Launchpad - Platform not provided!";
							noProvide.Body = "The server does not provide the game for the selected platform.";
							noProvide.Show();

							PrimaryButton.Label = "Install";
							PrimaryButton.Sensitive = true;
						}								
					}												
					break;
				}
				case "Launch":
				{
					Console.WriteLine ("Launching game...");
					Game.GameLaunchFailed += OnGameLaunchFailed;
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
            //Take the resulting HTML string from the changelog download and send it to the Webkit browser
            Gtk.Application.Invoke(delegate
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
			launchFailed.Summary = "Launchpad - Failed to launch the game!";
			launchFailed.Body = "The game failed to launch. Try repairing the installation.";
			launchFailed.Show();

			PrimaryButton.Label = "Repair";	
			PrimaryButton.Sensitive = true;
		}

        /// <summary>
        /// Provides alternatives when the game fails to download, either through an update or through an installation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the type of failure that occurred.</param>
		private void OnGameDownloadFailed(object sender, GameDownloadFailedEventArgs e)
		{
			switch(e.ResultType)
			{
				case "Install":
				{
					Console.WriteLine (e.Metadata);
					MessageLabel.Text = e.Metadata;
					break;
				}
				case "Update":
				{
					Console.WriteLine (e.Metadata);
					MessageLabel.Text = e.Metadata;
					break;
				}
				case "Repair":
				{
					Console.WriteLine (e.Metadata);
					MessageLabel.Text = e.Metadata;
					break;
				}
			}

			PrimaryButton.Label = e.ResultType;
			PrimaryButton.Sensitive = true;
		}

        /// <summary>
        /// Updates the progress bar and progress label during installations, repairs and updates.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the progress values and current filename.</param>
        protected void OnGameDownloadProgressChanged(object sender, FileDownloadProgressChangedEventArgs e)
        {
            Gtk.Application.Invoke(delegate
            {

                string progressbarText = String.Format("Downloading file {0}: {1} of {2} bytes.",
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
                    MessageLabel.Text = "Game download failed. Are you missing the manifest?";

                    Notification failedNot = new Notification();
                    failedNot.IconName = Stock.DialogError;
                    failedNot.Summary = "Launchpad - Error";
                    failedNot.Body = "The game failed to download. Are you missing the manifest?";

                    failedNot.Show();

                    PrimaryButton.Label = e.ResultType; //URL is used here to set the desired retry action
                    PrimaryButton.Sensitive = true;
                }
                else //the game has finished downloading, and we should be OK to launch
                {
                    MessageLabel.Text = "Idle";
                    progressbar2.Text = "";

                    Notification completedNot = new Notification();
                    completedNot.IconName = Stock.Info;
                    completedNot.Summary = "Launchpad - Info";
                    completedNot.Body = "Game download finished. Play away!";

                    completedNot.Show();

                    PrimaryButton.Label = "Launch";
                    PrimaryButton.Sensitive = true;
                }
            }           
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
            repairComplete.Summary = "Launchpad - Game repair finished";
            repairComplete.Body = "Launchpad has finished repairing the game installation. Play away!";
            repairComplete.Show();

            progressbar2.Text = "";

            PrimaryButton.Label = "Launch";
            PrimaryButton.Sensitive = true;
        }
	}
}

