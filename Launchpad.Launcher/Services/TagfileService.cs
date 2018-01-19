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
		/// <summary>
		/// Creates the launcher cookie.
		/// </summary>
		public void CreateLauncherTagfile()
		{
			var doesCookieExist = File.Exists(DirectoryHelpers.GetLauncherTagfilePath());
			if (!doesCookieExist)
			{
				File.Create(DirectoryHelpers.GetLauncherTagfilePath());
			}
		}

		/// <summary>
		/// Creates the install cookie.
		/// </summary>
		public void CreateGameTagfile()
		{
			var doesCookieExist = File.Exists(DirectoryHelpers.GetGameTagfilePath());
			if (!doesCookieExist)
			{
				File.Create(DirectoryHelpers.GetGameTagfilePath()).Close();
			}
		}
	}
}
