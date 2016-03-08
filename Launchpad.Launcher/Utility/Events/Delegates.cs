//
//  Delegates.cs
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

namespace Launchpad.Launcher.Utility.Events
{
	//FTP delegates
	public delegate void FileProgressChangedEventHandler(object sender,FileDownloadProgressChangedEventArgs e);
	public delegate void FileDownloadFinishedEventHandler(object sender,FileDownloadFinishedEventArgs e);

	//Game delegates
	//Generic
	public delegate void GameProgressChangedEventHandler(object sender,FileDownloadProgressChangedEventArgs e);

	// Success
	public delegate void GameDownloadFinishedEventHandler(object sender,GameDownloadFinishedEventArgs e);
	public delegate void GameUpdateFinishedEventHandler(object sender,GameUpdateFinishedEventArgs e);
	public delegate void GameRepairFinishedEventHandler(object sender,GameRepairFinishedEventArgs e);

	// Failure
	public delegate void GameDownloadFailedEventHander(object sender,GameDownloadFailedEventArgs e);
	public delegate void GameUpdateFailedEventHandler(object sender,GameUpdateFailedEventArgs e);
	public delegate void GameRepairFailedEventHandler(object sender,GameRepairFailedEventArgs e);
	public delegate void GameLaunchFailedEventHandler(object sender,GameLaunchFailedEventArgs e);

	// Game deletages
	public delegate void GameExitEventHandler(object sender,GameExitEventArgs e);

	//Launcher delegates
	public delegate void ChangelogProgressChangedEventHandler(object sender,FileDownloadProgressChangedEventArgs e);
	public delegate void ChangelogDownloadFinishedEventHandler(object sender,GameDownloadFinishedEventArgs e);
}

