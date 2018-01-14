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
using System.Drawing;
using System.IO;

using Launchpad.Common.Enums;
using log4net;

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
		private static readonly ILog Log = LogManager.GetLogger(typeof(PatchProtocolHandler));

		/// <summary>
		/// Creates a new instance of the <see cref="PatchProtocolHandler"/> class.
		/// </summary>
		protected PatchProtocolHandler()
		{
			this.ModuleInstallFinishedArgs = new ModuleInstallationFinishedArgs();
			this.ModuleInstallFailedArgs = new ModuleInstallationFailedArgs();
		}

		/// <summary>
		/// TODO: Move to constructor
		/// The config handler reference.
		/// </summary>
		protected readonly ConfigHandler Config = ConfigHandler.Instance;

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
		public event EventHandler<ModuleInstallationFinishedArgs> ModuleInstallationFinished;

		/// <summary>
		/// Raised whenver the installation of a module fails.
		/// </summary>
		public event EventHandler<ModuleInstallationFailedArgs> ModuleInstallationFailed;

		protected readonly ModuleProgressChangedArgs ModuleDownloadProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleVerifyProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleUpdateProgressArgs = new ModuleProgressChangedArgs();

		protected readonly ModuleInstallationFinishedArgs ModuleInstallFinishedArgs;
		protected readonly ModuleInstallationFailedArgs ModuleInstallFailedArgs;

		/// <summary>
		/// Determines whether this instance can provide patches. Checks for an active connection to the
		/// patch provider (file server, distributed hash tables, hyperspace compression waves etc.)
		/// </summary>
		/// <returns><c>true</c> if this instance can provide patches; otherwise, <c>false</c>.</returns>
		public abstract bool CanPatch();

		/// <summary>
		/// Determines whether the protocol can provide patches and updates for the provided platform.
		/// </summary>
		/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
		public abstract bool IsPlatformAvailable(ESystemTarget platform);

		/// <summary>
		/// Determines whether this protocol can provide access to a changelog.
		/// </summary>
		/// <returns><c>true</c> if this protocol can provide a changelog; otherwise, <c>false</c>.</returns>
		public abstract bool CanProvideChangelog();

		/// <summary>
		/// Determines whether this protocol can provide access to a banner for the game.
		/// </summary>
		/// <returns><c>true</c> if this instance can provide banner; otherwise, <c>false</c>.</returns>
		public abstract bool CanProvideBanner();

		/// <summary>
		/// Gets the changelog.
		/// </summary>
		/// <returns>The changelog.</returns>
		public abstract string GetChangelogSource();

		/// <summary>
		/// Gets the banner.
		/// </summary>
		/// <returns>The banner.</returns>
		public abstract Bitmap GetBanner();

		/// <summary>
		/// Determines whether or not the specified module is outdated.
		/// </summary>
		public abstract bool IsModuleOutdated(EModule module);

		/// <summary>
		/// Installs the game.
		/// </summary>
		public virtual void InstallGame()
		{
			this.ModuleInstallFinishedArgs.Module = EModule.Game;
			this.ModuleInstallFailedArgs.Module = EModule.Game;

			try
			{
				//create the .install file to mark that an installation has begun
				//if it exists, do nothing.
				ConfigHandler.CreateGameCookie();

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
		protected abstract void DownloadModule(EModule module);

		/// <summary>
		/// Updates the specified module to the latest version.
		/// </summary>
		/// <param name="module">The module to update.</param>
		public abstract void UpdateModule(EModule module);

		/// <summary>
		/// Verifies and repairs the files of the specified module.
		/// </summary>
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
		protected void OnModuleInstallationFinished()
		{
			this.ModuleInstallationFinished?.Invoke(this, this.ModuleInstallFinishedArgs);
		}

		/// <summary>
		/// Invoke the <see cref="ModuleInstallationFailed"/> event.
		/// </summary>
		protected void OnModuleInstallationFailed()
		{
			this.ModuleInstallationFailed?.Invoke(this, this.ModuleInstallFailedArgs);
		}
	}
}
