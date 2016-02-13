using System;
using System.IO;
using System.Net;

namespace Launchpad.Launcher
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
		/// Determines whether this instance can connect to the HTTP server. Run as little as possible, since it blocks the main thread while checking.
		/// </summary>
		/// <returns><c>true</c> if this instance can connect to the HTTP server; otherwise, <c>false</c>.</returns>
		public bool CanConnectToHTTP()
		{
			bool bCanConnectToHTTP;

			string HTTPURL = Config.GetHTTPUrl() + "/IcanHazPatch.html";
			string HTTPUserName = Config.GetHTTPUsername();
			string HTTPPassword = Config.GetHTTPPassword();

			try
			{
				HttpWebRequest plainRequest = (HttpWebRequest)WebRequest.Create(HTTPURL);
				plainRequest.Credentials = new NetworkCredential(HTTPUserName, HTTPPassword);
                plainRequest.Method = "HEAD";
				plainRequest.Timeout = 8000;

				try
				{
					WebResponse response = plainRequest.GetResponse();

					plainRequest.Abort();
					response.Close();

					bCanConnectToHTTP = true;
				}


                catch (WebException wex)
				{
                    Console.WriteLine("WebException in CanConnectToHTTP(): " + wex.Message);
                    Console.WriteLine(HTTPURL);

					plainRequest.Abort();
					bCanConnectToHTTP = false;
				}
			}
			catch (WebException wex)
			{
				//case where HTTP URL in config is not valid
				Console.WriteLine ("WebException CanConnectToHTTP() (Invalid URL): " + wex.Message);

				bCanConnectToHTTP = false;
				return bCanConnectToHTTP;
			}

			if (!bCanConnectToHTTP)
			{
				Console.WriteLine("Failed to connect to HTTP server at: {0}", Config.GetBaseHTTPUrl());
				bCanConnectToHTTP = false;
			}

			return bCanConnectToHTTP;
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
				return true;
			}
			else
			{
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

			//If any of these criteria are false, the game is not considered fully installed.
			return bHasDirectory && bHasInstallationCookie && IsInstallCookieEmpty() && bHasGameVersion;
		}

		/// <summary>
		/// Determines whether the game is outdated.
		/// </summary>
		/// <returns><c>true</c> if the game is outdated; otherwise, <c>false</c>.</returns>
		public bool IsGameOutdated()
		{
			HTTPHandler HTTP = new HTTPHandler ();
			try
			{
				Version local = Config.GetLocalGameVersion();
				Version remote = HTTP.GetRemoteGameVersion(true);

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
				Console.WriteLine ("WebException in IsGameOutdated(): " + wex.Message);
				return true;
			}
		}

		/// <summary>
		/// Determines whether the launcher is outdated.
		/// </summary>
		/// <returns><c>true</c> if the launcher is outdated; otherwise, <c>false</c>.</returns>
		public bool IsLauncherOutdated()
		{
			HTTPHandler HTTP = new HTTPHandler();
			try
			{
				Version local = Config.GetLocalLauncherVersion ();
				Version remote = HTTP.GetRemoteLauncherVersion ();	

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
				Console.WriteLine ("WebException in IsLauncherOutdated(): " + wex.Message);
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
		public bool IsManifestOutdated( string WhichManifest )
		{
			if (File.Exists(ConfigHandler.GetManifestPath( WhichManifest )))
			{
				HTTPHandler HTTP = new HTTPHandler ();

				string manifestURL = Config.GetManifestURL ( WhichManifest );
				string remoteHash = HTTP.ReadHTTPFile (manifestURL);
                string localHash = MD5Handler.GetFileHash(File.OpenRead(ConfigHandler.GetManifestPath( WhichManifest )));

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
			HTTPHandler HTTP = new HTTPHandler ();

			string remote = String.Format ("{0}/game/{1}/.provides",
			                                        Config.GetHTTPUrl(),
			                                        Platform.ToString());

			return HTTP.DoesFileExist (remote);
			
		}
	}
}

