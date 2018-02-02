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

using Launchpad.Common.Enums;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Services;
using NLog;
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
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Gets the config handler reference.
		/// </summary>
		protected ConfigHandler Config { get; } = ConfigHandler.Instance;

		/// <summary>
		/// Gets the configuration instance.
		/// </summary>
		protected ILaunchpadConfiguration Configuration { get; } = ConfigHandler.Instance.Configuration;

		/// <summary>
		/// Raised whenever the download progress of a module changes.
		/// </summary>
		public event EventHandler<ModuleProgressChangedArgs> ModuleDownloadProgressChanged;

		/// <summary>
		/// Raised whenever the verification progress of a module changes.
		/// </summary>
		public event EventHandler<ModuleProgressChangedArgs> ModuleVerifyProgressChanged;

		/// <summary>
		/// Raised whenever the update progress of a module changes.
		/// </summary>
		public event EventHandler<ModuleProgressChangedArgs> ModuleUpdateProgressChanged;

		/// <summary>
		/// Raised whenever the installation of a module finishes.
		/// </summary>
		public event EventHandler<EModule> ModuleInstallationFinished;

		/// <summary>
		/// Raised whenver the installation of a module fails.
		/// </summary>
		public event EventHandler<EModule> ModuleInstallationFailed;

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
		protected PatchProtocolHandler()
		{
			this.ModuleDownloadProgressArgs = new ModuleProgressChangedArgs();
			this.ModuleVerifyProgressArgs = new ModuleProgressChangedArgs();
			this.ModuleUpdateProgressArgs = new ModuleProgressChangedArgs();

			this.TagfileService = new TagfileService();
		}

		/// <summary>
		/// Determines whether this instance can provide patches. Checks for an active connection to the
		/// patch provider (file server, distributed hash tables, hyperspace compression waves etc.)
		/// </summary>
		/// <returns><c>true</c> if this instance can provide patches; otherwise, <c>false</c>.</returns>
		public abstract bool CanPatch();

		/// <summary>
		/// Determines whether the protocol can provide patches and updates for the provided platform.
		/// </summary>
		/// <param name="platform">The platform to check.</param>
		/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
		public abstract bool IsPlatformAvailable(ESystemTarget platform);

		/// <summary>
		/// Determines whether this protocol can provide access to a banner for the game.
		/// </summary>
		/// <returns><c>true</c> if this instance can provide banner; otherwise, <c>false</c>.</returns>
		public abstract bool CanProvideBanner();

		/// <summary>
		/// Gets the changelog.
		/// </summary>
		/// <returns>The changelog.</returns>
		public abstract string GetChangelogMarkup();

		/// <summary>
		/// Gets the banner.
		/// </summary>
		/// <returns>The banner.</returns>
		public abstract Image<Rgba32> GetBanner();

		/// <summary>
		/// Determines whether or not the specified module is outdated.
		/// </summary>
		/// <param name="module">The module.</param>
		/// <returns>true if the module is outdated; otherwise, false.</returns>
		public abstract bool IsModuleOutdated(EModule module);

		/// <summary>
		/// Installs the game.
		/// </summary>
		public virtual void InstallGame()
		{
			try
			{
				// Create the .install file to mark that an installation has begun.
				// If it exists, do nothing.
				this.TagfileService.CreateGameTagfile();

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

		/// <summary>
		/// Downloads the latest version of the specified module.
		/// </summary>
		/// <param name="module">The module.</param>
		protected abstract void DownloadModule(EModule module);

		/// <summary>
		/// Updates the specified module to the latest version.
		/// </summary>
		/// <param name="module">The module to update.</param>
		public abstract void UpdateModule(EModule module);

		/// <summary>
		/// Verifies and repairs the files of the specified module.
		/// </summary>
		/// <param name="module">The module.</param>
		public abstract void VerifyModule(EModule module);

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
