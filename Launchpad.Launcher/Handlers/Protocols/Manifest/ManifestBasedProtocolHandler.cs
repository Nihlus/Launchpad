//
//  ManifestBasedProtocolHandler.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Launchpad.Common;
using Launchpad.Common.Enums;
using Launchpad.Common.Handlers;
using Launchpad.Common.Handlers.Manifest;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using NGettext;
using NLog;

namespace Launchpad.Launcher.Handlers.Protocols.Manifest
{
	/// <summary>
	/// Base underlying class for protocols using a manifest.
	/// </summary>
	public abstract class ManifestBasedProtocolHandler : PatchProtocolHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The localization catalog.
		/// </summary>
		private static readonly ICatalog LocalizationCatalog = new Catalog("Launchpad", "./Content/locale");

		private readonly LocalVersionService LocalVersionService = new LocalVersionService();

		/// <summary>
		/// The file manifest handler. This allows access to the launcher and game file lists.
		/// </summary>
		private readonly ManifestHandler FileManifestHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="ManifestBasedProtocolHandler"/> class.
		/// </summary>
		protected ManifestBasedProtocolHandler()
		{
			this.FileManifestHandler = new ManifestHandler
			(
				DirectoryHelpers.GetLocalLauncherDirectory(),
				this.Configuration.RemoteAddress,
				this.Configuration.SystemTarget
			);
		}

		/// <inheritdoc />
		public override void InstallGame()
		{
			try
			{
				// Create the .install file to mark that an installation has begun.
				// If it exists, do nothing.
				this.TagfileService.CreateGameTagfile();

				// Make sure the manifest is up to date
				RefreshModuleManifest(EModule.Game);

				// Download Game
				DownloadModule(EModule.Game);

				// Verify Game
				VerifyModule(EModule.Game);
			}
			catch (IOException ioex)
			{
				Log.Warn("Game installation failed (IOException): " + ioex.Message);
			}

			// OnModuleInstallationFinished and OnModuleInstallationFailed is in VerifyGame
			// in order to allow it to run as a standalone action, while still keeping this functional.

			// As a side effect, it is required that it is the last action to run in Install and Update,
			// which happens to coincide with the general design.
		}

		/// <inheritdoc />
		public override void UpdateModule(EModule module)
		{
			IReadOnlyList<ManifestEntry> manifest;
			IReadOnlyList<ManifestEntry> oldManifest;
			switch (module)
			{
				case EModule.Launcher:
				{
					RefreshModuleManifest(EModule.Launcher);

					manifest = this.FileManifestHandler.GetManifest(EManifestType.Launchpad, false);
					oldManifest = this.FileManifestHandler.GetManifest(EManifestType.Launchpad, true);
					break;
				}
				case EModule.Game:
				{
					RefreshModuleManifest(EModule.Game);

					manifest = this.FileManifestHandler.GetManifest(EManifestType.Game, false);
					oldManifest = this.FileManifestHandler.GetManifest(EManifestType.Game, true);
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, "An invalid module value was passed to UpdateModule.");
				}
			}

			// Check to see if we have valid manifests
			if (manifest == null)
			{
				Log.Error($"No manifest was found when updating the module \"{module}\". The server files may be inaccessible or missing.");
				OnModuleInstallationFailed(module);
				return;
			}

			// This dictionary holds a list of new entries and their equivalents from the old manifest. It is used
			// to determine whether or not a file is partial, or merely old yet smaller.
			var oldEntriesBeingReplaced = new Dictionary<ManifestEntry, ManifestEntry>();
			var filesRequiringUpdate = new List<ManifestEntry>();
			foreach (var fileEntry in manifest)
			{
				filesRequiringUpdate.Add(fileEntry);
				if (oldManifest == null)
				{
					continue;
				}

				if (oldManifest.Contains(fileEntry))
				{
					continue;
				}

				// See if there is an old entry which matches the new one.
				var matchingOldEntry =
					oldManifest.FirstOrDefault(oldEntry => oldEntry.RelativePath == fileEntry.RelativePath);

				if (matchingOldEntry != null)
				{
					oldEntriesBeingReplaced.Add(fileEntry, matchingOldEntry);
				}
			}

			try
			{
				var updatedFiles = 0;
				foreach (var fileEntry in filesRequiringUpdate)
				{
					++updatedFiles;

					this.ModuleUpdateProgressArgs.IndicatorLabelMessage = GetUpdateIndicatorLabelMessage
					(
						Path.GetFileName(fileEntry.RelativePath),
						updatedFiles,
						filesRequiringUpdate.Count
					);
					OnModuleUpdateProgressChanged();

					// If we're updating an existing file, make sure to let the downloader know
					if (oldEntriesBeingReplaced.ContainsKey(fileEntry))
					{
						DownloadManifestEntry(fileEntry, module, oldEntriesBeingReplaced[fileEntry]);
					}
					else
					{
						DownloadManifestEntry(fileEntry, module);
					}
				}
			}
			catch (IOException ioex)
			{
				Log.Warn($"Updating of {module} files failed (IOException): " + ioex.Message);
				OnModuleInstallationFailed(module);
				return;
			}

			OnModuleInstallationFinished(module);
		}

		/// <inheritdoc />
		public override void VerifyModule(EModule module)
		{
			var manifest = this.FileManifestHandler.GetManifest((EManifestType)module, false);
			var brokenFiles = new List<ManifestEntry>();

			if (manifest == null)
			{
				Log.Error($"No manifest was found when verifying the module \"{module}\". The server files may be inaccessible or missing.");
				OnModuleInstallationFailed(module);
				return;
			}

			try
			{
				var verifiedFiles = 0;
				foreach (var fileEntry in manifest)
				{
					++verifiedFiles;

					// Prepare the progress event contents
					this.ModuleVerifyProgressArgs.IndicatorLabelMessage = GetVerifyIndicatorLabelMessage
					(
						Path.GetFileName(fileEntry.RelativePath),
						verifiedFiles,
						manifest.Count
					);
					OnModuleVerifyProgressChanged();

					if (fileEntry.IsFileIntegrityIntact())
					{
						continue;
					}

					brokenFiles.Add(fileEntry);
					Log.Info($"File \"{Path.GetFileName(fileEntry.RelativePath)}\" failed its integrity check and was queued for redownload.");
				}

				var downloadedFiles = 0;
				foreach (var fileEntry in brokenFiles)
				{
					++downloadedFiles;

					// Prepare the progress event contents
					this.ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage
					(
						Path.GetFileName(fileEntry.RelativePath),
						downloadedFiles,
						brokenFiles.Count
					);
					OnModuleDownloadProgressChanged();

					for (int i = 0; i < this.Configuration.RemoteFileDownloadRetries; ++i)
					{
						if (!fileEntry.IsFileIntegrityIntact())
						{
							DownloadManifestEntry(fileEntry, module);
							Log.Info($"File \"{Path.GetFileName(fileEntry.RelativePath)}\" failed its integrity check again after redownloading. ({i} retries)");
						}
						else
						{
							break;
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Log.Warn($"Verification of {module} files failed (IOException): " + ioex.Message);
				OnModuleInstallationFailed(module);
			}

			OnModuleInstallationFinished(module);
		}

		/// <inheritdoc />
		protected override void DownloadModule(EModule module)
		{
			IReadOnlyList<ManifestEntry> moduleManifest;
			switch (module)
			{
				case EModule.Launcher:
				{
					RefreshModuleManifest(EModule.Launcher);

					moduleManifest = this.FileManifestHandler.GetManifest(EManifestType.Launchpad, false);
					break;
				}
				case EModule.Game:
				{
					RefreshModuleManifest(EModule.Game);

					moduleManifest = this.FileManifestHandler.GetManifest(EManifestType.Game, false);
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException
					(
						nameof(module),
						module,
						"An invalid module value was passed to DownloadModule."
					);
				}
			}

			if (moduleManifest == null)
			{
				Log.Error($"No manifest was found when installing the module \"{module}\". The server files may be inaccessible or missing.");
				OnModuleInstallationFailed(module);
				return;
			}

			// In order to be able to resume downloading, we check if there is an entry
			// stored in the install cookie.

			// Attempt to parse whatever is inside the install cookie
			if (ManifestEntry.TryParse(File.ReadAllText(DirectoryHelpers.GetGameTagfilePath()), out var lastDownloadedFile))
			{
				// Loop through all the entries in the manifest until we encounter
				// an entry which matches the one in the install cookie
				foreach (var fileEntry in moduleManifest)
				{
					if (lastDownloadedFile.Equals(fileEntry))
					{
						// Skip all entries before the one we were last at.
						moduleManifest = moduleManifest.Skip(moduleManifest.ToList().IndexOf(fileEntry)).ToList();
					}
				}
			}

			var downloadedFiles = 0;
			foreach (var fileEntry in moduleManifest)
			{
				++downloadedFiles;

				// Prepare the progress event contents
				this.ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage
				(
					Path.GetFileName(fileEntry.RelativePath),
					downloadedFiles,
					moduleManifest.Count
				);
				OnModuleDownloadProgressChanged();

				DownloadManifestEntry(fileEntry, module);
			}
		}

		/// <summary>
		/// Reads the contents of a remote file as a string.
		/// </summary>
		/// <param name="url">The URL to read.</param>
		/// <param name="useAnonymousLogin">Whether or not to use anonymous credentials.</param>
		/// <returns>The contents of the file.</returns>
		protected abstract string ReadRemoteFile(string url, bool useAnonymousLogin = false);

		/// <summary>
		/// Downloads the contents of the file at the specified url to the specified local path.
		/// This method supported resuming a partial file.
		/// </summary>
		/// <param name="url">The URL to download.</param>
		/// <param name="localPath">The local path where the file should be saved.</param>
		/// <param name="totalSize">The expected total size of the file.</param>
		/// <param name="contentOffset">The offset into the file where reading and writing should start.</param>
		/// <param name="useAnonymousLogin">Whether or not to use anonymous credentials.</param>
		protected abstract void DownloadRemoteFile
		(
			string url,
			string localPath,
			long totalSize = 0,
			long contentOffset = 0,
			bool useAnonymousLogin = false
		);

		/// <inheritdoc />
		public override bool IsModuleOutdated(EModule module)
		{
			try
			{
				Version local;
				Version remote;

				switch (module)
				{
					case EModule.Launcher:
					{
						local = this.LocalVersionService.GetLocalLauncherVersion();
						remote = GetRemoteLauncherVersion();
						break;
					}
					case EModule.Game:
					{
						local = this.LocalVersionService.GetLocalGameVersion();
						remote = GetRemoteGameVersion();
						break;
					}
					default:
					{
						throw new ArgumentOutOfRangeException
						(
							nameof(module),
							module,
							"An invalid module value was passed to IsModuleOutdated."
						);
					}
				}

				return local < remote;
			}
			catch (WebException wex)
			{
				Log.Warn("Unable to determine whether or not the launcher was outdated (WebException): " + wex.Message);
				return false;
			}
		}

		/// <summary>
		/// Downloads the file referred to by the specifed manifest entry.
		/// </summary>
		/// <param name="fileEntry">The entry to download.</param>
		/// <param name="module">The module that the entry belongs to.</param>
		/// <param name="oldFileEntry">The old entry, if one exists.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Will be thrown if the local path set in the <paramref name="fileEntry"/> passed to the function is not a valid value.
		/// </exception>
		protected virtual void DownloadManifestEntry(ManifestEntry fileEntry, EModule module, ManifestEntry oldFileEntry = null)
		{
			this.ModuleDownloadProgressArgs.Module = module;

			string baseRemoteURL;
			string baseLocalPath;
			switch (module)
			{
				case EModule.Launcher:
				{
					baseRemoteURL = DirectoryHelpers.GetRemoteLauncherBinariesPath();
					baseLocalPath = DirectoryHelpers.GetTempLauncherDownloadPath();
					break;
				}
				case EModule.Game:
				{
					baseRemoteURL = DirectoryHelpers.GetRemoteGamePath();
					baseLocalPath = DirectoryHelpers.GetLocalGameDirectory();
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException
					(
						nameof(module),
						module,
						"An invalid module value was passed to DownloadManifestEntry."
					);
				}
			}

			// Build the access strings
			var remoteURL = $"{baseRemoteURL}{fileEntry.RelativePath}";
			var localPath = $"{baseLocalPath}{fileEntry.RelativePath}";

			// Make sure we have a directory to put the file in
			if (!string.IsNullOrEmpty(localPath))
			{
				var localPathParentDir = Path.GetDirectoryName(localPath);
				if (!Directory.Exists(localPathParentDir))
				{
					var parentDir = Path.GetDirectoryName(localPath);
					if (!(parentDir is null))
					{
						Directory.CreateDirectory(parentDir);
					}
				}
			}
			else
			{
				throw new ArgumentNullException(nameof(localPath), "The local path was null or empty.");
			}

			// Reset the cookie
			File.WriteAllText(DirectoryHelpers.GetGameTagfilePath(), string.Empty);

			// Write the current file progress to the install cookie
			using (TextWriter textWriterProgress = new StreamWriter(DirectoryHelpers.GetGameTagfilePath()))
			{
				textWriterProgress.WriteLine(fileEntry);
				textWriterProgress.Flush();
			}

			// First, let's see if an old file exists, and is valid.
			if (oldFileEntry != null)
			{
				// Check if the file is present, the correct size, and the correct hash
				if (oldFileEntry.IsFileIntegrityIntact())
				{
					// If it is, delete it.
					File.Delete(localPath);
				}
			}

			if (File.Exists(localPath))
			{
				var fileInfo = new FileInfo(localPath);
				if (fileInfo.Length != fileEntry.Size)
				{
					// If the file is partial, resume the download.
					if (fileInfo.Length < fileEntry.Size)
					{
						Log.Info($"Resuming interrupted file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\" at byte {fileInfo.Length}.");
						DownloadRemoteFile(remoteURL, localPath, fileEntry.Size, fileInfo.Length);
					}
					else
					{
						// If it's larger than expected, toss it in the bin and try again.
						Log.Info($"Restarting interrupted file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\": File bigger than expected.");

						File.Delete(localPath);
						DownloadRemoteFile(remoteURL, localPath, fileEntry.Size);
					}
				}
				else
				{
					string localHash;
					using (var fs = File.OpenRead(localPath))
					{
						localHash = MD5Handler.GetStreamHash(fs);
					}

					if (localHash != fileEntry.Hash)
					{
						// If the hash doesn't match, toss it in the bin and try again.
						Log.Info
						(
							$"Redownloading file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\": " +
							$"Hash sum mismatch. Local: {localHash}, Expected: {fileEntry.Hash}"
						);

						File.Delete(localPath);
						DownloadRemoteFile(remoteURL, localPath, fileEntry.Size);
					}
				}
			}
			else
			{
				// No file, download it
				DownloadRemoteFile(remoteURL, localPath, fileEntry.Size);
			}

			// We've finished the download, so empty the cookie
			File.WriteAllText(DirectoryHelpers.GetGameTagfilePath(), string.Empty);
		}

		/// <summary>
		/// Determines whether or not the local copy of the manifest for the specifed module is outdated.
		/// </summary>
		/// <param name="module">The module.</param>
		/// <returns>true if the manifest is outdated; otherwise, false.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
		/// </exception>
		protected virtual bool IsModuleManifestOutdated(EModule module)
		{
			string manifestPath;
			switch (module)
			{
				case EModule.Launcher:
				case EModule.Game:
				{
					manifestPath = this.FileManifestHandler.GetManifestPath((EManifestType)module, false);
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException
					(
						nameof(module),
						module,
						"An invalid module value was passed to RefreshModuleManifest."
					);
				}
			}

			if (!File.Exists(manifestPath))
			{
				return true;
			}

			var remoteHash = GetRemoteModuleManifestChecksum(module);
			using (var file = File.OpenRead(manifestPath))
			{
				var localHash = MD5Handler.GetStreamHash(file);

				return remoteHash != localHash;
			}
		}

		/// <summary>
		/// Gets the checksum of the manifest for the specified module.
		/// </summary>
		/// <param name="module">The module.</param>
		/// <returns>The checksum.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
		/// </exception>
		protected virtual string GetRemoteModuleManifestChecksum(EModule module)
		{
			string checksum;
			switch (module)
			{
				case EModule.Launcher:
				case EModule.Game:
				{
					checksum = ReadRemoteFile(this.FileManifestHandler.GetManifestChecksumURL((EManifestType)module)).RemoveLineSeparatorsAndNulls();
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException
					(
						nameof(module),
						module,
						"An invalid module value was passed to GetRemoteModuleManifestChecksum."
					);
				}
			}

			return checksum.RemoveLineSeparatorsAndNulls();
		}

		/// <summary>
		/// Refreshes the current manifest by redownloading it, if required;
		/// </summary>
		/// <param name="module">The module.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
		/// </exception>
		protected virtual void RefreshModuleManifest(EModule module)
		{
			bool manifestExists;
			switch (module)
			{
				case EModule.Launcher:
				case EModule.Game:
				{
					manifestExists = File.Exists(this.FileManifestHandler.GetManifestPath((EManifestType)module, false));
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException
					(
						nameof(module),
						module,
						"An invalid module value was passed to RefreshModuleManifest"
					);
				}
			}

			if (manifestExists)
			{
				if (IsModuleManifestOutdated(module))
				{
					DownloadModuleManifest(module);
				}
			}
			else
			{
				DownloadModuleManifest(module);
			}

			// Now update the handler instance
			this.FileManifestHandler.ReloadManifests((EManifestType)module);
		}

		/// <summary>
		/// Downloads the manifest for the specified module, and backs up the old copy of the manifest.
		/// </summary>
		/// <param name="module">The module.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
		/// </exception>
		protected virtual void DownloadModuleManifest(EModule module)
		{
			string remoteURL;
			string localPath;
			string oldLocalPath;
			switch (module)
			{
				case EModule.Launcher:
				case EModule.Game:
				{
					remoteURL = this.FileManifestHandler.GetManifestURL((EManifestType)module);
					localPath = this.FileManifestHandler.GetManifestPath((EManifestType)module, false);
					oldLocalPath = this.FileManifestHandler.GetManifestPath((EManifestType)module, true);

					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException
					(
						nameof(module),
						module,
						"An invalid module value was passed to DownloadModuleManifest"
					);
				}
			}

			try
			{
				// Delete the old backup (if there is one)
				if (File.Exists(oldLocalPath))
				{
					File.Delete(oldLocalPath);
				}

				// Create a backup of the old manifest so that we can compare them when updating the game
				if (File.Exists(localPath))
				{
					File.Move(localPath, oldLocalPath);
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Failed to back up the old launcher manifest (IOException): " + ioex.Message);
			}

			DownloadRemoteFile(remoteURL, localPath);
		}

		/// <summary>
		/// Gets the remote launcher version.
		/// </summary>
		/// <returns>The remote launcher version.
		/// If the version could not be retrieved from the server, a version of 0.0.0 is returned.</returns>
		protected virtual Version GetRemoteLauncherVersion()
		{
			var remoteVersionPath = DirectoryHelpers.GetRemoteLauncherVersionPath();

			// Config.GetDoOfficialUpdates is used here since the official update server always allows anonymous logins.
			var remoteVersion = ReadRemoteFile(remoteVersionPath, this.Configuration.UseOfficialUpdates).RemoveLineSeparatorsAndNulls();

			if (Version.TryParse(remoteVersion, out var version))
			{
				return version;
			}

			Log.Warn("Failed to parse the remote launcher version. Using the default of 0.0.0 instead.");
			return new Version("0.0.0");
		}

		/// <summary>
		/// Gets the remote game version.
		/// </summary>
		/// <returns>The remote game version.</returns>
		protected virtual Version GetRemoteGameVersion()
		{
			var remoteVersionPath = $"{this.Configuration.RemoteAddress}/game/{this.Configuration.SystemTarget}/bin/GameVersion.txt";
			var remoteVersion = ReadRemoteFile(remoteVersionPath).RemoveLineSeparatorsAndNulls();

			if (Version.TryParse(remoteVersion, out var version))
			{
				return version;
			}

			Log.Warn("Failed to parse the remote game version. Using the default of 0.0.0 instead.");
			return new Version("0.0.0");
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while repairing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="verifiedFiles">N files downloaded.</param>
		/// <param name="totalFiles">Total files to download.</param>
		protected virtual string GetVerifyIndicatorLabelMessage(string currentFilename, int verifiedFiles, int totalFiles)
		{
			return LocalizationCatalog.GetString("Verifying file {0} ({1} of {2})", currentFilename, verifiedFiles, totalFiles);
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while repairing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="updatedFiles">Number of files that have been updated</param>
		/// <param name="totalFiles">Total files that are to be updated</param>
		protected virtual string GetUpdateIndicatorLabelMessage(string currentFilename, int updatedFiles, int totalFiles)
		{
			return LocalizationCatalog.GetString("Updating file {0} ({1} of {2})", currentFilename, updatedFiles, totalFiles);
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while installing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="downloadedFiles">N files downloaded.</param>
		/// <param name="totalFiles">Total files to download.</param>
		protected virtual string GetDownloadIndicatorLabelMessage(string currentFilename, int downloadedFiles, int totalFiles)
		{
			return LocalizationCatalog.GetString("Downloading file {0} ({1} of {2})", currentFilename, downloadedFiles, totalFiles);
		}

		/// <summary>
		/// Gets the progress bar message.
		/// </summary>
		/// <returns>The progress bar message.</returns>
		/// <param name="filename">Filename.</param>
		/// <param name="downloadedBytes">Downloaded bytes.</param>
		/// <param name="totalBytes">Total bytes.</param>
		protected virtual string GetDownloadProgressBarMessage(string filename, long downloadedBytes, long totalBytes)
		{
			return LocalizationCatalog.GetString("Downloading {0}: {1} out of {2}", filename, downloadedBytes, totalBytes);
		}
	}
}
