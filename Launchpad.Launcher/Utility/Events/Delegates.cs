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
using System;

namespace Launchpad.Launcher.Utility.Events
{
	//TODO: Move these to the FTP protocol module. Can most likely be merged as well.
	public delegate void FileDownloadProgressChangedEventHandler(object sender,FileDownloadProgressChangedEventArgs e);
	public delegate void GameDownloadProgressChangedEventHandler(object sender,FileDownloadProgressChangedEventArgs e);

	public delegate void LauncherInstallFinishedEventHandler(object sender,EventArgs e);
	public delegate void GameInstallFinishedEventHandler(object sender,EventArgs e);

	public delegate void LauncherDownloadFailedEventHandler(object sender,EventArgs e);
	public delegate void GameDownloadFailedEventHander(object sender,GameDownloadFailedEventArgs e);

	public delegate void GameLaunchFailedEventHandler(object sender,EventArgs e);

	public delegate void GameExitEventHandler(object sender,GameExitEventArgs e);

	public delegate void ChangelogDownloadFinishedEventHandler(object sender,GameDownloadFinishedEventArgs e);
}

