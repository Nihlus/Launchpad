//
//  ManifestEntryExtensions.cs
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
using Launchpad.Common.Handlers;
using Launchpad.Common.Handlers.Manifest;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher
{
	/// <summary>
	/// Extension methods for the <see cref="ManifestEntry"/> class.
	/// </summary>
	public static class ManifestEntryExtensions
	{
		/// <summary>
		/// Verifies the integrity of the file in the manifest entry.
		/// </summary>
		/// <param name="entry">The manifest entry to test.</param>
		/// <returns><c>true</c>, if file was complete and undamaged, <c>false</c> otherwise.</returns>
		public static bool IsFileIntegrityIntact(this ManifestEntry entry)
		{
			var localPath = $"{DirectoryHelpers.GetLocalGameDirectory()}{entry.RelativePath}";
			if (!File.Exists(localPath))
			{
				return false;
			}

			var fileInfo = new FileInfo(localPath);
			if (fileInfo.Length != entry.Size)
			{
				return false;
			}

			using (Stream file = File.OpenRead(localPath))
			{
				string localHash = MD5Handler.GetStreamHash(file);
				if (localHash != entry.Hash)
				{
					return false;
				}
			}

			return true;
		}
	}
}
