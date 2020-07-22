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
using System.Threading.Tasks;
using Launchpad.Common;
using Launchpad.Common.Enums;
using Launchpad.Common.Handlers;
using Launchpad.Common.Handlers.Manifest;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using Microsoft.Extensions.Logging;
using NGettext;
using Remora.Results;
using FileInfo = System.IO.FileInfo;

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
        private readonly ILogger<ManifestBasedProtocolHandler> _log;

        /// <summary>
        /// The localization catalog.
        /// </summary>
        private readonly ICatalog _localizationCatalog;

        /// <summary>
        /// The local version service.
        /// </summary>
        private readonly LocalVersionService _localVersionService;

        /// <summary>
        /// The file manifest handler. This allows access to the launcher and game file lists.
        /// </summary>
        private readonly ManifestHandler _fileManifestHandler;

        /// <summary>
        /// The directory helpers.
        /// </summary>
        private readonly DirectoryHelpers _directoryHelpers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestBasedProtocolHandler"/> class.
        /// </summary>
        /// <param name="log">The logging instance.</param>
        /// <param name="localVersionService">The local version service.</param>
        /// <param name="fileManifestHandler">The manifest handler.</param>
        /// <param name="localizationCatalog">The localization catalog.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="tagfileService">The tagfile service.</param>
        /// <param name="directoryHelpers">The directory helpers.</param>
        protected ManifestBasedProtocolHandler
        (
            ILogger<ManifestBasedProtocolHandler> log,
            LocalVersionService localVersionService,
            ManifestHandler fileManifestHandler,
            ICatalog localizationCatalog,
            ILaunchpadConfiguration configuration,
            TagfileService tagfileService,
            DirectoryHelpers directoryHelpers
        )
            : base(configuration, tagfileService)
        {
            _log = log;
            _localVersionService = localVersionService;
            _fileManifestHandler = fileManifestHandler;
            _localizationCatalog = localizationCatalog;
            _directoryHelpers = directoryHelpers;
        }

        /// <inheritdoc />
        public override async Task<DetermineConditionResult> InstallGameAsync()
        {
            // Create the .install file to mark that an installation has begun.
            // If it exists, do nothing.
            this.TagfileService.CreateGameTagfile();

            // Make sure the manifest is up to date
            var refreshResult = await RefreshModuleManifestAsync(EModule.Game);
            if (!refreshResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(refreshResult);
            }

            // Download Game
            var downloadResult = await DownloadModuleAsync(EModule.Game);
            if (!downloadResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(downloadResult);
            }

            // Verify Game
            var verifyResult = await VerifyModuleAsync(EModule.Game);
            if (!verifyResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(verifyResult);
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <inheritdoc />
        public override async Task<DetermineConditionResult> UpdateModuleAsync(EModule module)
        {
            var manifestType = module switch
            {
                EModule.Launcher => EManifestType.Launchpad,
                EModule.Game => EManifestType.Game,
                _ => throw new ArgumentOutOfRangeException()
            };

            var refreshResult = await RefreshModuleManifestAsync(module);
            if (!refreshResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(refreshResult);
            }

            var getManifest = _fileManifestHandler.GetManifest(manifestType, false);
            if (!getManifest.IsSuccess)
            {
                return DetermineConditionResult.FromError
                (
                    $"No manifest was found when updating the module \"{module}\". The server files may be inaccessible" +
                    $" or missing."
                );
            }

            var manifest = getManifest.Entity;

            var getOldManifest = _fileManifestHandler.GetManifest(manifestType, true);

            // This dictionary holds a list of new entries and their equivalents from the old manifest. It is used
            // to determine whether or not a file is partial, or merely old yet smaller.
            var oldEntriesBeingReplaced = new Dictionary<ManifestEntry, ManifestEntry>();
            var filesRequiringUpdate = new List<ManifestEntry>();
            foreach (var fileEntry in manifest)
            {
                filesRequiringUpdate.Add(fileEntry);
                if (!getOldManifest.IsSuccess)
                {
                    continue;
                }

                var oldManifest = getOldManifest.Entity;
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
                    var oldEntry = oldEntriesBeingReplaced.ContainsKey(fileEntry)
                        ? oldEntriesBeingReplaced[fileEntry]
                        : null;

                    var downloadEntry = await DownloadManifestEntryAsync
                    (
                        fileEntry,
                        module,
                        oldEntry
                    );

                    if (!downloadEntry.IsSuccess)
                    {
                        return DetermineConditionResult.FromError(downloadEntry);
                    }
                }
            }
            catch (IOException ioex)
            {
                return DetermineConditionResult.FromError(ioex);
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <inheritdoc />
        public override async Task<DetermineConditionResult> VerifyModuleAsync(EModule module)
        {
            var getManifest = _fileManifestHandler.GetManifest((EManifestType)module, false);
            if (!getManifest.IsSuccess)
            {
                return DetermineConditionResult.FromError
                (
                    $"No manifest was found when verifying the module \"{module}\". The server files may be " +
                    $"inaccessible or missing."
                );
            }

            var manifest = getManifest.Entity;

            try
            {
                var brokenFiles = new List<ManifestEntry>();

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

                    if (fileEntry.IsFileIntegrityIntact(_directoryHelpers))
                    {
                        continue;
                    }

                    brokenFiles.Add(fileEntry);
                    _log.LogInformation($"File \"{Path.GetFileName(fileEntry.RelativePath)}\" failed its integrity check and was queued for redownload.");
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

                    var retries = 0;
                    while (true)
                    {
                        if (!fileEntry.IsFileIntegrityIntact(_directoryHelpers))
                        {
                            _log.LogInformation
                            (
                                $"File \"{Path.GetFileName(fileEntry.RelativePath)}\" failed its integrity check " +
                                $"again after redownloading. ({retries} retries)"
                            );

                            var downloadEntry = await DownloadManifestEntryAsync(fileEntry, module);
                            if (downloadEntry.IsSuccess)
                            {
                                break;
                            }

                            if (retries > this.Configuration.RemoteFileDownloadRetries)
                            {
                                return DetermineConditionResult.FromError(downloadEntry);
                            }

                            ++retries;
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
                return DetermineConditionResult.FromError
                (
                    $"Verification of {module} files failed.",
                    ioex
                );
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <inheritdoc />
        protected override async Task<DetermineConditionResult> DownloadModuleAsync(EModule module)
        {
            var manifestType = module switch
            {
                EModule.Launcher => EManifestType.Launchpad,
                EModule.Game => EManifestType.Game,
                _ => throw new ArgumentOutOfRangeException()
            };

            var refreshResult = await RefreshModuleManifestAsync(module);
            if (!refreshResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(refreshResult);
            }

            var getManifest = _fileManifestHandler.GetManifest(manifestType, false);
            if (!getManifest.IsSuccess)
            {
                return DetermineConditionResult.FromError
                (
                    $"No manifest was found when installing the module \"{module}\". The server files may be " +
                    $"inaccessible or missing."
                );
            }

            var moduleManifest = getManifest.Entity;

            // In order to be able to resume downloading, we check if there is an entry
            // stored in the install cookie.

            // Attempt to parse whatever is inside the install cookie
            if (ManifestEntry.TryParse(await File.ReadAllTextAsync(_directoryHelpers.GetGameTagfilePath()), out var lastDownloadedFile))
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

                var downloadResult = await DownloadManifestEntryAsync(fileEntry, module);
                if (!downloadResult.IsSuccess)
                {
                    return DetermineConditionResult.FromError(downloadResult);
                }
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Reads the contents of a remote file as a string.
        /// </summary>
        /// <param name="url">The URL to read.</param>
        /// <param name="useAnonymousLogin">Whether or not to use anonymous credentials.</param>
        /// <returns>The contents of the file.</returns>
        protected abstract Task<RetrieveEntityResult<string>> ReadRemoteFileAsync(string url, bool useAnonymousLogin = false);

        /// <summary>
        /// Downloads the contents of the file at the specified url to the specified local path.
        /// This method supported resuming a partial file.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="localPath">The local path where the file should be saved.</param>
        /// <param name="totalSize">The expected total size of the file.</param>
        /// <param name="contentOffset">The offset into the file where reading and writing should start.</param>
        /// <param name="useAnonymousLogin">Whether or not to use anonymous credentials.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        protected abstract Task<DetermineConditionResult> DownloadRemoteFileAsync
        (
            string url,
            string localPath,
            long totalSize = 0,
            long contentOffset = 0,
            bool useAnonymousLogin = false
        );

        /// <inheritdoc />
        public override async Task<RetrieveEntityResult<bool>> IsModuleOutdatedAsync(EModule module)
        {
            try
            {
                Version local;
                Version remote;

                switch (module)
                {
                    case EModule.Launcher:
                    {
                        local = _localVersionService.GetLocalLauncherVersion();
                        var getRemote = await GetRemoteLauncherVersionAsync();
                        if (!getRemote.IsSuccess)
                        {
                            return RetrieveEntityResult<bool>.FromError(getRemote);
                        }

                        remote = getRemote.Entity;

                        break;
                    }
                    case EModule.Game:
                    {
                        local = _localVersionService.GetLocalGameVersion();
                        var getRemote = await GetRemoteGameVersionAsync();
                        if (!getRemote.IsSuccess)
                        {
                            return RetrieveEntityResult<bool>.FromError(getRemote);
                        }

                        remote = getRemote.Entity;

                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }

                return local < remote;
            }
            catch (WebException wex)
            {
                _log.LogWarning("Unable to determine whether or not the launcher was outdated (WebException): " + wex.Message);
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
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task<DetermineConditionResult> DownloadManifestEntryAsync
        (
            ManifestEntry fileEntry,
            EModule module,
            ManifestEntry? oldFileEntry = null
        )
        {
            this.ModuleDownloadProgressArgs.Module = module;

            string baseRemoteURL;
            string baseLocalPath;
            switch (module)
            {
                case EModule.Launcher:
                {
                    baseRemoteURL = _directoryHelpers.GetRemoteLauncherBinariesPath();
                    baseLocalPath = _directoryHelpers.GetTempLauncherDownloadPath();
                    break;
                }
                case EModule.Game:
                {
                    baseRemoteURL = _directoryHelpers.GetRemoteGamePath();
                    baseLocalPath = _directoryHelpers.GetLocalGameDirectory();
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            // Build the access strings
            var remoteURL = $"{baseRemoteURL}/{fileEntry.RelativePath}";
            var localPath = Path.Combine(baseLocalPath, fileEntry.RelativePath);

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
            await File.WriteAllTextAsync(_directoryHelpers.GetGameTagfilePath(), string.Empty);

            // Write the current file progress to the install cookie
            await using (TextWriter textWriterProgress = new StreamWriter(_directoryHelpers.GetGameTagfilePath()))
            {
                textWriterProgress.WriteLine(fileEntry);
                await textWriterProgress.FlushAsync();
            }

            // First, let's see if an old file exists, and is valid.
            if (oldFileEntry != null)
            {
                // Check if the file is present, the correct size, and the correct hash
                if (oldFileEntry.IsFileIntegrityIntact(_directoryHelpers))
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
                        _log.LogInformation($"Resuming interrupted file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\" at byte {fileInfo.Length}.");
                        await DownloadRemoteFileAsync(remoteURL, localPath, fileEntry.Size, fileInfo.Length);
                    }
                    else
                    {
                        // If it's larger than expected, toss it in the bin and try again.
                        _log.LogInformation($"Restarting interrupted file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\": File bigger than expected.");

                        File.Delete(localPath);
                        await DownloadRemoteFileAsync(remoteURL, localPath, fileEntry.Size);
                    }
                }
                else
                {
                    string localHash;
                    await using (var fs = File.OpenRead(localPath))
                    {
                        localHash = MD5Handler.GetStreamHash(fs);
                    }

                    if (localHash != fileEntry.Hash)
                    {
                        // If the hash doesn't match, toss it in the bin and try again.
                        _log.LogInformation
                        (
                            $"Redownloading file \"{Path.GetFileNameWithoutExtension(fileEntry.RelativePath)}\": " +
                            $"Hash sum mismatch. Local: {localHash}, Expected: {fileEntry.Hash}"
                        );

                        File.Delete(localPath);
                        await DownloadRemoteFileAsync(remoteURL, localPath, fileEntry.Size);
                    }
                }
            }
            else
            {
                // No file, download it
                await DownloadRemoteFileAsync(remoteURL, localPath, fileEntry.Size);
            }

            // We've finished the download, so empty the cookie
            await File.WriteAllTextAsync(_directoryHelpers.GetGameTagfilePath(), string.Empty);
            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether or not the local copy of the manifest for the specifed module is outdated.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>true if the manifest is outdated; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
        /// </exception>
        protected virtual async Task<RetrieveEntityResult<bool>> IsModuleManifestOutdatedAsync(EModule module)
        {
            var manifestPath = _fileManifestHandler.GetManifestPath((EManifestType)module, false);

            if (!File.Exists(manifestPath))
            {
                return true;
            }

            var getRemoteHash = await GetRemoteModuleManifestChecksumAsync(module);
            if (!getRemoteHash.IsSuccess)
            {
                return RetrieveEntityResult<bool>.FromError(getRemoteHash);
            }

            var remoteHash = getRemoteHash.Entity;

            await using var file = File.OpenRead(manifestPath);
            var localHash = MD5Handler.GetStreamHash(file);

            return remoteHash != localHash;
        }

        /// <summary>
        /// Gets the checksum of the manifest for the specified module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>The checksum.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
        /// </exception>
        protected virtual async Task<RetrieveEntityResult<string>> GetRemoteModuleManifestChecksumAsync(EModule module)
        {
            var getRemoteChecksum = await ReadRemoteFileAsync
            (
                _fileManifestHandler.GetManifestChecksumURL((EManifestType)module)
            );

            if (!getRemoteChecksum.IsSuccess)
            {
                return RetrieveEntityResult<string>.FromError(getRemoteChecksum);
            }

            var checksum = getRemoteChecksum.Entity.RemoveLineSeparatorsAndNulls();

            return checksum.RemoveLineSeparatorsAndNulls();
        }

        /// <summary>
        /// Refreshes the current manifest by redownloading it, if required.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
        /// </exception>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task<DetermineConditionResult> RefreshModuleManifestAsync(EModule module)
        {
            var manifestExists = File.Exists(_fileManifestHandler.GetManifestPath((EManifestType)module, false));
            if (manifestExists)
            {
                var getIsOutdated = await IsModuleManifestOutdatedAsync(module);
                if (!getIsOutdated.IsSuccess)
                {
                    return DetermineConditionResult.FromError(getIsOutdated);
                }

                if (getIsOutdated.Entity)
                {
                    var downloadResult = await DownloadModuleManifestAsync(module);
                    if (!downloadResult.IsSuccess)
                    {
                        return DetermineConditionResult.FromError(downloadResult);
                    }
                }
            }
            else
            {
                var downloadResult = await DownloadModuleManifestAsync(module);
                if (!downloadResult.IsSuccess)
                {
                    return DetermineConditionResult.FromError(downloadResult);
                }
            }

            // Now update the handler instance
            _fileManifestHandler.ReloadManifests((EManifestType)module);
            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Downloads the manifest for the specified module, and backs up the old copy of the manifest.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Will be thrown if the <see cref="EModule"/> passed to the function is not a valid value.
        /// </exception>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task<DetermineConditionResult> DownloadModuleManifestAsync(EModule module)
        {
            var remoteURL = _fileManifestHandler.GetManifestURL((EManifestType)module);
            var localPath = _fileManifestHandler.GetManifestPath((EManifestType)module, false);
            var oldLocalPath = _fileManifestHandler.GetManifestPath((EManifestType)module, true);

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
                return DetermineConditionResult.FromError(ioex);
            }

            var downloadResult = await DownloadRemoteFileAsync(remoteURL, localPath);
            if (!downloadResult.IsSuccess)
            {
                return DetermineConditionResult.FromError(downloadResult);
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Gets the remote launcher version.
        /// </summary>
        /// <returns>The remote launcher version.</returns>
        protected virtual async Task<RetrieveEntityResult<Version>> GetRemoteLauncherVersionAsync()
        {
            var remoteVersionPath = _directoryHelpers.GetRemoteLauncherVersionPath();
            var readFile = await ReadRemoteFileAsync(remoteVersionPath);
            if (!readFile.IsSuccess)
            {
                return RetrieveEntityResult<Version>.FromError(readFile);
            }

            var remoteVersion = readFile.Entity.RemoveLineSeparatorsAndNulls();

            if (Version.TryParse(remoteVersion, out var version) || version is null)
            {
                return RetrieveEntityResult<Version>.FromError("Failed to parse the remote launcher version.");
            }

            return version;
        }

        /// <summary>
        /// Gets the remote game version.
        /// </summary>
        /// <returns>The remote game version.</returns>
        protected virtual async Task<RetrieveEntityResult<Version>> GetRemoteGameVersionAsync()
        {
            var remoteVersionPath = $"{this.Configuration.RemoteAddress}/game/{this.Configuration.SystemTarget}/bin/GameVersion.txt";
            var readFile = await ReadRemoteFileAsync(remoteVersionPath);
            if (!readFile.IsSuccess)
            {
                return RetrieveEntityResult<Version>.FromError(readFile);
            }

            var remoteVersion = readFile.Entity.RemoveLineSeparatorsAndNulls();

            if (!Version.TryParse(remoteVersion, out var version))
            {
                return RetrieveEntityResult<Version>.FromError("Failed to parse the remote game version.");
            }

            return version;
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
            return _localizationCatalog.GetString("Verifying file {0} ({1} of {2})", currentFilename, verifiedFiles, totalFiles);
        }

        /// <summary>
        /// Gets the indicator label message to display to the user while repairing.
        /// </summary>
        /// <returns>The indicator label message.</returns>
        /// <param name="currentFilename">Current filename.</param>
        /// <param name="updatedFiles">Number of files that have been updated.</param>
        /// <param name="totalFiles">Total files that are to be updated.</param>
        protected virtual string GetUpdateIndicatorLabelMessage(string currentFilename, int updatedFiles, int totalFiles)
        {
            return _localizationCatalog.GetString("Updating file {0} ({1} of {2})", currentFilename, updatedFiles, totalFiles);
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
            return _localizationCatalog.GetString("Downloading file {0} ({1} of {2})", currentFilename, downloadedFiles, totalFiles);
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
            return _localizationCatalog.GetString("Downloading {0}: {1} out of {2}", filename, downloadedBytes, totalBytes);
        }
    }
}
