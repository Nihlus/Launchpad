//
//  ILaunchpadConfiguration.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
//

using System;
using Config.Net;
using Launchpad.Common.Enums;

namespace Launchpad.Launcher.Configuration
{
	/// <summary>
	/// Configuration file interface.
	/// </summary>
	public interface ILaunchpadConfiguration
	{
		// Launcher section
		// ...

		/// <summary>
		/// Gets or sets the address where the changelog is hosted.
		/// </summary>
		[Option(Alias = "Launcher.ChangelogAddress", DefaultValue = "http://sharkman.asuscomm.com/launchpad/changelog/changelog.html")]
		Uri ChangelogAddress { get; set; }

		/// <summary>
		/// Gets or sets the system target of the launcher.
		/// </summary>
		[Option(Alias = "Launcher.SystemTarget", DefaultValue = "Linux")]
		ESystemTarget SystemTarget { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to use the official update source.
		/// </summary>
		[Option(Alias = "Launcher.UseOfficialUpdates", DefaultValue = true)]
		bool UseOfficialUpdates { get; set; }

		// Game section
		// ...

		/// <summary>
		/// Gets or sets the name of the game.
		/// </summary>
		[Option(Alias = "Game.Name", DefaultValue = "LaunchpadExample")]
		string GameName { get; set; }

		/// <summary>
		/// Gets or sets the path to the game's executable, relative to the launcher.
		/// </summary>
		[Option(Alias = "Game.ExecutablePath", DefaultValue = "LaunchpadExample/Binaries/Linux/LaunchpadExample")]
		string ExecutablePath { get; set; }

		// Remote section
		// ...

		/// <summary>
		/// Gets or sets the address of the remote server.
		/// </summary>
		[Option(Alias = "Remote.Address", DefaultValue = "ftp://sharkman.asuscomm.com")]
		Uri RemoteAddress { get; set; }

		/// <summary>
		/// Gets or sets the username to use when authenticating with the remote server.
		/// </summary>
		[Option(Alias = "Remote.Username", DefaultValue = "anonymous")]
		string RemoteUsername { get; set; }

		/// <summary>
		/// Gets or sets the password to use when authenticating with the remote server.
		/// </summary>
		[Option(Alias = "Remote.Password", DefaultValue = "anonymous")]
		string RemotePassword { get; set; }

		/// <summary>
		/// Gets or sets the number of times to retry file downloads.
		/// </summary>
		[Option(Alias = "Remote.FileDownloadRetries", DefaultValue = 2)]
		int RemoteFileDownloadRetries { get; set; }

		/// <summary>
		/// Gets or sets the buffer size to use when downloading files.
		/// </summary>
		[Option(Alias = "Remote.FileDownloadBufferSize", DefaultValue = 8192)]
		int RemoteFileDownloadBufferSize { get; set; }
	}
}
