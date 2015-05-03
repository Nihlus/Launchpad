using Mono.Unix.Native;
using System;
using System.Runtime.Serialization;

namespace Launchpad.Launcher
{
	/// <summary>
	/// Unix-specific functionality handler.
	/// </summary>
	internal static class UnixHandler
	{
		/// <summary>
		/// Sets the execute bit on target file. Note that this replaces all previous
		/// permissions on the file, resulting in RWXRWXR-- permissions.
		/// </summary>
		/// <returns><c>true</c>, if operation succeeded, <c>false</c> otherwise.</returns>
		/// <param name="fileName">File name.</param>
		public static bool MakeExecutable(string fileName)
		{
			try
			{
				Syscall.chmod (fileName, FilePermissions.S_IRWXU | FilePermissions.S_IRWXG | FilePermissions.S_IROTH);
				return true;
			}
			catch (ApplicationException aex)
			{
				Console.WriteLine ("ApplicationException in MakeExecutable(): " + aex.Message);
				throw new BitOperationException ("Failed to set the execute bit on " + fileName, aex);
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

