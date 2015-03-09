using System;
using Mono.Unix.Native;

namespace Launchpad_Launcher
{
	/// <summary>
	/// Unix-specific functionality handler.
	/// </summary>
	public sealed class UnixHandler
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.UnixHandler"/> class.
		/// </summary>
		public UnixHandler ()
		{
		}

		/// <summary>
		/// Sets the execute bit on target file. Note that this replaces all previous
		/// permissions on the file, resulting in RWX------ permissions.
		/// </summary>
		/// <returns><c>true</c>, if operation succeeded, <c>false</c> otherwise.</returns>
		/// <param name="fileName">File name.</param>
		public bool MakeExecutable(string fileName)
		{
			try
			{
				Syscall.chmod (fileName, FilePermissions.S_IRWXU);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine ("MakeExecutable(): " + ex.Message);
				return false;
			}
		}
	}
}

