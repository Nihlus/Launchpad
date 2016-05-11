using Launchpad.Launcher.Events.Arguments;

namespace Launchpad.Launcher.Events.Delegates
{
	//FTP delegates
	public delegate void FileProgressChangedEventHandler(object sender, FileDownloadProgressChangedEventArgs e);	
	public delegate void FileDownloadFinishedEventHandler (object sender, FileDownloadFinishedEventArgs e);

	//Game delegates
	//Generic
	public delegate void GameProgressChangedEventHandler(object sender, FileDownloadProgressChangedEventArgs e);

	// Success
	public delegate void GameDownloadFinishedEventHandler (object sender, GameDownloadFinishedEventArgs e);
	public delegate void GameUpdateFinishedEventHandler (object sender, GameUpdateFinishedEventArgs e);
	public delegate void GameRepairFinishedEventHandler (object sender, GameRepairFinishedEventArgs e);

	// Failure
	public delegate void GameDownloadFailedEventHander (object sender, GameDownloadFailedEventArgs e);
	public delegate void GameUpdateFailedEventHandler (object sender, GameUpdateFailedEventArgs e);
	public delegate void GameRepairFailedEventHandler (object sender, GameRepairFailedEventArgs e);
	public delegate void GameLaunchFailedEventHandler (object sender, GameLaunchFailedEventArgs e);

	// Game deletages
	public delegate void GameExitEventHandler (object sender, GameExitEventArgs e);

	//Launcher delegates
	public delegate void ChangelogProgressChangedEventHandler(object sender, FileDownloadProgressChangedEventArgs e);
	public delegate void ChangelogDownloadFinishedEventHandler (object sender, GameDownloadFinishedEventArgs e);
}

