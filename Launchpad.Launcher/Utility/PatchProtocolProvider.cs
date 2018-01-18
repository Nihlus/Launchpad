//
//  PatchProtocolProvider.cs
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
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Handlers.Protocols.Manifest;

namespace Launchpad.Launcher.Utility
{
	/// <summary>
	/// TODO: Temporary hack class. This is going away.
	/// </summary>
	public static class PatchProtocolProvider
	{
		/// <summary>
		/// Gets an instance of the patch protocol handler which supports the URI set in the configuration.
		/// </summary>
		/// <returns>A handler instance.</returns>
		/// <exception cref="ArgumentException">Thrown if no compatible handler is available.</exception>
		public static PatchProtocolHandler GetHandler()
		{
			var config = ConfigHandler.Instance.Configuration;
			var remoteAddress = config.RemoteAddress;

			switch (remoteAddress.Scheme.ToLowerInvariant())
			{
				case "ftp":
				{
					return new FTPProtocolHandler();
				}
				case "http":
				case "https":
				{
					return new HTTPProtocolHandler();
				}
				default:
				{
					throw new ArgumentException($"No compatible protocol handler found for a URI of the form \"{remoteAddress}\".");
				}
			}
		}
	}
}
