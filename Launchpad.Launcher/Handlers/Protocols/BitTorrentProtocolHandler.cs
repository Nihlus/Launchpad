//
//  BitTorrentProtocolHandler.cs
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

// TODO: Look into MonoTorrent
using Launchpad.Launcher.Utility.Enums;
using System;
using System.Drawing;


namespace Launchpad.Launcher.Handlers.Protocols
{
	/// <summary>
	/// Bit torrent protocol handler. Downloads and patches the game
	/// and launcher using a P2P BitTorrent protocol.
	///
	/// This protocol does not use a manifest.
	/// </summary>
	internal sealed class BitTorrentProtocolHandler : PatchProtocolHandler
	{
		public override bool CanPatch()
		{
			return false;
		}

		public override bool IsPlatformAvailable(ESystemTarget platform)
		{
			return false;
		}

		public override bool CanProvideChangelog()
		{
			return false;
		}

		public override string GetChangelogSource()
		{
			return string.Empty;
		}

		public override bool CanProvideBanner()
		{
			return false;
		}

		public override Bitmap GetBanner()
		{
			return null;
		}

		public override bool IsModuleOutdated(EModule module)
		{
			throw new NotImplementedException();
		}

		public override void InstallGame()
		{
			throw new NotImplementedException();
		}

		protected override void DownloadModule(EModule module)
		{
			throw new NotImplementedException();
		}

		public override void UpdateModule(EModule module)
		{
			throw new NotImplementedException();
		}

		public override void VerifyModule(EModule module)
		{
			throw new NotImplementedException();
		}
	}
}

