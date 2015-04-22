using System;
using System.IO;
using System.Net;

namespace Launchpad
{
	/// <summary>
	/// This class handles all the launcher's checks, returning bools for each function.
	/// Since this class is meant to be used in both the Forms UI and the GTK UI, 
	/// there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class ChecksHandler
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		private ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.ChecksHandler"/> class.
		/// </summary>
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
				FtpWebRequest plainRequest = (FtpWebRequest)FtpWebRequest.Create(FTPURL);
				plainRequest.Credentials = new NetworkCredential(FTPUserName, FTPPassword);
				plainRequest.Method = WebRequestMethods.Ftp.ListDirectory;
				plainRequest.Timeout = 8000;

				try
				{
					WebResponse response = plainRequest.GetResponse();

					plainRequest.Abort();
					response.Close();

					bCanConnectToFTP = true;
				}
				catch (WebException wex)
				{
                    Console.WriteLine("CanConnectToFTP(): " + wex.Message);

					plainRequest.Abort();
					bCanConnectToFTP = false;
				}
			}
			catch (WebException wex)
			{
				//case where FTP URL in config is not valid
				Console.WriteLine ("CanConnectToFTP(): " + wex.Message);

				bCanConnectToFTP = false;
				return bCanConnectToFTP;
			}

			if (!bCanConnectToFTP)
			{
				Console.WriteLine("Failed to connect to FTP server at: {0}", Config.GetBaseFTPUrl());
				bCanConnectToFTP = false;
			}

			return bCanConnectToFTP;
		}

		/// <summary>
		/// Determines whether this is the first time the launcher starts.
		/// </summary>
		/// <returns><c>true</c> if this is the first time; otherwise, <c>false</c>.</returns>
		public static bool IsInitialStartup()
		{
			//we use an empty file to determine if this is the first launch or not
			if (!File.Exists(ConfigHandler.GetUpdateCookiePath()))
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
		/// Determines whether this instance is running on Unix.
		/// </summary>
		/// <returns><c>true</c> if this instance is running on unix; otherwise, <c>false</c>.</returns>
		public static bool IsRunningOnUnix()
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
			bool bHasDirectory = Directory.Exists(Config.GetGamePath(true));
			//Is there an .install file in the directory?
			bool bHasInstallationCookie = File.Exists(ConfigHandler.GetInstallCookiePath());
			//is there a version file?
			bool bHasGameVersion = File.Exists (Config.GetGameVersionPath ());

			Console.WriteLine (String.Format ("{0} {1} {2}", 
			              bHasDirectory.ToString (), 
			              bHasInstallationCookie.ToString (),
			              IsInstallCookieEmpty ().ToString ()));

			//If any of these criteria are false, the game is not considered fully installed.
			return bHasDirectory && bHasInstallationCookie && IsInstallCookieEmpty() && bHasGameVersion;
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
				Version local = Config.GetLocalGameVersion();
				Version remote = FTP.GetRemoteGameVersion(true);

				if (local < remote)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (WebException wex)
			{
				Console.WriteLine ("IsGameOutdated(): " + wex.Message);
				return true;
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
				Version local = Config.GetLocalLauncherVersion ();
				Version remote = FTP.GetRemoteLauncherVersion ();	

				if (local < remote)
				{
					return true;
				} 
				else
				{
					return false;
				}
			} 
			catch (WebException wex)
			{
				Console.WriteLine ("IsLauncherOutdated(): " + wex.Message);
				return false;	
			}
		}

		/// <summary>
		/// Determines whether the install cookie is empty
		/// </summary>
		/// <returns><c>true</c> if the install cookie is empty, otherwise, <c>false</c>.</returns>
		public static bool IsInstallCookieEmpty()
		{
			//Is there an .install file in the directory?
			bool bHasInstallationCookie = File.Exists(ConfigHandler.GetInstallCookiePath());
			//Is the .install file empty? Assume false.
			bool bIsInstallCookieEmpty = false;

			if (bHasInstallationCookie)
			{
				bIsInstallCookieEmpty = String.IsNullOrEmpty(File.ReadAllText(ConfigHandler.GetInstallCookiePath()));
			}

			return bIsInstallCookieEmpty;
		}

		/// <summary>
		/// Determines whether the  manifest is outdated.
		/// </summary>
		/// <returns><c>true</c> if the manifest is outdated; otherwise, <c>false</c>.</returns>
		public bool IsManifestOutdated()
		{
			if (File.Exists(ConfigHandler.GetManifestPath()))
			{
				FTPHandler FTP = new FTPHandler ();

				string manifestURL = Config.GetManifestURL ();
				string remoteHash = FTP.ReadFTPFile (manifestURL);
                string localHash = MD5Handler.GetFileHash(File.OpenRead(ConfigHandler.GetManifestPath()));

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

		public bool DoesServerProvidePlatform(ESystemTarget Platform)
		{
			FTPHandler FTP = new FTPHandler ();

			string remote = String.Format ("{0}/game/{1}/.provides",
			                                        Config.GetFTPUrl(),
			                                        Platform.ToString());

			return FTP.DoesFileExist (remote);
			
		}
	}
}

