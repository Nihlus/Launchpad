using System;
using Gtk;

namespace Launchpad_Launcher
{
	public partial class MainWindow : Gtk.Window
	{
		//Can we connect to the specified FTP server?
		//assume we can connect until we cannot.
		bool bCanConnectToFTP = true;

		//Could we download the manifest from the server?
		bool bManifestDownloadFailed = false;

		//Did the launcher fail to check its own version against the server?
		bool bLauncherVersionCheckFailed = false;
		//Does the launcher need to update?
		bool bLauncherNeedsUpdate = false;

		//Does the game need to update?
		bool bGameNeedsUpdate = false;
		//Is the game installed?
		bool bGameIsInstalled = false;

		//Are we currently installing a game?
		bool bIsInstallingGame = false;
		//Did we attempt to install the game?
		bool bDidAttemptInstall = false;
		//Did the install complete successfully?
		bool bInstallCompleted = false;

		bool bIsUpdatingGame = false;
		bool bUpdateCompleted = false;

		bool bShouldBeginAutoInstall = false;

		bool bIsBackgroundImageLoadedFromServer = false;
		bool bIsChangelogLoadedFromServer = false;


		//set up handlers
		ChecksHandler Checks = new ChecksHandler();
		//MD5 hashing handler - allows us to create and match MD5 hashes for files.
		MD5Handler md5 = new MD5Handler();
		//Config handler - allows us to read values from the configuration file.
		ConfigHandler Config = new ConfigHandler();
		//FTP handler - allows us to read and download files from a remote server.
		FTPHandler FTP = new FTPHandler();

		public MainWindow () : 
				base(Gtk.WindowType.Toplevel)
		{

			this.Build ();

			if (!Checks.CanConnectToFTP ())
			{
				MessageDialog dialog = new MessageDialog (null, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok, "Failed to connect to the FTP server. Please check your FTP settings.");
				dialog.Run ();
				dialog.Destroy ();
				bCanConnectToFTP = false;
			} 
			else
			{
				//if we can connect, proceeed with the rest of our checks.

				//this section sends some anonymous useage stats back home. If you don't want to do this for your game, simply change this boolean to false.
				bool bSendAnonStats = false;
				if (bSendAnonStats)
				{
					Console.WriteLine ("Sending anonymous useage stats to hambase 1 :) Thanks!");
					StatsHandler stats = new StatsHandler ();
					stats.SendUseageStats(Config.GetGUID(), Config.GetLauncherVersion(), Config.GetGameName(), Config.GetDoOfficialUpdates());
				}

				//check if this is the first time we're starting the launcher.
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
	}
}

