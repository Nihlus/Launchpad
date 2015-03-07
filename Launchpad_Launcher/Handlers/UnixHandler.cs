using System;
using Mono.Unix.Native;

namespace Launchpad_Launcher
{
	public sealed class UnixHandler
	{
		public UnixHandler ()
		{
		}

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

