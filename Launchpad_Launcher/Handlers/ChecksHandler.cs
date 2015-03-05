using System;
using System.IO;
using System.Net;

/*
 * This class handles all the launcher's checks, returning bools for each function.
 * Since this class is meant to be used in both the Forms UI and the GTK UI, 
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
			FTPHandler FTP = new FTPHandler();

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
				requestDir.Timeout = 20000;

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

		/// <summary>
		/// Determines whether this instance is running on unix.
		/// </summary>
		/// <returns><c>true</c> if this instance is running on unix; otherwise, <c>false</c>.</returns>
		public bool IsRunningOnUnix()
		{
			int p = (int)Environment.OSVersion.Platform;
			if ((p == 4) || (p == 6) || (p == 128))
			{
				Console.WriteLine("Running on Unix");
				return true;
			}
			else
			{
				Console.WriteLine("Not running on Unix");
				return false;
			}
		}

		/// <summary>
		/// Determines whether the game is installed.
		/// </summary>
		/// <returns><c>true</c> if the game is installed; otherwise, <c>false</c>.</returns>
		public bool IsGameInstalled()
		{
			//Criteria for considering the game 'installed'
			//Does the game directory exist?
			bool bHasDirectory = Directory.Exists(Config.GetGamePath());
			//Is there an .install file in the directory?
			bool bHasInstallationCookie = File.Exists(Config.GetInstallCookie());

			Console.WriteLine (String.Format ("{0} {1} {2}", 
			              bHasDirectory.ToString (), 
			              bHasInstallationCookie.ToString (),
			              IsInstallCookieEmpty ().ToString ()));

			//If any of these criteria are false, the game is not considered fully installed.
			return bHasDirectory && bHasInstallationCookie && IsInstallCookieEmpty();
		}

		/// <summary>
		/// Determines whether the game is outdated.
		/// </summary>
		/// <returns><c>true</c> if the game is outdated; otherwise, <c>false</c>.</returns>
		public bool IsGameOutdated()
		{
			FTPHandler FTP = new FTPHandler ();
			try
			{
				Version local = new Version(Config.GetLocalGameVersion());
				Version remote = new Version(FTP.GetRemoteGameVersion());

				if (local < remote)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine ("IsGameOutdated(): " + ex.StackTrace);
				return false;
			}
		}

		/// <summary>
		/// Determines whether the launcher is outdated.
		/// </summary>
		/// <returns><c>true</c> if the launcher is outdated; otherwise, <c>false</c>.</returns>
		public bool IsLauncherOutdated()
		{
			FTPHandler FTP = new FTPHandler();
			try
			{
				Version local = new Version(Config.GetLocalLauncherVersion());

				Version remote = new Version(FTP.GetRemoteLauncherVersion ());	

				if (local < remote)
				{
					return true;
				} 
				else
				{
					return false;
				}
			} 
			catch (Exception ex)
			{
				Console.WriteLine ("IsLauncherOutdated(): " + ex.StackTrace);
				return false;	
			}
		}

		public bool IsInstallCookieEmpty()
		{
			//Is there an .install file in the directory?
			bool bHasInstallationCookie = File.Exists(Config.GetInstallCookie());
			//Is the .install file empty? Assume false.
			bool bIsInstallCookieEmpty = false;

			if (bHasInstallationCookie)
			{
				bIsInstallCookieEmpty = (File.ReadAllText(Config.GetInstallCookie()) == "");
			}

			return bIsInstallCookieEmpty;
		}

		public bool IsManifestOutdated()
		{
			if (File.Exists(Config.GetManifestPath()))
			{
				FTPHandler FTP = new FTPHandler ();
				MD5Handler MD5 = new MD5Handler ();

				string remoteHash = FTP.ReadFTPFile (Config.GetManifestURL ());
				string localHash = MD5.GetFileHash (File.OpenRead (Config.GetManifestPath ()));

				if (remoteHash != localHash)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}
	}
}

