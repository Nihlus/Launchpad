//
//  HTTPProtocolHandler.cs
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

namespace Launchpad.Launcher.Handlers.Protocols
{
	/// <summary>
	/// HTTP protocol handler. Patches the launcher and game using the 
	/// HTTP/HTTPS protocol.
	/// 
	/// This protocol uses a manifest.
	/// </summary>
	internal sealed class HTTPProtocolHandler : PatchProtocolHandler
	{
		public HTTPProtocolHandler(bool bShouldUseHTTPS = false)
			: base()
		{
		}

		public override bool CanPatch()
		{
			return false;
		}

		public override bool IsLauncherOutdated()
		{
			return false;
		}

		public override bool IsGameOutdated()
		{
			return false;
		}

		public override void InstallLauncher()
		{

		}

		public override void InstallGame()
		{

		}

		protected override void DownloadLauncher()
		{

		}

		protected override void DownloadGame()
		{

		}

		public override void VerifyLauncher()
		{

		}

		public override void VerifyGame()
		{

		}
	}
}

