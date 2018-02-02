//
//  ChecksHandler.cs
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

using System.IO;

using Launchpad.Common.Enums;
using Launchpad.Launcher.Handlers.Protocols;

using Launchpad.Launcher.Utility;
using NLog;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// This class handles all the launcher's checks, returning bools for each function.
	/// </summary>
	internal sealed class ChecksHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		private readonly PatchProtocolHandler Patch;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChecksHandler"/> class.
		/// </summary>
		public ChecksHandler()
		{
			this.Patch = PatchProtocolProvider.GetHandler();
		}

		/// <summary>
		/// Determines whether this instance can connect to a patching service.
		/// </summary>
		/// <returns><c>true</c> if this instance can connect to a patching service; otherwise, <c>false</c>.</returns>
		public bool CanPatch()
		{
			return this.Patch != null && this.Patch.CanPatch();
		}

		/// <summary>
		/// Determines whether this is the first time the launcher starts.
		/// </summary>
		/// <returns><c>true</c> if this is the first time; otherwise, <c>false</c>.</returns>
		public static bool IsInitialStartup()
		{
			// We use an empty file to determine if this is the first launch or not
			return !File.Exists(DirectoryHelpers.GetLauncherTagfilePath());
		}

		/// <summary>
		/// Determines whether the game is installed.
		/// </summary>
		/// <returns><c>true</c> if the game is installed; otherwise, <c>false</c>.</returns>
		public bool IsGameInstalled()
		{
			// Criteria for considering the game 'installed'
			var hasGameDirectory = Directory.Exists(DirectoryHelpers.GetLocalGameDirectory());
			var hasInstallCookie = File.Exists(DirectoryHelpers.GetGameTagfilePath());
			var hasGameVersionFile = File.Exists(DirectoryHelpers.GetLocalGameVersionPath());

			if (!hasGameVersionFile && hasGameDirectory)
			{
				Log.Warn
				(
					"No GameVersion.txt file was found in the installation directory.\n" +
					"This may be due to a download error, or the developer may not have included one.\n" +
					"Without it, the game cannot be considered fully installed.\n" +
					"If you are the developer of this game, add one to your game files with your desired version in it."
				);
			}

			// If any of these criteria are false, the game is not considered fully installed.
			return hasGameDirectory && hasInstallCookie && IsInstallCookieEmpty() && hasGameVersionFile;
		}

		/// <summary>
		/// Determines whether the game is outdated.
		/// </summary>
		/// <returns><c>true</c> if the game is outdated; otherwise, <c>false</c>.</returns>
		public bool IsGameOutdated()
		{
			return this.Patch.IsModuleOutdated(EModule.Game);
		}

		/// <summary>
		/// Determines whether the launcher is outdated.
		/// </summary>
		/// <returns><c>true</c> if the launcher is outdated; otherwise, <c>false</c>.</returns>
		public bool IsLauncherOutdated()
		{
			return this.Patch.IsModuleOutdated(EModule.Launcher);
		}

		/// <summary>
		/// Determines whether the install cookie is empty
		/// </summary>
		/// <returns><c>true</c> if the install cookie is empty, otherwise, <c>false</c>.</returns>
		private static bool IsInstallCookieEmpty()
		{
			// Is there an .install file in the directory?
			var hasInstallCookie = File.Exists(DirectoryHelpers.GetGameTagfilePath());
			var isInstallCookieEmpty = false;

			if (hasInstallCookie)
			{
				isInstallCookieEmpty = string.IsNullOrEmpty(File.ReadAllText(DirectoryHelpers.GetGameTagfilePath()));
			}

			return isInstallCookieEmpty;
		}

		/// <summary>
		/// Checks whether or not the server provides binaries and patches for the specified platform.
		/// </summary>
		/// <returns><c>true</c>, if the server does provide files for the platform, <c>false</c> otherwise.</returns>
		/// <param name="platform">platform.</param>
		public bool IsPlatformAvailable(ESystemTarget platform)
		{
			return this.Patch.IsPlatformAvailable(platform);
		}
	}
}
