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
using Launchpad.Launcher.Utility.Events;

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
	internal abstract class PatchProtocolHandler
	{
		public PatchProtocolHandler()
		{
			ProgressArgs = new FileDownloadProgressChangedEventArgs();
			DownloadFinishedArgs = new FileDownloadFinishedEventArgs();
		}

		/// <summary>
		/// The config handler reference.
		/// </summary>
		protected ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// Occurs when file progress changed.
		/// </summary>
		public event FileProgressChangedEventHandler FileProgressChanged;
		/// <summary>
		/// Occurs when file download finished.
		/// </summary>
		public event FileDownloadFinishedEventHandler FileDownloadFinished;

		/// <summary>
		/// The progress arguments object. Is updated during file download operations.
		/// </summary>
		protected FileDownloadProgressChangedEventArgs ProgressArgs;

		/// <summary>
		/// The download finished arguments object. Is updated once a file download finishes.
		/// </summary>
		protected FileDownloadFinishedEventArgs DownloadFinishedArgs;

		/// <summary>
		/// Checks whether or not the game has a new patch available.
		/// </summary>
		/// <returns><c>true</c>, if there's a patch available, <c>false</c> otherwise.</returns>
		public abstract bool GameHasNewPatch();

		/// <summary>
		/// Checks whether or not the launcher has a new patch available.
		/// </summary>
		/// <returns><c>true</c>, if there's a patch available, <c>false</c> otherwise.</returns>
		public abstract bool LauncherHasNewPatch();

		/// <summary>
		/// Downloads the latest version of the game.
		/// </summary>
		public abstract void DownloadGame();

		/// <summary>
		/// Downloads the latest version of the launcher.
		/// </summary>
		public abstract void DownloadLauncher();

		/// <summary>
		/// Raises the progress changed event.
		/// </summary>
		protected void OnFileProgressChanged()
		{
			if (FileProgressChanged != null)
			{
				FileProgressChanged(this, ProgressArgs);
			}
		}

		/// <summary>
		/// Raises the download finished event.
		/// </summary>
		protected void OnFileDownloadFinished()
		{
			if (FileDownloadFinished != null)
			{
				FileDownloadFinished(this, DownloadFinishedArgs);
			}
		}
	}
}

