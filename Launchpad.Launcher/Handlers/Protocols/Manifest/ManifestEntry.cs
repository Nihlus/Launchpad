//
//  ManifestEntry.cs
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
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.Handlers.Protocols.Manifest
{
	/// <summary>
	/// A manifest entry derived from the raw unformatted string.
	/// Contains the relative path of the referenced file, as well as
	/// its MD5 hash and size in bytes.
	/// </summary>
	public sealed class ManifestEntry : IEquatable<ManifestEntry>
	{
		public string RelativePath
		{
			get;
			set;
		}

		public string Hash
		{
			get;
			set;
		}

		public long Size
		{
			get;
			set;
		}

		public ManifestEntry()
		{
			this.RelativePath = string.Empty;
			this.Hash = string.Empty;
			this.Size = 0;
		}

		/// <summary>
		/// Attempts to parse an entry from a raw input.
		/// The input is expected to be in [path]:[hash]:[size] format.
		/// </summary>
		/// <returns><c>true</c>, if the input was successfully parse, <c>false</c> otherwise.</returns>
		/// <param name="rawInput">Raw input.</param>
		/// <param name="inEntry">The resulting entry.</param>
		public static bool TryParse(string rawInput, out ManifestEntry inEntry)
		{
			// Clear out the entry for the new data
			inEntry = new ManifestEntry();

			if (string.IsNullOrEmpty(rawInput))
			{
				return false;
			}

			string cleanInput = rawInput.RemoveLineSeparatorsAndNulls();

			// Split the string into its three components - file, hash and size
			string[] entryElements = cleanInput.Split(':');

			// If we have three elements (which we should always have), set them in the provided entry
			if (entryElements.Length != 3)
			{
				return false;
			}

			// Sanitize the manifest path, converting \ to / on unix and / to \ on Windows.
			if (ChecksHandler.IsRunningOnUnix())
			{
				inEntry.RelativePath = entryElements[0].Replace("\\", "/");
			}
			else
			{
				inEntry.RelativePath = entryElements[0].Replace("/", "\\");
			}

			// Set the hash to the second element
			inEntry.Hash = entryElements[1];

			// Attempt to parse the final element as a long-type byte count.
			long parsedSize;
			if (!long.TryParse(entryElements[2], out parsedSize))
			{
				// Oops. The parsing failed, so this entry is invalid.
				return false;
			}

			inEntry.Size = parsedSize;
			return true;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="ManifestEntry"/>.
		/// The returned value matches a raw in-manifest representation of the entry, in the form of
		/// [path]:[hash]:[size]
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="ManifestEntry"/>.</returns>
		public override string ToString()
		{
			return this.RelativePath + ":" + this.Hash + ":" + this.Size;
		}

		/// <summary>
		/// Determines whether the specified <see cref="ManifestEntry"/> is equal to the current <see cref="ManifestEntry"/>.
		/// </summary>
		/// <param name="other">The <see cref="ManifestEntry"/> to compare with the current <see cref="ManifestEntry"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="ManifestEntry"/> is equal to the current
		/// <see cref="ManifestEntry"/>; otherwise, <c>false</c>.</returns>
		public bool Equals(ManifestEntry other)
		{
			if (other == null)
			{
				return false;
			}

			return this.RelativePath == other.RelativePath &&
			       this.Hash == other.Hash &&
			       this.Size == other.Size;
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="ManifestEntry"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		/// <summary>
		/// Verifies the integrity of the file in the manifest entry.
		/// </summary>
		/// <returns><c>true</c>, if file was complete and undamaged, <c>false</c> otherwise.</returns>
		public bool IsFileIntegrityIntact()
		{
			string localPath = $"{ConfigHandler.Instance.GetGamePath()}{this.RelativePath}";
			if (!File.Exists(localPath))
			{
				return false;
			}

			FileInfo fileInfo = new FileInfo(localPath);
			if (fileInfo.Length != this.Size)
			{
				return false;
			}

			using (Stream file = File.OpenRead(localPath))
			{
				string localHash = MD5Handler.GetStreamHash(file);
				if (localHash != this.Hash)
				{
					return false;
				}
			}

			return true;
		}
	}
}