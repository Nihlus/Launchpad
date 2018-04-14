//
//  DirectoryHelpers.cs
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
using System.IO;
using System.Reflection;

namespace Launchpad.Utilities.Utility
{
	/// <summary>
	/// Contains helper functions for directory manipulation.
	/// </summary>
	public static class DirectoryHelpers
	{
		/// <summary>
		/// Gets the assembly-local directory, that is, the directory where the executing assembly resides.
		/// </summary>
		/// <returns>The local dir, terminated by a directory separator.</returns>
		public static string GetLocalDir()
		{
			Uri codeBaseURI = new UriBuilder(Assembly.GetExecutingAssembly().Location).Uri;

			return Path.GetDirectoryName(Uri.UnescapeDataString(codeBaseURI.AbsolutePath));
		}
	}
}
