//
//  ManifestEntry.cs
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

namespace Launchpad.Common.Handlers.Manifest
{
	/// <summary>
	/// A manifest entry derived from the raw unformatted string.
	/// Contains the relative path of the referenced file, as well as
	/// its MD5 hash and size in bytes.
	/// </summary>
	public sealed class ManifestEntry : IEquatable<ManifestEntry>
	{
		/// <summary>
		/// Gets or sets the path of the file, relative to the game directory.
		/// </summary>
		public string RelativePath
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the MD5 hash of the file.
		/// </summary>
		public string Hash
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the size in bytes of the file.
		/// </summary>
		public long Size
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManifestEntry"/> class.
		/// </summary>
		public ManifestEntry()
		{
			this.RelativePath = string.Empty;
			this.Hash = string.Empty;
			this.Size = 0;
		}

		/// <summary>
		/// Attempts to parse an entry from a raw input.
		/// The input is expected to be in [path]:[hash]:[size] format. Note that the file path is case sensitive,
		/// but the hash is not.
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

			var cleanInput = rawInput.RemoveLineSeparatorsAndNulls();

			// Split the string into its three components - file, hash and size
			var entryElements = cleanInput.Split(':');

			// If we have three elements (which we should always have), set them in the provided entry
			if (entryElements.Length != 3)
			{
				return false;
			}

			// Sanitize the manifest path, converting \ to / on unix and / to \ on Windows.
			if (PlatformHelpers.IsRunningOnUnix())
			{
				inEntry.RelativePath = entryElements[0].Replace('\\', '/');
			}
			else
			{
				inEntry.RelativePath = entryElements[0].Replace('/', '\\');
			}

			// Hashes must be exactly 32 characters
			if (entryElements[1].Length != 32)
			{
				return false;
			}

			// Set the hash to the second element
			inEntry.Hash = entryElements[1];

			// Attempt to parse the final element as a long-type byte count.
			if (!long.TryParse(entryElements[2], out var parsedSize))
			{
				// Oops. The parsing failed, so this entry is invalid.
				return false;
			}

			// Negative sizes are not allowed
			if (parsedSize < 0)
			{
				return false;
			}

			inEntry.Size = parsedSize;
			return true;
		}

		/// <summary>
		/// Returns a <see cref="string"/> that represents the current <see cref="ManifestEntry"/>.
		/// The returned value matches a raw in-manifest representation of the entry, in the form of
		/// [path]:[hash]:[size]
		/// </summary>
		/// <returns>A <see cref="string"/> that represents the current <see cref="ManifestEntry"/>.</returns>
		public override string ToString()
		{
			return $"{this.RelativePath}:{this.Hash}:{this.Size}";
		}

		/// <summary>
		/// Determines whether the specified <see cref="object"/> is equal to the current <see cref="ManifestEntry"/>.
		/// </summary>
		/// <param name="obj">The <see cref="object"/> to compare with the current <see cref="ManifestEntry"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="object"/> is equal to the current
		/// <see cref="ManifestEntry"/>; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as ManifestEntry);
		}

		/// <inheritdoc />
		public bool Equals(ManifestEntry other)
		{
			if (other == null)
			{
				return false;
			}

			return this.RelativePath == other.RelativePath &&
				string.Equals(this.Hash, other.Hash, StringComparison.InvariantCultureIgnoreCase) &&
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
	}
}
