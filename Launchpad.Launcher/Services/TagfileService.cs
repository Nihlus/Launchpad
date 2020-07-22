//
//  TagfileService.cs
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
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.Services
{
    /// <summary>
    /// Service for creating and managing tagfiles.
    /// </summary>
    public class TagfileService
    {
        private readonly DirectoryHelpers _directoryHelpers;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagfileService"/> class.
        /// </summary>
        /// <param name="directoryHelpers">The directory helpers.</param>
        public TagfileService(DirectoryHelpers directoryHelpers)
        {
            _directoryHelpers = directoryHelpers;
        }

        /// <summary>
        /// Creates the launcher cookie.
        /// </summary>
        public void CreateLauncherTagfile()
        {
            var launcherTagfilePath = _directoryHelpers.GetLauncherTagfilePath();

            var doesCookieExist = File.Exists(launcherTagfilePath);
            if (!doesCookieExist)
            {
                File.Create(launcherTagfilePath);
            }
        }

        /// <summary>
        /// Creates the install cookie.
        /// </summary>
        public void CreateGameTagfile()
        {
            var gameTagfilePath = _directoryHelpers.GetGameTagfilePath();

            var doesCookieExist = File.Exists(gameTagfilePath);
            if (!doesCookieExist)
            {
                File.Create(gameTagfilePath).Close();
            }
        }
    }
}
