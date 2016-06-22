//
//  ManifestHandler.cs
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
using Launchpad.Utilities.Utility.Events;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Launchpad.Utilities.Handlers
{
	public class ManifestHandler
	{
		public event ManifestGenerationProgressChangedEventHandler ManifestGenerationProgressChanged;
		public event ManifestGenerationFinishedEventHandler ManifestGenerationFinished;

		private readonly ManifestGenerationProgressChangedEventArgs GenerationProgressArgs = new ManifestGenerationProgressChangedEventArgs();

		/// <summary>
		/// Generates a manifest containing the relative path, MD5 hash and file size from
		/// all files in the provided root path.
		/// </summary>
		/// <param name="targetPath">The root path of the directory the manifest should represent.</param>
		/// <param name="manifestType">The type of manifest that should be generated.</param>
		public void GenerateManifest(string targetPath, EManifestType manifestType)
		{
			Thread t = new Thread(() => GenerateManifest_Implementation(targetPath, manifestType));
			t.Start();
		}

		/// <summary>
		/// The asynchronous implementation of the GenerateManifest function.
		/// </summary>
		/// <param name="targetPath">The root path of the directory the manifest should represent.</param>
        /// <param name="manifestType">The type of manifest that should be generated.</param>
		private void GenerateManifest_Implementation(string targetPath, EManifestType manifestType)
		{
			string parentDirectory = Directory.GetParent(targetPath).ToString();
			string manifestPath = $@"{parentDirectory}{Path.DirectorySeparatorChar}{manifestType}Manifest.txt";
			string manifestChecksumPath = $@"{parentDirectory}{Path.DirectorySeparatorChar}{manifestType}Manifest.checksum";

			List<string> manifestFilePaths = new List<string>(Directory
				.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories)
				.Where(s => !IsPathABlacklistedFile(s)));

			using (TextWriter tw = new StreamWriter(File.Create(manifestPath)))
			{
				int completedFiles = 0;
				foreach (string filePath in manifestFilePaths)
				{
					ManifestEntry newEntry = CreateEntryForFile(targetPath, filePath);

					tw.WriteLine(newEntry);
					tw.Flush();

					completedFiles++;

					GenerationProgressArgs.TotalFiles = manifestFilePaths.Count;
					GenerationProgressArgs.CompletedFiles = completedFiles;
					GenerationProgressArgs.Filepath = newEntry.RelativePath;
					GenerationProgressArgs.Hash = newEntry.Hash;
					GenerationProgressArgs.Filesize = newEntry.Size;
					OnManifestGenerationProgressChanged();
				}
			}

			// Create a checksum file for the manifest.
			using (Stream manifestStream = File.OpenRead(manifestPath))
			{
				string manifestHash = MD5Handler.GetStreamHash(manifestStream);

				using (FileStream checksumStream = File.Create(manifestChecksumPath))
				{
					using (TextWriter tw = new StreamWriter(checksumStream))
					{
						tw.WriteLine(manifestHash);
						tw.Close();
					}
				}
			}

			OnManifestGenerationFinished();
		}

		private static ManifestEntry CreateEntryForFile(string parentDirectory, string filePath)
		{
			string hash;
			long fileSize;
			using (FileStream fileStream = File.OpenRead(filePath))
			{
				// Calculate the hash of the file
				hash = MD5Handler.GetStreamHash(fileStream);

				// Get the disk size of the file
				fileSize = fileStream.Length;
			}

			// Get the relative path of the file
			string relativeFilePath = filePath.Substring(parentDirectory.Length);

			// Write the entry to the manifest
			ManifestEntry newEntry = new ManifestEntry
			{
				RelativePath = relativeFilePath,
				Hash = hash,
				Size = fileSize
			};

			return newEntry;
		}

		private static bool IsPathABlacklistedFile(string filePath)
		{
			return 	filePath.EndsWith(".install") ||
			       	filePath.EndsWith(".update") ||
			       	filePath.EndsWith("GameManifest.txt") ||
					filePath.EndsWith("GameManifest.checksum");
		}

		private void OnManifestGenerationProgressChanged()
		{
			ManifestGenerationProgressChanged?.Invoke(this, GenerationProgressArgs);
		}

		private void OnManifestGenerationFinished()
		{
			ManifestGenerationFinished?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Enum defining the type of manifest.
	/// </summary>
	public enum EManifestType : byte
	{
		Unknown,
		Launchpad,
		Game
	}

	/// <summary>
	/// A manifest entry derived from the raw unformatted string.
	/// Contains the relative path of the referenced file, as well as
	/// its MD5 hash and size in bytes.
	/// </summary>
	internal sealed class ManifestEntry : IEquatable<ManifestEntry>
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
			RelativePath = string.Empty;
			Hash = string.Empty;
			Size = 0;
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

			if (!string.IsNullOrEmpty(rawInput))
			{
				// Remove any and all bad characters from the input string,
				// such as \0, \n and \r.
				string cleanInput = SanitizeString(rawInput);

				// Split the string into its three components - file, hash and size
				string[] entryElements = cleanInput.Split(':');

				// If we have three elements (which we should always have), set them in the provided entry
				if (entryElements.Length == 3)
				{
					// Normalize the manifest path, converting \ to / on unix and / to \ on Windows.
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
			}

			return false;
		}

		/// <summary>
		/// Clean the specified input from newlines and nulls (\r, \n and \0)
		/// </summary>
		/// <param name="input">Input string.</param>
		private static string SanitizeString(string input)
		{
			return input.Replace("\n", string.Empty).Replace("\0", string.Empty).Replace("\r", string.Empty);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Launchpad.Utilities.Handlers.ManifestEntry"/>.
		/// The returned value matches a raw in-manifest representation of the entry, in the form of
		/// [path]:[hash]:[size]
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Launchpad.Utilities.Handlers.ManifestEntry"/>.</returns>
		public override string ToString()
		{
			return RelativePath + ":" + Hash + ":" + Size;
		}

		public bool Equals(ManifestEntry other)
		{
			return this.RelativePath == other.RelativePath &&
			this.Hash == other.Hash &&
			this.Size == other.Size;
		}
	}
}

