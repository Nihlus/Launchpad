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
		/// Determines whether this instance can connect to the Patch server. Run as little as possible, since it blocks the main thread while checking.
		/// </summary>
		/// <returns><c>true</c> if this instance can connect to the Patch server; otherwise, <c>false</c>.</returns>
		public bool CanConnectToPatchServer()
		{
			bool bCanConnectToPatch;

			string PatchURL = Config.GetPatchUrl() + "/IcanHazPatch.html";
			string PatchUserName = Config.GetPatchUsername();
			string PatchPassword = Config.GetPatchPassword();

			try
			{

                WebRequest plainRequest = WebRequest.Create(PatchURL);

                plainRequest.Credentials = new NetworkCredential(PatchUserName, PatchPassword);
                if (Config.bUseHTTP())
                {
                    plainRequest.Method = "HEAD";
                }
                else
                {
                    plainRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                }
				plainRequest.Timeout = 8000;

				try
				{
					WebResponse response = plainRequest.GetResponse();

					plainRequest.Abort();
					response.Close();

					bCanConnectToPatch = true;
				}


                catch (WebException wex)
				{
                    Console.WriteLine("WebException in CanConnectToPatch(): " + wex.Message);
                    Console.WriteLine(PatchURL);

					plainRequest.Abort();
					bCanConnectToPatch = false;
				}
			}
			catch (WebException wex)
			{
				//case where Patch URL in config is not valid
				Console.WriteLine ("WebException CanConnectToPatch() (Invalid URL): " + wex.Message);

				bCanConnectToPatch = false;
				return bCanConnectToPatch;
			}

			if (!bCanConnectToPatch)
			{
				Console.WriteLine("Failed to connect to Patch server at: {0}", Config.GetBasePatchUrl());
				bCanConnectToPatch = false;
			}

			return bCanConnectToPatch;
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
			ProtocolHandler Patch = new ProtocolHandler ( Config.bUseHTTP() );
			try
			{
				Version local = Config.GetLocalGameVersion();
				Version remote = Patch.GetRemoteGameVersion(true);

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
            return false;
			ProtocolHandler Patch = new ProtocolHandler( Config.bUseHTTP() );
			try
			{
				Version local = Config.GetLocalLauncherVersion ();
				Version remote = Patch.GetRemoteLauncherVersion ();	

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
		public bool IsManifestOutdated()
		{
			if (File.Exists(ConfigHandler.GetManifestPath()))
			{
				ProtocolHandler Patch = new ProtocolHandler ( Config.bUseHTTP() );

				string manifestURL = Config.GetManifestURL ();
				string remoteHash = Patch.ReadPatchFile (manifestURL);
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
			ProtocolHandler Patch = new ProtocolHandler ( Config.bUseHTTP() );

			string remote = String.Format ("{0}/game/{1}/.provides",
			                                        Config.GetPatchUrl(),
			                                        Platform.ToString());

			return Patch.DoesFileExist (remote);
			
		}
	}
}

