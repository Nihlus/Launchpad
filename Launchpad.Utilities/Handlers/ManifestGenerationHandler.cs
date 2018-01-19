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
using System.Threading.Tasks;
using Launchpad.Common.Enums;
using Launchpad.Common.Handlers;
using Launchpad.Common.Handlers.Manifest;

namespace Launchpad.Utilities.Handlers
{
	public class ManifestGenerationHandler
	{
		private readonly ManifestGenerationProgressChangedEventArgs GenerationProgressArgs = new ManifestGenerationProgressChangedEventArgs();

		/// <summary>
		/// Generates a manifest containing the relative path, MD5 hash and file size from
		/// all files in the provided root path.
		/// </summary>
		/// <param name="targetPath">The root path of the directory the manifest should represent.</param>
		/// <param name="manifestType">The type of manifest that should be generated.</param>
		/// <param name="progressReporter">The progress reporter to use.</param>
		/// <param name="ct">The cancellation token to use.</param>
		public Task GenerateManifestAsync
		(
			string targetPath,
			EManifestType manifestType,
			IProgress<ManifestGenerationProgressChangedEventArgs> progressReporter,
			CancellationToken ct
		)
		{
			var parentDirectory = Directory.GetParent(targetPath).ToString();

			var manifestPath = Path.Combine(parentDirectory, $"{manifestType}Manifest.txt");
			var manifestChecksumPath = Path.Combine(parentDirectory, $"{manifestType}Manifest.checksum");

			return Task.Run
			(
				async () =>
				{
					var manifestFilePaths = new List<string>(Directory
						.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories)
						.Where(s => !IsPathABlacklistedFile(s)));

					this.GenerationProgressArgs.TotalFiles = manifestFilePaths.Count;

					using (var tw = new StreamWriter(File.Create(manifestPath, 4096, FileOptions.Asynchronous)))
					{
						var completedFiles = 0;
						foreach (var filePath in manifestFilePaths)
						{
							ct.ThrowIfCancellationRequested();

							var newEntry = CreateEntryForFile(targetPath, filePath);

							await tw.WriteLineAsync(newEntry.ToString());
							await tw.FlushAsync();

							completedFiles++;

							this.GenerationProgressArgs.CompletedFiles = completedFiles;
							this.GenerationProgressArgs.Filepath = newEntry.RelativePath;
							this.GenerationProgressArgs.Hash = newEntry.Hash;
							this.GenerationProgressArgs.Filesize = newEntry.Size;

							progressReporter.Report(this.GenerationProgressArgs);
						}
					}

					await CreateManifestChecksumAsync(manifestPath, manifestChecksumPath);
				},
				ct
			);
		}

		private async Task CreateManifestChecksumAsync(string manifestPath, string manifestChecksumPath)
		{
			// Create a checksum file for the manifest.
			using (var manifestStream = File.OpenRead(manifestPath))
			{
				var manifestHash = MD5Handler.GetStreamHash(manifestStream);

				using (var checksumStream = File.Create(manifestChecksumPath, 4096, FileOptions.Asynchronous))
				{
					using (var tw = new StreamWriter(checksumStream))
					{
						await tw.WriteLineAsync(manifestHash);
						await tw.FlushAsync();
						tw.Close();
					}
				}
			}
		}

		private ManifestEntry CreateEntryForFile(string parentDirectory, string filePath)
		{
			string hash;
			long fileSize;
			using (var fileStream = File.OpenRead(filePath))
			{
				hash = MD5Handler.GetStreamHash(fileStream);
				fileSize = fileStream.Length;
			}

			var relativeFilePath = filePath.Substring(parentDirectory.Length);
			var newEntry = new ManifestEntry
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
		private bool IsPathABlacklistedFile(string filePath)
		{
			return
				filePath.EndsWith(".install") ||
		        filePath.EndsWith(".update") ||
		        filePath.EndsWith("GameManifest.txt") ||
				filePath.EndsWith("GameManifest.checksum");
		}
	}
}

