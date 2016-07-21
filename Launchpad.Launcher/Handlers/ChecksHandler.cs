//
//  ChecksHandler.cs
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
using System.IO;
using log4net;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Handlers.Protocols;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// This class handles all the launcher's checks, returning bools for each function.
	/// Since this class is meant to be used in both the Forms UI and the GTK UI,
	/// there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class ChecksHandler
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		private readonly ConfigHandler Configuration = ConfigHandler.Instance;

		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(ChecksHandler));

		/// <summary>
		/// Determines whether this instance can connect to a patching service.
		/// </summary>
		/// <returns><c>true</c> if this instance can connect to a patching service; otherwise, <c>false</c>.</returns>
		public bool CanPatch()
		{
			PatchProtocolHandler patchService = Configuration.GetPatchProtocol();

			return patchService != null && patchService.CanPatch();
		}

		/// <summary>
		/// Determines whether this is the first time the launcher starts.
		/// </summary>
		/// <returns><c>true</c> if this is the first time; otherwise, <c>false</c>.</returns>
		public static bool IsInitialStartup()
		{
			// We use an empty file to determine if this is the first launch or not
			return !File.Exists(ConfigHandler.GetUpdateCookiePath());
		}

		/// <summary>
		/// Determines whether this instance is running on Unix.
		/// </summary>
		/// <returns><c>true</c> if this instance is running on unix; otherwise, <c>false</c>.</returns>
		public static bool IsRunningOnUnix()
		{
			int platform = (int)Environment.OSVersion.Platform;
			if ((platform == 4) || (platform == 6) || (platform == 128))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Determines whether the game is installed.
		/// </summary>
		/// <returns><c>true</c> if the game is installed; otherwise, <c>false</c>.</returns>
		public bool IsGameInstalled()
		{
			// Criteria for considering the game 'installed'
			// Does the game directory exist?
			bool bHasGameDirectory = Directory.Exists(Configuration.GetGamePath());

			// Is there an .install file in the directory?
			bool bHasInstallationCookie = File.Exists(ConfigHandler.GetInstallCookiePath());

			// Is there a version file?
			bool bHasGameVersion = File.Exists(Configuration.GetGameVersionPath());

			if (!bHasGameVersion && bHasGameDirectory)
			{
				Log.Warn("No GameVersion.txt file was found in the installation directory.\n" +
				         "This may be due to a download error, or the develop may not have included one.\n" +
				         "Without it, the game cannot be considered fully installed.\n" +
				         "If you are the developer of this game, add one to your game files with your desired version in it.");
			}

			// If any of these criteria are false, the game is not considered fully installed.
			return bHasGameDirectory && bHasInstallationCookie && IsInstallCookieEmpty() && bHasGameVersion;
		}

		/// <summary>
		/// Determines whether the game is outdated.
		/// </summary>
		/// <returns><c>true</c> if the game is outdated; otherwise, <c>false</c>.</returns>
		public bool IsGameOutdated()
		{
			PatchProtocolHandler patchService = Configuration.GetPatchProtocol();
			return patchService.IsModuleOutdated(EModule.Game);
		}

		/// <summary>
		/// Determines whether the launcher is outdated.
		/// </summary>
		/// <returns><c>true</c> if the launcher is outdated; otherwise, <c>false</c>.</returns>
		public bool IsLauncherOutdated()
		{
			PatchProtocolHandler patchService = Configuration.GetPatchProtocol();
			return patchService.IsModuleOutdated(EModule.Launcher);
		}

		/// <summary>
		/// Determines whether the install cookie is empty
		/// </summary>
		/// <returns><c>true</c> if the install cookie is empty, otherwise, <c>false</c>.</returns>
		private static bool IsInstallCookieEmpty()
		{
			//Is there an .install file in the directory?
			bool bHasInstallationCookie = File.Exists(ConfigHandler.GetInstallCookiePath());

			//Is the .install file empty? Assume false.
			bool bIsInstallCookieEmpty = false;

			if (bHasInstallationCookie)
			{
				bIsInstallCookieEmpty = string.IsNullOrEmpty(File.ReadAllText(ConfigHandler.GetInstallCookiePath()));
			}

			return bIsInstallCookieEmpty;
		}

		/// <summary>
		/// Checks whether or not the server provides binaries and patches for the specified platform.
		/// </summary>
		/// <returns><c>true</c>, if the server does provide files for the platform, <c>false</c> otherwise.</returns>
		/// <param name="platform">platform.</param>
		public bool IsPlatformAvailable(ESystemTarget platform)
		{
			PatchProtocolHandler patchService = Configuration.GetPatchProtocol();
			return patchService.IsPlatformAvailable(platform);
		}
	}
}

