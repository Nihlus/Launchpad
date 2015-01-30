using System;
using System.IO;
using System.Net;

/*
 * This class handles all the launcher's checks, returning bools for each function.
 * Since this class is mean to be used in both the Forms UI and the GTK UI, 
 * there must be no useage of UI code in this class. Keep it clean!
 * 
 */

namespace Launchpad_Launcher
{
	public class ChecksHandler
	{
		ConfigHandler Config = new ConfigHandler();
		public ChecksHandler ()
		{
		}


		/// <summary>
		/// Determines whether this instance can connect to the FTP server. Run as little as possible, since it blocks the main while checking.
		/// </summary>
		/// <returns><c>true</c> if this instance can connect to the FTP server; otherwise, <c>false</c>.</returns>
		public bool CanConnectToFTP()
		{
			bool bCanConnectToFTP;
			Console.WriteLine("\nChecking for FTP connection...");

			string FTPURL = Config.GetFTPUrl();
			string FTPUserName = Config.GetFTPUsername();
			string FTPPassword = Config.GetFTPPassword();

			try
			{
				FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(FTPURL);
				requestDir.Credentials = new NetworkCredential(FTPUserName, FTPPassword);
				requestDir.Method = WebRequestMethods.Ftp.ListDirectory;

				try
				{
					WebResponse response = requestDir.GetResponse();

					Console.WriteLine("Can connect to FTP at: {0} username: {1} password: {2}", FTPURL, FTPUserName, FTPPassword);
					requestDir.Abort();//important otherwise FTP remains open and further attemps to access it hang
					response.Close();

					bCanConnectToFTP = true;
				}
				catch
				{
					requestDir.Abort();
					bCanConnectToFTP = false;
				}
			}
			catch
			{
				//case where ftp url in config is not valid
				Console.WriteLine ("Failed to connect to FTP server. It seems like the specified URL was invalid - please check the configuration.");;

				bCanConnectToFTP = false;
				return bCanConnectToFTP;
			}

			if (!bCanConnectToFTP)
			{
				Console.WriteLine("Failed to connect to FTP server at: {0} username: {1} password: {2}", FTPURL, FTPUserName, FTPPassword);
				bCanConnectToFTP = false;
			}

			return bCanConnectToFTP;
		}

		/// <summary>
		/// Determines whether this is the first time the launcher starts.
		/// </summary>
		/// <returns><c>true</c> if this is the first time; otherwise, <c>false</c>.</returns>
		public bool IsInitialStartup()
		{
			Console.WriteLine("DoInitialSetupCheck()");

			//we use an empty file to determine if this is the first launch or not
			if (!File.Exists(Config.GetUpdateCookie()))
			{
				Console.WriteLine ("First time starting launcher.");
				return true;
			}
			else
			{
				Console.WriteLine("Initial setup already complete.");
				return false;
			}
		}
	}
}

