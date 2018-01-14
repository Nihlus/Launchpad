//
//  ManifestGenerationHandler.cs
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
using Launchpad.Common.Enums;
using Launchpad.Common.Handlers;
using Launchpad.Common.Handlers.Manifest;

namespace Launchpad.Utilities.Handlers
{
	public class ManifestGenerationHandler
	{
		public event EventHandler<ManifestGenerationProgressChangedEventArgs> ManifestGenerationProgressChanged;
		public event EventHandler ManifestGenerationFinished;

		private readonly ManifestGenerationProgressChangedEventArgs GenerationProgressArgs = new ManifestGenerationProgressChangedEventArgs();

		/// <summary>
		/// Generates a manifest containing the relative path, MD5 hash and file size from
		/// all files in the provided root path.
		/// </summary>
		/// <param name="targetPath">The root path of the directory the manifest should represent.</param>
		/// <param name="manifestType">The type of manifest that should be generated.</param>
		public void GenerateManifest(string targetPath, EManifestType manifestType)
		{
			Thread t = new Thread(() => GenerateManifest_Implementation(targetPath, manifestType))
			{
				Name = "GenerateManifest"
			};

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

					this.GenerationProgressArgs.TotalFiles = manifestFilePaths.Count;
					this.GenerationProgressArgs.CompletedFiles = completedFiles;
					this.GenerationProgressArgs.Filepath = newEntry.RelativePath;
					this.GenerationProgressArgs.Hash = newEntry.Hash;
					this.GenerationProgressArgs.Filesize = newEntry.Size;
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

		/// <summary>
		/// Determines whether or not the specified path is blacklisted and should not be included in the manifest.
		/// </summary>
		/// <param name="filePath">The path to test.</param>
		/// <returns><value>true</value> if the path is blackliste; otherwise, <value>false</value>.</returns>
		private static bool IsPathABlacklistedFile(string filePath)
		{
			return 	filePath.EndsWith(".install") ||
			       	filePath.EndsWith(".update") ||
			       	filePath.EndsWith("GameManifest.txt") ||
					filePath.EndsWith("GameManifest.checksum");
		}

		private void OnManifestGenerationProgressChanged()
		{
			this.ManifestGenerationProgressChanged?.Invoke(this, this.GenerationProgressArgs);
		}

		private void OnManifestGenerationFinished()
		{
			this.ManifestGenerationFinished?.Invoke(this, EventArgs.Empty);
		}
	}
}

