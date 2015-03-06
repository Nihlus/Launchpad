using System;
using System.IO;
using Gtk;
using WebKit;

namespace Launchpad_Launcher
{
	public partial class MainWindow : Gtk.Window
	{
		//Can we connect to the specified FTP server?
		//assume we can connect until we cannot.
		bool bCanConnectToFTP = true;
		bool bLauncherNeedsUpdate = false;
		//set up handlers
		ChecksHandler Checks = new ChecksHandler ();
		//Config handler - allows us to read values from the configuration file.
		ConfigHandler Config = new ConfigHandler ();
		//Launcher handler - allows async changelog loading and launcher updating
		LauncherHandler Launcher = new LauncherHandler ();
		//Game handler - allows installing, updating and launching the game
		GameHandler Game = new GameHandler();

		// Add a WebView for our changelog
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

				bCanConnectToFTP = false;
			}
			else
			{
				//if we can connect, proceeed with the rest of our checks.
				//check if this is the first time we're starting the launcher.
				if (Checks.IsInitialStartup ())
				{
					MessageDialog dialog = new MessageDialog (
						null, 
						DialogFlags.Modal, 
						MessageType.Question, 
						ButtonsType.OkCancel, 
						String.Format (
						"This appears to be the first time you're starting the launcher." +
						"Is this the location where you would like to install the game?" +
						"\n\n{0}", Config.GetLocalDir ()
					));

					if (dialog.Run () == (int)Gtk.ResponseType.Ok)
					{
						dialog.Destroy ();
						//yes, install here
						Console.WriteLine ("Installing in current directory.");
						Config.CreateUpdateCookie ();
					}
					else
					{
						dialog.Destroy ();
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
					StatsHandler stats = new StatsHandler ();
					stats.SendUseageStats ();
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
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}

		protected void OnSettingsActionActivated (object sender, EventArgs e)
		{
			SettingsDialog Settings = new SettingsDialog ();
			Settings.Run ();
		}

		protected void OnGameDownloadProgressChanged (object sender, ProgressEventArgs e)
		{
			Gtk.Application.Invoke (delegate
			{

				string progressbarText = String.Format ("Downloading file {0}: {1} of {2} bytes.", 
				                                       System.IO.Path.GetFileNameWithoutExtension (e.Filename), 
				                                       e.DownloadedBytes.ToString (), 
				                                       e.TotalBytes.ToString ());
				progressbar2.Text = progressbarText;
				progressbar2.Fraction = (double)e.DownloadedBytes / (double)e.TotalBytes;

			});
		}

		protected void OnGameDownloadFinished (object sender, DownloadFinishedEventArgs e)
		{
			if (e.Value == "1") //there was an error
			{
				MessageLabel.Text = "Game download failed. Are you missing the manifest?";

				PrimaryButton.Label = e.URL; //URL is used here to set the desired retry action
				PrimaryButton.Sensitive = true;
			}
			else //the game has finished downloading, and we should be OK to launch
			{
				MessageLabel.Text = "Idle";
				progressbar2.Text = "";

				PrimaryButton.Label = "Launch";
				PrimaryButton.Sensitive = true;
			}
		}

		protected void OnChangelogDownloadFinished (object sender, DownloadFinishedEventArgs e)
		{
			Gtk.Application.Invoke (delegate
			{
				Browser.LoadHtmlString (e.Value, e.URL);

			});
		}

		protected void OnPrimaryButtonClicked (object sender, EventArgs e)
		{
			string Mode = PrimaryButton.Label;
			switch (Mode)
			{
				case "Repair":
				{
					Console.WriteLine ("Repairing installation...");
					//bind events for UI updating
					Game.DownloadFinished += OnGameDownloadFinished;
					Game.ProgressChanged += OnGameDownloadProgressChanged;

					break;
				}
				case "Install":
				{
					Console.WriteLine ("Installing game...");
					PrimaryButton.Label = "Installing...";
					PrimaryButton.Sensitive = false;
					//bind events for UI updating
					Game.DownloadFinished += OnGameDownloadFinished;
					Game.ProgressChanged += OnGameDownloadProgressChanged;
					
					//install the game asynchronously
					Game.InstallGame ();
					break;
				}
				case "Update":
				{
					if (bLauncherNeedsUpdate)
					{
						Console.WriteLine ("Updating launcher...");
					PrimaryButton.Label = "Updating...";
					PrimaryButton.Sensitive = false;
					}
					else
					{
						Console.WriteLine ("Updating game...");
						//bind events for UI updating
						Game.DownloadFinished += OnGameDownloadFinished;
						Game.ProgressChanged += OnGameDownloadProgressChanged;

						//update the game asynchronously
						Game.UpdateGame ();
					}					
					break;
				}
				case "Launch":
				{
					Console.WriteLine ("Launching game...");
					break;
				}
				default:
				{
					Console.WriteLine ("No functionality for this mode.");
					break;
				}
			}
		}
	}
}

