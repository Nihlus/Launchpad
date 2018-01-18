//
//  MD5Handler.cs
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
using System.Security.Cryptography;

namespace Launchpad.Common.Handlers
{
	/// <summary>
	/// MD5 hashing handler. Used to ensure file integrity.
	/// </summary>
	public static class MD5Handler
	{
		/// <summary>
		/// Gets the file hash from a data stream.
		/// </summary>
		/// <returns>The hash.</returns>
		/// <param name="dataStream">File stream.</param>
		public static string GetStreamHash(Stream dataStream)
		{
			using (var md5 = MD5.Create())
			{
				// Calculate the hash of the stream.
				var resultString = BitConverter.ToString(md5.ComputeHash(dataStream)).Replace("-", string.Empty);

				return resultString;
			}
		}
	}
}
