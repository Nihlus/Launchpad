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

		private readonly ManifestHandler manifestHandler = new ManifestHandler();

		public override void InstallGame()
		{
			ModuleInstallFinishedArgs.Module = EModule.Game;
			ModuleInstallFailedArgs.Module = EModule.Game;

			try
			{
				//create the .install file to mark that an installation has begun
				//if it exists, do nothing.
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

		public override void UpdateModule(EModule Module)
		{
			List<ManifestEntry> Manifest;
			List<ManifestEntry> OldManifest;
			switch (Module)
			{
				case EModule.Launcher:
				{
					RefreshModuleManifest(EModule.Launcher);


					ModuleInstallFinishedArgs.Module = EModule.Launcher;
					ModuleInstallFailedArgs.Module = EModule.Launcher;
					Manifest = manifestHandler.LaunchpadManifest;
					OldManifest = manifestHandler.OldLaunchpadManifest;
					break;
				}
				case EModule.Game:
				{
					RefreshModuleManifest(EModule.Game);


					ModuleInstallFinishedArgs.Module = EModule.Game;
					ModuleInstallFailedArgs.Module = EModule.Game;
					Manifest = manifestHandler.GameManifest;
					OldManifest = manifestHandler.OldGameManifest;
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(Module), Module, null);
				}
			}

			List<ManifestEntry> FilesRequiringUpdate = new List<ManifestEntry>();
			foreach (ManifestEntry Entry in Manifest)
			{
				if (!OldManifest.Contains(Entry))
				{
					FilesRequiringUpdate.Add(Entry);
				}
			}

			try
			{
				int updatedFiles = 0;
				foreach (ManifestEntry Entry in FilesRequiringUpdate)
				{
					++updatedFiles;

					ModuleUpdateProgressArgs.IndicatorLabelMessage = GetUpdateIndicatorLabelMessage(Path.GetFileName(Entry.RelativePath),
						updatedFiles,
						FilesRequiringUpdate.Count);
					OnModuleUpdateProgressChanged();

					DownloadManifestEntry(Entry, Module);
				}
			}
			catch (IOException ioex)
			{
				Log.Warn($"Updating of {Module} files failed (IOException): " + ioex.Message);
				OnModuleInstallationFailed();
			}

			OnModuleInstallationFinished();
		}

		public override void VerifyModule(EModule Module)
		{
			List<ManifestEntry> Manifest;
			List<ManifestEntry> BrokenFiles = new List<ManifestEntry>();
			if (Module == EModule.Game)
			{
				Manifest = manifestHandler.GameManifest;
			}
			else
			{
				Manifest = manifestHandler.LaunchpadManifest;
			}

			try
			{
				int verifiedFiles = 0;
				foreach (ManifestEntry Entry in Manifest)
				{
					++verifiedFiles;

					// Prepare the progress event contents
					ModuleVerifyProgressArgs.IndicatorLabelMessage = GetVerifyIndicatorLabelMessage(verifiedFiles, Path.GetFileName(Entry.RelativePath), Manifest.Count);
					OnModuleVerifyProgressChanged();

					if (!Entry.IsFileIntegrityIntact())
					{
						BrokenFiles.Add(Entry);
						Log.Info($"File \"{Path.GetFileName(Entry.RelativePath)}\" failed its integrity check and was queued for redownload.");
					}
				}

				int downloadedFiles = 0;
				foreach (ManifestEntry Entry in BrokenFiles)
				{
					++downloadedFiles;

					// Prepare the progress event contents
					ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(Entry.RelativePath), BrokenFiles.Count);
					OnModuleDownloadProgressChanged();

					for (int i = 0; i < Config.GetFileRetries(); ++i)
					{
						if (!Entry.IsFileIntegrityIntact())
						{
							DownloadManifestEntry(Entry, Module);
							Log.Info($"File \"{Path.GetFileName(Entry.RelativePath)}\" failed its integrity check again after redownloading. ({i} retries)");
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
				Log.Warn($"Verification of {Module} files failed (IOException): " + ioex.Message);
				OnModuleInstallationFailed();
			}

			OnModuleInstallationFinished();
		}

		protected override void DownloadModule(EModule Module)
		{
			List<ManifestEntry> Manifest;
			switch (Module)
			{
				case EModule.Launcher:
				{
					RefreshModuleManifest(EModule.Launcher);


					ModuleInstallFinishedArgs.Module = EModule.Launcher;
					ModuleInstallFailedArgs.Module = EModule.Launcher;
					Manifest = manifestHandler.LaunchpadManifest;
					break;
				}
				case EModule.Game:
				{
					RefreshModuleManifest(EModule.Game);


					ModuleInstallFinishedArgs.Module = EModule.Game;
					ModuleInstallFailedArgs.Module = EModule.Game;
					Manifest = manifestHandler.GameManifest;
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(Module), Module, null);
				}
			}

			//in order to be able to resume downloading, we check if there is an entry
			//stored in the install cookie.
			//attempt to parse whatever is inside the install cookie
			ManifestEntry lastDownloadedFile;
			if (ManifestEntry.TryParse(File.ReadAllText(ConfigHandler.GetInstallCookiePath()), out lastDownloadedFile))
			{
				//loop through all the entries in the manifest until we encounter
				//an entry which matches the one in the install cookie

				foreach (ManifestEntry Entry in Manifest)
				{
					if (lastDownloadedFile == Entry)
					{
						//remove all entries before the one we were last at.
						Manifest.RemoveRange(0, Manifest.IndexOf(Entry));
					}
				}
			}

			int downloadedFiles = 0;
			foreach (ManifestEntry Entry in Manifest)
			{
				++downloadedFiles;

				// Prepare the progress event contents
				ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(Entry.RelativePath), Manifest.Count);
				OnModuleDownloadProgressChanged();

				DownloadManifestEntry(Entry, Module);
			}
		}

		protected abstract string ReadRemoteFile(string URL, bool useAnonymousLong = false);

		protected abstract void DownloadRemoteFile(string URL, string localPath, long totalSize = 0, long contentOffset = 0,
			bool useAnonymousLogin = false);

		public override bool IsModuleOutdated(EModule Module)
		{
			try
			{
				Version local;
				Version remote;

				switch (Module)
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
						throw new ArgumentOutOfRangeException(nameof(Module), Module, null);
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

		protected virtual void DownloadManifestEntry(ManifestEntry Entry, EModule Module)
		{
			ModuleDownloadProgressArgs.Module = Module;

			string baseRemoteURL;
			string baseLocalPath;
			switch (Module)
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
					throw new ArgumentOutOfRangeException(nameof(Module), Module, "The module passed to DownloadManifestEntry was invalid.");
				}
			}

			// Build the access strings
			string remoteURL = $"{baseRemoteURL}{Entry.RelativePath}";
			string localPath = $"{baseLocalPath}{Path.DirectorySeparatorChar}{Entry.RelativePath}";

			// Make sure we have a directory to put the file in
			if (Path.GetDirectoryName(localPath) != null)
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
				textWriterProgress.WriteLine(Entry);
				textWriterProgress.Flush();
			}

			if (File.Exists(localPath))
			{
				FileInfo fileInfo = new FileInfo(localPath);
				if (fileInfo.Length != Entry.Size)
				{
					// If the file is partial, resume the download.
					if (fileInfo.Length < Entry.Size)
					{
						Log.Info($"Resuming interrupted file \"{Path.GetFileNameWithoutExtension(Entry.RelativePath)}\" at byte {fileInfo.Length}.");
						DownloadRemoteFile(remoteURL, localPath, Entry.Size, fileInfo.Length);
					}
					else
					{
						// If it's larger than expected, toss it in the bin and try again.
						Log.Info($"Restarting interrupted file \"{Path.GetFileNameWithoutExtension(Entry.RelativePath)}\": File bigger than expected.");

						File.Delete(localPath);
						DownloadRemoteFile(remoteURL, localPath, Entry.Size);
					}
				}
				else
				{
					string localHash;
					using (FileStream fs = File.OpenRead(localPath))
					{
						localHash = MD5Handler.GetStreamHash(fs);
					}

					if (localHash != Entry.Hash)
					{
						// If the hash doesn't match, toss it in the bin and try again.
						Log.Info($"Redownloading file \"{Path.GetFileNameWithoutExtension(Entry.RelativePath)}\": " +
						         $"Hash sum mismatch. Local: {localHash}, Expected: {Entry.Hash}");

						File.Delete(localPath);
						DownloadRemoteFile(remoteURL, localPath, Entry.Size);
					}
				}
			}
			else
			{
				//no file, download it
				DownloadRemoteFile(remoteURL, localPath,Entry.Size);
			}

			if (ChecksHandler.IsRunningOnUnix())
			{
				//if we're dealing with a file that should be executable,
				string gameName = Config.GetGameName();
				bool bFileIsGameExecutable = (Path.GetFileName(localPath).EndsWith(".exe")) || (Path.GetFileName(localPath) == gameName);
				if (bFileIsGameExecutable)
				{
					//set the execute bits
					UnixHandler.MakeExecutable(localPath);
				}
			}

			// We've finished the download, so empty the cookie
			File.WriteAllText(ConfigHandler.GetInstallCookiePath(), string.Empty);
		}

		/// <summary>
		/// Determines whether or not the local copy of the manifest for the specifed module is outdated.
		/// </summary>
		protected virtual bool IsModuleManifestOutdated(EModule Module)
		{
			string manifestPath;
			switch (Module)
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
					throw new ArgumentOutOfRangeException(nameof(Module), Module, "The module passed to RefreshModuleManifest was invalid.");
				}
			}

			if (File.Exists(manifestPath))
			{
				string remoteHash = GetRemoteModuleManifestChecksum(Module);
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
		protected virtual string GetRemoteModuleManifestChecksum(EModule Module)
		{
			string checksum;
			switch (Module)
			{
				case EModule.Launcher:
				{
					checksum = ReadRemoteFile(manifestHandler.GetLaunchpadManifestChecksumURL());
					break;
				}
				case EModule.Game:
				{
					checksum = ReadRemoteFile(manifestHandler.GetGameManifestChecksumURL());
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(Module), Module, null);
				}
			}

			return Utilities.Clean(checksum);
		}

		/// <summary>
		/// Refreshes the current manifest by redownloading it, if required;
		/// </summary>
		protected virtual void RefreshModuleManifest(EModule Module)
		{
			bool doesManifestExist;
			switch (Module)
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
					throw new ArgumentOutOfRangeException(nameof(Module), Module, "The module passed to RefreshModuleManifest was invalid.");
				}
			}

			if (doesManifestExist)
			{
				if (IsModuleManifestOutdated(Module))
				{
					DownloadModuleManifest(Module);
				}
			}
			else
			{
				DownloadModuleManifest(Module);
			}
		}

		/// <summary>
		/// Downloads the manifest for the specified module, and backs up the old copy of the manifest.
		/// </summary>
		protected virtual void DownloadModuleManifest(EModule Module)
		{

			string RemoteURL;
			string LocalPath;
			string OldLocalPath;
			switch (Module)
			{
				case EModule.Launcher:
				{
					RemoteURL = manifestHandler.GetLaunchpadManifestURL();
					LocalPath = ManifestHandler.GetLaunchpadManifestPath();
					OldLocalPath = ManifestHandler.GetOldLaunchpadManifestPath();

					break;
				}
				case EModule.Game:
				{
					RemoteURL = manifestHandler.GetGameManifestURL();
					LocalPath = ManifestHandler.GetGameManifestPath();
					OldLocalPath = ManifestHandler.GetOldGameManifestPath();

					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(Module), Module, "The module passed to DownloadModuleManifest was invalid.");
				}
			}

			try
			{
				// Delete the old backup (if there is one)
				if (File.Exists(OldLocalPath))
				{
					File.Delete(OldLocalPath);
				}

				// Create a backup of the old manifest so that we can compare them when updating the game
				File.Move(LocalPath, OldLocalPath);
			}
			catch (IOException ioex)
			{
				Log.Warn("Failed to back up the old launcher manifest (IOException): " + ioex.Message);
			}

			DownloadRemoteFile(RemoteURL, LocalPath);
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