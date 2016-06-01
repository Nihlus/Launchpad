//
//  UnixHandler.cs
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

using Mono.Unix.Native;
using System;
using System.Runtime.Serialization;
using log4net;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// Unix-specific functionality handler.
	/// </summary>
	internal static class UnixHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(UnixHandler));

		/// <summary>
		/// Sets the execute bit on target file. Note that this replaces all previous
		/// permissions on the file, resulting in rwx-r-x-r-- permissions.
		/// </summary>
		/// <returns><c>true</c>, if operation succeeded, <c>false</c> otherwise.</returns>
		/// <param name="fileName">File name.</param>
		/// <exception cref="BitOperationException">Throws a BitOperationException if the executable bit could not be set for any reason.</exception>
		public static bool MakeExecutable(string fileName)
		{
			try
			{
				Syscall.chmod(fileName, FilePermissions.S_IRWXU | FilePermissions.S_IRGRP | FilePermissions.S_IXGRP | FilePermissions.S_IROTH);
				return true;
			}
			catch (ApplicationException aex)
			{
				Log.Error("Failed to set the execute bit on the game executable (ApplicationException): " + aex.Message);
				throw new BitOperationException("Failed to set the execute bit on " + fileName, aex);
			}
		}
	}

	/// <summary>
	/// Generic bit operation exception for Unix file permissions.
	/// </summary>
	[Serializable]
	public class BitOperationException : Exception
	{
		public BitOperationException()
		{

		}

		public BitOperationException(string message)
			: base(message)
		{

		}

		public BitOperationException(string message, Exception inner)
			: base(message, inner)
		{

		}

		protected BitOperationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{

		}
	}
}

