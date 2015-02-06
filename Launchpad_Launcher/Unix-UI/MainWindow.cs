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


		//set up handlers
		ChecksHandler Checks = new ChecksHandler();
		//Config handler - allows us to read values from the configuration file.
		ConfigHandler Config = new ConfigHandler();
		//Launcher handler - allows async changelog loading and launcher updating
		LauncherHandler Launcher = new LauncherHandler();

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
					} else
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
					Console.WriteLine ("Outdated!");
					//Download the new launcher
				}

				//Start loading the changelog asynchronously
				Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;
				Launcher.LoadChangelog ();




				//if the game is not installed, offer to install it

				//if the game is installed (which it should be at this point), check if it needs to be updated

				//if it does, offer to update it

				//if not, enable launching the game
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

		protected void OnGameDownloadProgressChanged(object sender, ProgressEventArgs e)
		{
			Gtk.Application.Invoke(delegate {

				string progressbarText = String.Format("Downloading file {0}: {1} of {2} bytes.", 
				                                       System.IO.Path.GetFileNameWithoutExtension(e.Filename), 
				                                       e.DownloadedBytes.ToString(), 
				                                       e.TotalBytes.ToString());
				progressbar2.Text = progressbarText;
				progressbar2.Fraction = (double)e.DownloadedBytes / (double)e.TotalBytes;

			});
		}

		protected void OnGameDownloadFinished(object sender, DownloadFinishedEventArgs e)
		{

		}

		protected void OnChangelogDownloadFinished (object sender, DownloadFinishedEventArgs e)
		{
			Gtk.Application.Invoke (delegate {
				Browser.LoadHtmlString(e.Value, e.URL);

			});
		}
	}
}

