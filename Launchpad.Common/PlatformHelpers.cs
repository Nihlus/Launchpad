//
//  PlatformHelpers.cs
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
using System.Runtime.InteropServices;
using Launchpad.Common.Enums;

namespace Launchpad.Common
{
	/// <summary>
	/// Helper methods for determining the current platform.
	/// </summary>
	public static class PlatformHelpers
	{
		/// <summary>
		/// Determines whether this instance is running on Unix.
		/// </summary>
		/// <returns><c>true</c> if this instance is running on unix; otherwise, <c>false</c>.</returns>
		public static bool IsRunningOnUnix()
		{
			var currentPlatform = GetCurrentPlatform();
			return currentPlatform == ESystemTarget.Linux || currentPlatform == ESystemTarget.Mac;
		}

		/// <summary>
		/// Gets the current platform the launcher is running on.
		/// </summary>
		/// <returns>The current platform.</returns>
		public static ESystemTarget GetCurrentPlatform()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return ESystemTarget.Linux;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return ESystemTarget.Mac;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return Environment.Is64BitOperatingSystem ? ESystemTarget.Win64 : ESystemTarget.Win32;
			}

			throw new PlatformNotSupportedException();
		}
	}
}
