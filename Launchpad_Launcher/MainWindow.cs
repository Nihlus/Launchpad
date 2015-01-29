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

			Console.WriteLine (Config.GetLocalDir ());
			Console.WriteLine (Config.GetChangelogURL ());
			Console.WriteLine (Config.GetGameExecutable ());
			Console.WriteLine (Config.GetTempDir ());
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}
	}
}

