//
//  Utilities.cs
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
using log4net;

namespace Launchpad.Launcher.Utility
{
	internal static class Utilities
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(Utilities));
		/// <summary>
		/// Sanitizes the input string, removing any \n, \r, or \0 characters.
		/// </summary>
		/// <param name="input">Input string.</param>
		public static string SanitizeString(string input)
		{
			return input.Replace("\n", string.Empty).Replace("\0", string.Empty).Replace("\r", string.Empty);
		}

		public static ESystemTarget ParseSystemTarget(string input)
		{
			ESystemTarget systemTarget = ESystemTarget.Unknown;

			try
			{
				systemTarget = (ESystemTarget)Enum.Parse(typeof(ESystemTarget), input);
			}
			catch (ArgumentNullException anex)
			{
				Log.Warn("Failed to parse the system target from the input string (ArgumentNullException): " + anex.Message +
					"\n\tInput: null");
			}
			catch (ArgumentException aex)
			{
				Log.Warn("Failed to parse the system target from the input string (ArgumentException): " + aex.Message +
					"\n\tInput: " + input);
			}
			catch (OverflowException oex)
			{
				Log.Warn("Failed to parse the system target from the input string (OverflowException): " + oex.Message +
					"\n\tInput: " + input);
			}

			return systemTarget;
		}
	}
}

