//
//  PatchProtocolHandler.cs
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
using Launchpad.Launcher.Utility.Enums;

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
		protected PatchProtocolHandler()
		{
			ModuleInstallFinishedArgs = new ModuleInstallationFinishedArgs();
			ModuleInstallFailedArgs = new ModuleInstallationFailedArgs();
		}

		/// <summary>
		/// The config handler reference.
		/// </summary>
		protected ConfigHandler Config = ConfigHandler._instance;

		public event ModuleDownloadProgressChangedEventHandler ModuleDownloadProgressChanged;
		public event ModuleVerifyProgressChangedEventHandler ModuleVerifyProgressChanged;
		public event ModuleUpdateProgressChangedEventHandler ModuleUpdateProgressChanged;

		public event ModuleInstallationFinishedEventHandler ModuleInstallationFinished;
		public event ModuleInstallationFailedEventHandler ModuleInstallationFailed;

		protected readonly ModuleProgressChangedArgs ModuleDownloadProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleCopyProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleVerifyProgressArgs = new ModuleProgressChangedArgs();
		protected readonly ModuleProgressChangedArgs ModuleUpdateProgressArgs = new ModuleProgressChangedArgs();

		protected ModuleInstallationFinishedArgs ModuleInstallFinishedArgs;
		protected ModuleInstallationFailedArgs ModuleInstallFailedArgs;

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
		public abstract bool IsPlatformAvailable(ESystemTarget Platform);

		/// <summary>
		/// Determines whether this protocol can provide access to a changelog.
		/// </summary>
		/// <returns><c>true</c> if this protocol can provide a changelog; otherwise, <c>false</c>.</returns>
		public abstract bool CanProvideChangelog();

		/// <summary>
		/// Gets the changelog.
		/// </summary>
		/// <returns>The changelog.</returns>
		public abstract string GetChangelog();

		/// <summary>
		/// Checks whether or not the launcher has a new patch available.
		/// </summary>
		/// <returns><c>true</c>, if there's a patch available, <c>false</c> otherwise.</returns>
		public abstract bool IsLauncherOutdated();

		/// <summary>
		/// Checks whether or not the game has a new patch available.
		/// </summary>
		/// <returns><c>true</c>, if there's a patch available, <c>false</c> otherwise.</returns>
		public abstract bool IsGameOutdated();

		/// <summary>
		/// Installs the game.
		/// </summary>
		public abstract void InstallGame();

		/// <summary>
		/// Downloads the latest version of the game.
		/// </summary>
		protected abstract void DownloadGame();

		/// <summary>
		/// Verifies and repairs the game files.
		/// </summary>
		public abstract void VerifyGame();

		/// <summary>
		/// Updates the game to the latest version.
		/// </summary>
		public abstract void UpdateGame();

		/// <summary>
		/// Downloads the latest version of the launcher.
		/// </summary>
		public abstract void DownloadLauncher();

		/// <summary>
		/// Verifies and repairs the launcher files.
		/// </summary>
		public abstract void VerifyLauncher();


		protected void OnModuleDownloadProgressChanged()
		{
			if (ModuleDownloadProgressChanged != null)
			{
				ModuleDownloadProgressChanged(this, ModuleDownloadProgressArgs);
			}
		}

		protected void OnModuleVerifyProgressChanged()
		{
			if (ModuleVerifyProgressChanged != null)
			{
				ModuleVerifyProgressChanged(this, ModuleVerifyProgressArgs);
			}
		}

		protected void OnModuleUpdateProgressChanged()
		{
			if (ModuleUpdateProgressChanged != null)
			{
				ModuleUpdateProgressChanged(this, ModuleUpdateProgressArgs);
			}
		}

		protected void OnModuleInstallationFinished()
		{
			if (ModuleInstallationFinished != null)
			{
				ModuleInstallationFinished(this, ModuleInstallFinishedArgs);
			}
		}

		protected void OnModuleInstallationFailed()
		{
			if (ModuleInstallationFailed != null)
			{
				ModuleInstallationFailed(this, ModuleInstallFailedArgs);
			}
		}
	}

	/// <summary>
	/// A list of modules that can be downloaded and reported on.
	/// </summary>
	public enum EModule : byte
	{
		Launcher,
		Game
	}

	/*
		Common events for all patching protocols
	*/
	public delegate void ModuleInstallationProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleDownloadProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleVerifyProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);
	public delegate void ModuleUpdateProgressChangedEventHandler(object sender,ModuleProgressChangedArgs e);

	public delegate void ModuleInstallationFinishedEventHandler(object sender,ModuleInstallationFinishedArgs e);
	public delegate void ModuleInstallationFailedEventHandler(object sender,ModuleInstallationFailedArgs e);

	/*
		Common arguments for all patching protocols
	*/
	public sealed class ModuleProgressChangedArgs : EventArgs
	{
		public EModule Module
		{
			get;
			set;
		}

		public string ProgressBarMessage
		{
			get;
			set;
		}

		public string IndicatorLabelMessage
		{
			get;
			set;
		}

		public double ProgressFraction
		{
			get;
			set;
		}
	}

	public sealed class ModuleInstallationFinishedArgs : EventArgs
	{
		public EModule Module
		{
			get;
			set;
		}
	}

	public sealed class ModuleInstallationFailedArgs : EventArgs
	{
		public EModule Module
		{
			get;
			set;
		}
	}
}

