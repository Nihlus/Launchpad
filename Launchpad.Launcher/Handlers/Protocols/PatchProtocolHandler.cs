//
//  PatchProtocolHandler.cs
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
using System.IO;
using System.Threading.Tasks;
using Launchpad.Common.Enums;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Services;
using Remora.Results;
using SixLabors.ImageSharp;

namespace Launchpad.Launcher.Handlers.Protocols
{
    /// <summary>
    /// Patch protocol handler.
    /// This class is the base class for all file transfer protocols, providing
    /// a common framework for protocols to adhere to. It abstracts away the actual
    /// functionality, and reduces the communication with other parts of the launcher
    /// down to requests in, files out.
    ///
    /// By default, the patch protocol handler does not know anything specific about
    /// the actual workings of the protocol.
    /// </summary>
    public abstract class PatchProtocolHandler
    {
        /// <summary>
        /// Gets the configuration instance.
        /// </summary>
        protected ILaunchpadConfiguration Configuration { get; }

        /// <summary>
        /// Raised whenever the download progress of a module changes.
        /// </summary>
        public event EventHandler<ModuleProgressChangedArgs>? ModuleDownloadProgressChanged;

        /// <summary>
        /// Raised whenever the verification progress of a module changes.
        /// </summary>
        public event EventHandler<ModuleProgressChangedArgs>? ModuleVerifyProgressChanged;

        /// <summary>
        /// Raised whenever the update progress of a module changes.
        /// </summary>
        public event EventHandler<ModuleProgressChangedArgs>? ModuleUpdateProgressChanged;

        /// <summary>
        /// Raised whenever the installation of a module finishes.
        /// </summary>
        public event EventHandler<EModule>? ModuleInstallationFinished;

        /// <summary>
        /// Raised whenever the installation of a module fails.
        /// </summary>
        public event EventHandler<EModule>? ModuleInstallationFailed;

        /// <summary>
        /// Gets the download progress arguments.
        /// </summary>
        protected ModuleProgressChangedArgs ModuleDownloadProgressArgs { get; }

        /// <summary>
        /// Gets the verification progress arguments.
        /// </summary>
        protected ModuleProgressChangedArgs ModuleVerifyProgressArgs { get; }

        /// <summary>
        /// Gets the update progress arguments.
        /// </summary>
        protected ModuleProgressChangedArgs ModuleUpdateProgressArgs { get; }

        /// <summary>
        /// Gets the tagfile service.
        /// </summary>
        protected TagfileService TagfileService { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatchProtocolHandler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="tagfileService">The tagfile service.</param>
        protected PatchProtocolHandler
        (
            ILaunchpadConfiguration configuration,
            TagfileService tagfileService
        )
        {
            this.Configuration = configuration;
            this.TagfileService = tagfileService;
            this.ModuleDownloadProgressArgs = new ModuleProgressChangedArgs
            (
                EModule.Game,
                string.Empty,
                string.Empty,
                0
            );

            this.ModuleVerifyProgressArgs = new ModuleProgressChangedArgs
            (
                EModule.Game,
                string.Empty,
                string.Empty,
                0
            );

            this.ModuleUpdateProgressArgs = new ModuleProgressChangedArgs
            (
                EModule.Game,
                string.Empty,
                string.Empty,
                0
            );
        }

        /// <summary>
        /// Determines whether this instance can provide patches. Checks for an active connection to the
        /// patch provider (file server, distributed hash tables, hyperspace compression waves etc.)
        /// </summary>
        /// <returns><c>true</c> if this instance can provide patches; otherwise, <c>false</c>.</returns>
        public abstract Task<RetrieveEntityResult<bool>> CanPatchAsync();

        /// <summary>
        /// Determines whether the protocol can provide patches and updates for the provided platform.
        /// </summary>
        /// <param name="platform">The platform to check.</param>
        /// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
        public abstract Task<RetrieveEntityResult<bool>> IsPlatformAvailableAsync(ESystemTarget platform);

        /// <summary>
        /// Determines whether this protocol can provide access to a banner for the game.
        /// </summary>
        /// <returns><c>true</c> if this instance can provide banner; otherwise, <c>false</c>.</returns>
        public abstract Task<RetrieveEntityResult<bool>> CanProvideBannerAsync();

        /// <summary>
        /// Gets the changelog.
        /// </summary>
        /// <returns>The changelog.</returns>
        public abstract Task<RetrieveEntityResult<string>> GetChangelogMarkupAsync();

        /// <summary>
        /// Gets the banner.
        /// </summary>
        /// <returns>The banner.</returns>
        public abstract Task<RetrieveEntityResult<Image<Rgba32>>> GetBannerAsync();

        /// <summary>
        /// Determines whether or not the specified module is outdated.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>true if the module is outdated; otherwise, false.</returns>
        public abstract Task<RetrieveEntityResult<bool>> IsModuleOutdatedAsync(EModule module);

        /// <summary>
        /// Installs the game.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual async Task<DetermineConditionResult> InstallGameAsync()
        {
            try
            {
                // Create the .install file to mark that an installation has begun.
                // If it exists, do nothing.
                this.TagfileService.CreateGameTagfile();

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
            }
            catch (IOException ioex)
            {
                return DetermineConditionResult.FromError(ioex);
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Downloads the latest version of the specified module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task<DetermineConditionResult> DownloadModuleAsync(EModule module);

        /// <summary>
        /// Updates the specified module to the latest version.
        /// </summary>
        /// <param name="module">The module to update.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task<DetermineConditionResult> UpdateModuleAsync(EModule module);

        /// <summary>
        /// Verifies and repairs the files of the specified module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task<DetermineConditionResult> VerifyModuleAsync(EModule module);

        /// <summary>
        /// Invoke the <see cref="ModuleDownloadProgressChanged"/> event.
        /// </summary>
        protected void OnModuleDownloadProgressChanged()
        {
            this.ModuleDownloadProgressChanged?.Invoke(this, this.ModuleDownloadProgressArgs);
        }

        /// <summary>
        /// Invoke the <see cref="ModuleVerifyProgressChanged"/> event.
        /// </summary>
        protected void OnModuleVerifyProgressChanged()
        {
            this.ModuleVerifyProgressChanged?.Invoke(this, this.ModuleVerifyProgressArgs);
        }

        /// <summary>
        /// Invoke the <see cref="ModuleUpdateProgressChanged"/> event.
        /// </summary>
        protected void OnModuleUpdateProgressChanged()
        {
            this.ModuleUpdateProgressChanged?.Invoke(this, this.ModuleUpdateProgressArgs);
        }

        /// <summary>
        /// Invoke the <see cref="ModuleInstallationFinished"/> event.
        /// </summary>
        /// <param name="module">The module that finished.</param>
        protected void OnModuleInstallationFinished(EModule module)
        {
            this.ModuleInstallationFinished?.Invoke(this, module);
        }

        /// <summary>
        /// Invoke the <see cref="ModuleInstallationFailed"/> event.
        /// </summary>
        /// <param name="module">The module that failed.</param>
        protected void OnModuleInstallationFailed(EModule module)
        {
            this.ModuleInstallationFailed?.Invoke(this, module);
        }
    }
}
