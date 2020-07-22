//
//  LocalVersionService.cs
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
using System.IO;
using Launchpad.Launcher.Utility;
using Microsoft.Extensions.Logging;

namespace Launchpad.Launcher.Services
{
    /// <summary>
    /// A service which handles local version discovery.
    /// </summary>
    public class LocalVersionService
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private readonly ILogger<LocalVersionService> _log;

        /// <summary>
        /// The directory helpers.
        /// </summary>
        private readonly DirectoryHelpers _directoryHelpers;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalVersionService"/> class.
        /// </summary>
        /// <param name="log">The logging instance.</param>
        /// <param name="directoryHelpers">The directory helpers.</param>
        public LocalVersionService(ILogger<LocalVersionService> log, DirectoryHelpers directoryHelpers)
        {
            _log = log;
            _directoryHelpers = directoryHelpers;
        }

        /// <summary>
        /// Gets the local game version.
        /// </summary>
        /// <returns>The local game version.</returns>
        public Version GetLocalGameVersion()
        {
            try
            {
                var rawGameVersion = File.ReadAllText(_directoryHelpers.GetLocalGameVersionPath());

                if (Version.TryParse(rawGameVersion, out var gameVersion))
                {
                    return gameVersion;
                }

                _log.LogWarning("Could not parse local game version. Contents: " + rawGameVersion);
                return new Version("0.0.0");
            }
            catch (IOException ioex)
            {
                _log.LogWarning("Could not read local game version (IOException): " + ioex.Message);
                return new Version("0.0.0");
            }
        }

        /// <summary>
        /// Gets the local launcher version.
        /// </summary>
        /// <returns>The version.</returns>
        public Version GetLocalLauncherVersion()
        {
            return GetType().Assembly.GetName().Version ?? new Version("0.0.0");
        }
    }
}
