//
//  ManifestBasedProtocolHandler.cs
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using log4net;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.Handlers.Protocols
{
	/// <summary>
	/// Base underlying class for protocols using a manifest.
	/// </summary>
	public abstract class ManifestBasedProtocolHandler : PatchProtocolHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(ManifestBasedProtocolHandler));

		private readonly ManifestHandler Manifest = ManifestHandler.Instance;

		public override void InstallGame()
		{
			ModuleInstallFinishedArgs.Module = EModule.Game;
			ModuleInstallFailedArgs.Module = EModule.Game;

			try
			{
				// Create the .install file to mark that an installation has begun.
				// If it exists, do nothing.
				ConfigHandler.CreateInstallCookie();

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

		public override void UpdateModule(EModule module)
		{
			List<ManifestEntry> manifest;
			List<ManifestEntry> oldManifest;
			switch (module)
			{
				case EModule.Launcher:
				{
					RefreshModuleManifest(EModule.Launcher);


					ModuleInstallFinishedArgs.Module = EModule.Launcher;
					ModuleInstallFailedArgs.Module = EModule.Launcher;
					manifest = this.Manifest.LaunchpadManifest;
					oldManifest = this.Manifest.OldLaunchpadManifest;
					break;
				}
				case EModule.Game:
				{
					RefreshModuleManifest(EModule.Game);


					ModuleInstallFinishedArgs.Module = EModule.Game;
					ModuleInstallFailedArgs.Module = EModule.Game;
					manifest = this.Manifest.GameManifest;
					oldManifest = this.Manifest.OldGameManifest;
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, null);
				}
			}

			List<ManifestEntry> filesRequiringUpdate = new List<ManifestEntry>();
			foreach (ManifestEntry fileEntry in manifest)
			{
				if (!oldManifest.Contains(fileEntry))
				{
					filesRequiringUpdate.Add(fileEntry);
				}
			}

			try
			{
				int updatedFiles = 0;
				foreach (ManifestEntry fileEntry in filesRequiringUpdate)
				{
					++updatedFiles;

					ModuleUpdateProgressArgs.IndicatorLabelMessage = GetUpdateIndicatorLabelMessage(Path.GetFileName(fileEntry.RelativePath),
						updatedFiles,
						filesRequiringUpdate.Count);
					OnModuleUpdateProgressChanged();

					DownloadManifestEntry(fileEntry, module);
				}
			}
			catch (IOException ioex)
			{
				Log.Warn($"Updating of {module} files failed (IOException): " + ioex.Message);
				OnModuleInstallationFailed();
			}

			OnModuleInstallationFinished();
		}

		public override void VerifyModule(EModule module)
		{
			List<ManifestEntry> manifest;
			List<ManifestEntry> brokenFiles = new List<ManifestEntry>();
			if (module == EModule.Game)
			{
				manifest = this.Manifest.GameManifest;
			}
			else
			{
				manifest = this.Manifest.LaunchpadManifest;
			}

			try
			{
				int verifiedFiles = 0;
				foreach (ManifestEntry fileEntry in manifest)
				{
					++verifiedFiles;

					// Prepare the progress event contents
					ModuleVerifyProgressArgs.IndicatorLabelMessage = GetVerifyIndicatorLabelMessage(verifiedFiles, Path.GetFileName(fileEntry.RelativePath), manifest.Count);
					OnModuleVerifyProgressChanged();

					if (!fileEntry.IsFileIntegrityIntact())
					{
						brokenFiles.Add(fileEntry);
						Log.Info($"File \"{Path.GetFileName(fileEntry.RelativePath)}\" failed its integrity check and was queued for redownload.");
					}
				}

				int downloadedFiles = 0;
				foreach (ManifestEntry fileEntry in brokenFiles)
				{
					++downloadedFiles;

					// Prepare the progress event contents
					ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(fileEntry.RelativePath), brokenFiles.Count);
					OnModuleDownloadProgressChanged();

					for (int i = 0; i < Config.GetFileRetries(); ++i)
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
				OnModuleInstallationFailed();
			}

			OnModuleInstallationFinished();
		}

		protected override void DownloadModule(EModule module)
		{
			List<ManifestEntry> moduleManifest;
			switch (module)
			{
				case EModule.Launcher:
				{
					RefreshModuleManifest(EModule.Launcher);


					ModuleInstallFinishedArgs.Module = EModule.Launcher;
					ModuleInstallFailedArgs.Module = EModule.Launcher;
					moduleManifest = Manifest.LaunchpadManifest;
					break;
				}
				case EModule.Game:
				{
					RefreshModuleManifest(EModule.Game);


					ModuleInstallFinishedArgs.Module = EModule.Game;
					ModuleInstallFailedArgs.Module = EModule.Game;
					moduleManifest = Manifest.GameManifest;
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, null);
				}
			}

			// In order to be able to resume downloading, we check if there is an entry
			// stored in the install cookie.

			// Attempt to parse whatever is inside the install cookie
			ManifestEntry lastDownloadedFile;
			if (ManifestEntry.TryParse(File.ReadAllText(ConfigHandler.GetInstallCookiePath()), out lastDownloadedFile))
			{
				// Loop through all the entries in the manifest until we encounter
				// an entry which matches the one in the install cookie

				foreach (ManifestEntry fileEntry in moduleManifest)
				{
					if (lastDownloadedFile == fileEntry)
					{
						// Remove all entries before the one we were last at.
						moduleManifest.RemoveRange(0, moduleManifest.IndexOf(fileEntry));
					}
				}
			}

			int downloadedFiles = 0;
			foreach (ManifestEntry fileEntry in moduleManifest)
			{
				++downloadedFiles;

				// Prepare the progress event contents
				ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(fileEntry.RelativePath), moduleManifest.Count);
				OnModuleDownloadProgressChanged();

				DownloadManifestEntry(fileEntry, module);
			}
		}

		protected abstract string ReadRemoteFile(string url, bool useAnonymousLong = false);

		protected abstract void DownloadRemoteFile(string url, string localPath, long totalSize = 0, long contentOffset = 0,
			bool useAnonymousLogin = false);

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
						local = Config.GetLocalLauncherVersion();
						remote = GetRemoteLauncherVersion();
						break;
					}
					case EModule.Game:
					{
						local = Config.GetLocalGameVersion();
						remote = GetRemoteGameVersion();
						break;
					}
					default:
					{
						throw new ArgumentOutOfRangeException(nameof(module), module, null);
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

		protected virtual void DownloadManifestEntry(ManifestEntry fileEntry, EModule module)
		{
			ModuleDownloadProgressArgs.Module = module;

			string baseRemoteURL;
			string baseLocalPath;
			switch (module)
			{
				case EModule.Launcher:
				{
					baseRemoteURL = Config.GetLauncherBinariesURL();
                 	baseLocalPath = ConfigHandler.GetTempLauncherDownloadPath();
					break;
				}
				case EModule.Game:
				{
					baseRemoteURL = Config.GetGameURL();
					baseLocalPath = Config.GetGamePath();
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, "The module passed to DownloadManifestEntry was invalid.");
				}
			}

			// Build the access strings
			string remoteURL = $"{baseRemoteURL}{fileEntry.RelativePath}";
			string localPath = $"{baseLocalPath}{Path.DirectorySeparatorChar}{fileEntry.RelativePath}";

			// Make sure we have a directory to put the file in
			if (!string.IsNullOrEmpty(localPath) &&  Path.GetDirectoryName(localPath) != null)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(localPath));
			}
			else
			{
				throw new ArgumentNullException(nameof(localPath), "The local path was null.");
			}

			// Reset the cookie
			File.WriteAllText(ConfigHandler.GetInstallCookiePath(), string.Empty);

			// Write the current file progress to the install cookie
			using (TextWriter textWriterProgress = new StreamWriter(ConfigHandler.GetInstallCookiePath()))
			{
				textWriterProgress.WriteLine(fileEntry);
				textWriterProgress.Flush();
			}

			if (File.Exists(localPath))
			{
				FileInfo fileInfo = new FileInfo(localPath);
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
					using (FileStream fs = File.OpenRead(localPath))
					{
						localHash = MD5Handler.GetStreamHash(fs);
					}

					if (localHash != fileEntry.Hash)
					{
						// If the hash doesn't match, toss it in the bin and try again.
						Log.Info($"Redownloading file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\": " +
						         $"Hash sum mismatch. Local: {localHash}, Expected: {fileEntry.Hash}");

						File.Delete(localPath);
						DownloadRemoteFile(remoteURL, localPath, fileEntry.Size);
					}
				}
			}
			else
			{
				// No file, download it
				DownloadRemoteFile(remoteURL, localPath,fileEntry.Size);
			}

			// We've finished the download, so empty the cookie
			File.WriteAllText(ConfigHandler.GetInstallCookiePath(), string.Empty);
		}

		/// <summary>
		/// Determines whether or not the local copy of the manifest for the specifed module is outdated.
		/// </summary>
		protected virtual bool IsModuleManifestOutdated(EModule module)
		{
			string manifestPath;
			switch (module)
			{
				case EModule.Launcher:
				{
					manifestPath = ManifestHandler.GetLaunchpadManifestPath();
					break;
				}
				case EModule.Game:
				{
					manifestPath = ManifestHandler.GetGameManifestPath();
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, "The module passed to RefreshModuleManifest was invalid.");
				}
			}

			if (File.Exists(manifestPath))
			{
				string remoteHash = GetRemoteModuleManifestChecksum(module);
				using (Stream file = File.OpenRead(manifestPath))
				{
					string localHash = MD5Handler.GetStreamHash(file);

					return remoteHash != localHash;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets the checksum of the manifest for the specified module.
		/// </summary>
		protected virtual string GetRemoteModuleManifestChecksum(EModule module)
		{
			string checksum;
			switch (module)
			{
				case EModule.Launcher:
				{
					checksum = ReadRemoteFile(ManifestHandler.GetLaunchpadManifestChecksumURL());
					break;
				}
				case EModule.Game:
				{
					checksum = ReadRemoteFile(Manifest.GetGameManifestChecksumURL());
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, null);
				}
			}

			return Utilities.SanitizeString(checksum);
		}

		/// <summary>
		/// Refreshes the current manifest by redownloading it, if required;
		/// </summary>
		protected virtual void RefreshModuleManifest(EModule module)
		{
			bool doesManifestExist;
			switch (module)
			{
				case EModule.Launcher:
				{
					doesManifestExist = File.Exists(ManifestHandler.GetLaunchpadManifestPath());
					break;
				}
				case EModule.Game:
				{
					doesManifestExist = File.Exists(ManifestHandler.GetGameManifestPath());
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, "The module passed to RefreshModuleManifest was invalid.");
				}
			}

			if (doesManifestExist)
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
		}

		/// <summary>
		/// Downloads the manifest for the specified module, and backs up the old copy of the manifest.
		/// </summary>
		protected virtual void DownloadModuleManifest(EModule module)
		{
			string remoteURL;
			string localPath;
			string oldLocalPath;
			switch (module)
			{
				case EModule.Launcher:
				{
					remoteURL = ManifestHandler.GetLaunchpadManifestURL();
					localPath = ManifestHandler.GetLaunchpadManifestPath();
					oldLocalPath = ManifestHandler.GetOldLaunchpadManifestPath();

					break;
				}
				case EModule.Game:
				{
					remoteURL = Manifest.GetGameManifestURL();
					localPath = ManifestHandler.GetGameManifestPath();
					oldLocalPath = ManifestHandler.GetOldGameManifestPath();

					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(module), module, "The module passed to DownloadModuleManifest was invalid.");
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
				File.Move(localPath, oldLocalPath);
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
			string remoteVersionPath = Config.GetLauncherVersionURL();

			// Config.GetDoOfficialUpdates is used here since the official update server always allows anonymous logins.
			string remoteVersion = ReadRemoteFile(remoteVersionPath, Config.GetDoOfficialUpdates());

			Version version;
			if (Version.TryParse(remoteVersion, out version))
			{
				return version;
			}
			else
			{
				Log.Warn("Failed to parse the remote launcher version. Using the default of 0.0.0 instead.");
				return new Version("0.0.0");
			}
		}

		/// <summary>
		/// Gets the remote game version.
		/// </summary>
		/// <returns>The remote game version.</returns>
		protected virtual Version GetRemoteGameVersion()
		{
			string remoteVersionPath = $"{Config.GetBaseProtocolURL()}/game/{Config.GetSystemTarget()}/bin/GameVersion.txt";
			string remoteVersion = ReadRemoteFile(remoteVersionPath);

			Version version;
			if (Version.TryParse(remoteVersion, out version))
			{
				return version;
			}
			else
			{
				Log.Warn("Failed to parse the remote game version. Using the default of 0.0.0 instead.");
				return new Version("0.0.0");
			}
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while repairing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="nFilesToVerify">N files downloaded.</param>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="totalFilesVerified">Total files to download.</param>
		protected virtual string GetVerifyIndicatorLabelMessage(int nFilesToVerify, string currentFilename, int totalFilesVerified)
		{
			return $"Verifying file {currentFilename} ({nFilesToVerify} of {totalFilesVerified})";
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
			return $"Verifying file {currentFilename} ({updatedFiles} of {totalFiles})";
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while installing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="nFilesDownloaded">N files downloaded.</param>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="totalFilesToDownload">Total files to download.</param>
		protected virtual string GetDownloadIndicatorLabelMessage(int nFilesDownloaded, string currentFilename, int totalFilesToDownload)
		{
			return $"Downloading file {currentFilename} ({nFilesDownloaded} of {totalFilesToDownload})";
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
			return $"Downloading {filename}: {downloadedBytes} out of {totalBytes} bytes";
		}
	}
}