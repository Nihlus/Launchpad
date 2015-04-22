using System;
using Mono.Unix.Native;

namespace Launchpad
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
				return false;
			}
		}
	}
}

