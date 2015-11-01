using System;
using System.IO;
using System.Net;

namespace Launchpad.Utilities.Handlers
{
	/// <summary>
	/// This class handles all the launcher's checks, returning bools for each function.
	/// Since this class is meant to be used in both the Forms UI and the GTK UI, 
	/// there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class ChecksHandler
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.ChecksHandler"/> class.
		/// </summary>
		public ChecksHandler ()
		{

		}

		/// <summary>
		/// Determines whether this instance is running on Unix.
		/// </summary>
		/// <returns><c>true</c> if this instance is running on unix; otherwise, <c>false</c>.</returns>
		public static bool IsRunningOnUnix ()
		{
			int p = (int)Environment.OSVersion.Platform;
			if ((p == 4) || (p == 6) || (p == 128))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}

