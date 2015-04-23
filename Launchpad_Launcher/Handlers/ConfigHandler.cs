using IniParser;
using IniParser.Model;
using System;
using System.IO;


namespace Launchpad
{
	/// <summary>
	/// Config handler. This class handles reading and writing to the launcher's configuration.
	/// Read and write operations are synchronized by locks, so it should be threadsafe.
	/// This is a singleton class, and it should always be accessed through _Instance.
	/// </summary>
    internal sealed class ConfigHandler
    {
		/// <summary>
		/// The config lock object.
		/// </summary>
		private object ReadLock = new Object ();
		/// <summary>
		/// The write lock object.
		/// </summary>
		private object WriteLock = new Object ();

		/// <summary>
		/// The singleton Instance. Will always point to one shared object.
		/// </summary>
		public static readonly ConfigHandler _instance = new ConfigHandler ();

        /// <summary>
        /// Initializes a new instance of the <see cref="Launchpad_Launcher.ConfigHandler"/> class.
        /// </summary>
        private ConfigHandler()
        {
            
        }	

		/// <summary>
		/// Writes the config to disk. This method is thread-locking, and all write operations 
		/// are synchronized via lock(WriteLock).
		/// </summary>
		/// <param name="Parser">Parser.</param>
		/// <param name="Data">Data.</param>
		private void WriteConfig(FileIniDataParser Parser, IniData Data)
		{
			lock (WriteLock)
			{
				Parser.WriteFile (GetConfigPath (), Data);
			}
		}

		/// <summary>
		/// Gets the config path.
		/// </summary>
		/// <returns>The config path.</returns>
        private static string GetConfigPath()
        {
			string configPath = String.Format(@"{0}Config{1}LauncherConfig.ini", 
			                                  GetLocalDir(), 
			                                  Path.DirectorySeparatorChar);
            
            return configPath;
        }

		/// <summary>
		/// Gets the config dir.
		/// </summary>
		/// <returns>The config dir.</returns>
        private static string GetConfigDir()
        {
			string configDir = String.Format(@"{0}Config", GetLocalDir());
            return configDir;
        }

		/// <summary>
		/// Initializes the config by checking for bad values or files. 
		/// Run once when the launcher starts, then avoid unless absolutely neccesary.
		/// </summary>
		public void Initialize()
		{
			//Since Initialize will write to the config, we'll create the parser here and load the file later
			FileIniDataParser Parser = new FileIniDataParser();

			string configDir = GetConfigDir();
			string configPath = GetConfigPath();

			//Major release 0.1.0, linux support
			string defaultLauncherVersion = "0.1.0";

			//Check for pre-unix config. If it exists, fix the values and copy it.
			UpdateOldConfig ();

			//Check for old cookie file. If it exists, rename it.
			ReplaceOldUpdateCookie ();

			//should be safe to lock the config now for initializing it
			lock (ReadLock)
			{
				if (!Directory.Exists(configDir))
				{
					Directory.CreateDirectory(configDir);
				}
				if (!File.Exists(configPath))
				{
					//here we create a new empty file
					FileStream configStream = File.Create(configPath);
					configStream.Close();

					//read the file as an INI file
					try
					{
						IniData data = Parser.ReadFile(GetConfigPath());

						string GeneratedGUID = Guid.NewGuid ().ToString ();

						data.Sections.AddSection("Local");
						data.Sections.AddSection("Remote");
						data.Sections.AddSection("Launchpad");

						data["Local"].AddKey("LauncherVersion", defaultLauncherVersion);
						data["Local"].AddKey("GameName", "LaunchpadExample");
						data["Local"].AddKey("SystemTarget", "Win64");
						data["Local"].AddKey("GUID", GeneratedGUID);

						data["Remote"].AddKey("FTPUsername", "anonymous");
						data["Remote"].AddKey("FTPPassword", "anonymous");
						data["Remote"].AddKey("FTPUrl", "ftp://directorate.asuscomm.com");

						data["Launchpad"].AddKey("bOfficialUpdates", "true");

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine(ioex.Message);
					}

				}
				else
				{
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Local"]["LauncherVersion"] = defaultLauncherVersion;
					if (!data ["Local"].ContainsKey ("GUID"))
					{
						string GeneratedGUID = Guid.NewGuid ().ToString ();
						data ["Local"].AddKey ("GUID", GeneratedGUID);
					}

					WriteConfig (Parser, data);
				}
			}
		}

		/// <summary>
		/// Gets the update cookie.
		/// </summary>
		/// <returns>The update cookie.</returns>
        public static string GetUpdateCookiePath()
        {
			string updateCookie = String.Format(@"{0}{1}.update", 
			                                    Directory.GetCurrentDirectory(), 
			                                    Path.DirectorySeparatorChar);
            return updateCookie;
        }

		/// <summary>
		/// Creates the update cookie.
		/// </summary>
		/// <returns>The update cookie's path.</returns>
		public static string CreateUpdateCookie()
		{
			bool bCookieExists = File.Exists (String.Format (@"{0}{1}.update", 
			                                                Directory.GetCurrentDirectory (), 
			                                                Path.DirectorySeparatorChar));
			if (!bCookieExists)
			{
				File.Create (String.Format(@"{0}{1}.update", 
				                           Directory.GetCurrentDirectory(), 
				                           Path.DirectorySeparatorChar));

				return String.Format(@"{0}{1}.update", 
				                      Directory.GetCurrentDirectory(), 
				                      Path.DirectorySeparatorChar);
			}
			else
			{
				return GetUpdateCookiePath ();
			}
		}

		/// <summary>
		/// Gets the install cookie.
		/// </summary>
		/// <returns>The install cookie.</returns>
		public static string GetInstallCookiePath()
		{
			string installCookie = String.Format(@"{0}{1}.install", 
			                                    Directory.GetCurrentDirectory(), 
			                                    Path.DirectorySeparatorChar);
			return installCookie;
		}

		/// <summary>
		/// Creates the install cookie.
		/// </summary>
		/// <returns>The install cookie's path.</returns>
		public static string CreateInstallCookie()
		{
			bool bCookieExists = File.Exists (String.Format (@"{0}{1}.install", 
			                                                 Directory.GetCurrentDirectory (), 
			                                                 Path.DirectorySeparatorChar));
			if (!bCookieExists)
			{
				File.Create (String.Format(@"{0}{1}.install", 
				                           Directory.GetCurrentDirectory(), 
				                           Path.DirectorySeparatorChar));

				return String.Format(@"{0}{1}.install", 
				                     Directory.GetCurrentDirectory(), 
				                     Path.DirectorySeparatorChar);
			}
			else
			{
				return GetInstallCookiePath ();
			}
		}

		/// <summary>
		/// Gets the local dir.
		/// </summary>
		/// <returns>The local dir.</returns>
        public static string GetLocalDir()
        {
			string localDir = String.Format(@"{0}{1}", Directory.GetCurrentDirectory(), Path.DirectorySeparatorChar);
            return localDir;
        }

		/// <summary>
		/// Gets the temp dir.
		/// </summary>
		/// <returns>The temp dir.</returns>
        public static string GetTempDir()
        {
			string tempDir = Path.GetTempPath ();
            return tempDir;
        }

		/// <summary>
		/// Gets the manifest path.
		/// </summary>
		/// <returns>The manifest path.</returns>
        public static string GetManifestPath()
        {
            string manifestPath = String.Format(@"{0}LauncherManifest.txt", GetLocalDir());
            return manifestPath;
        }

		/// <summary>
		/// Gets the old manifest's path.
		/// </summary>
		/// <returns>The old manifest's path.</returns>
		public static string GetOldManifestPath()
		{
			string manifestPath = String.Format(@"{0}LauncherManifest.txt.old", GetLocalDir());
			return manifestPath;
		}

		/// <summary>
		/// Gets the game path, terminated by a separator char.
		/// </summary>
		/// <returns>The game path, terminated by a separator char.</returns>
        public string GetGamePath(bool bIncludeSystemTarget)
        {
			string gamePath = "";
			if (bIncludeSystemTarget)
			{
				gamePath = String.Format(@"{0}Game{2}{1}{2}", 
				                                GetLocalDir(),
				                                GetSystemTarget().ToString(),
				                                Path.DirectorySeparatorChar);
			}
			else
			{
				gamePath = String.Format(@"{0}Game{1}", 
				                                GetLocalDir(),
				                                Path.DirectorySeparatorChar);
			}

            return gamePath;
        }

		/// <summary>
		/// Gets the game executable.
		/// </summary>
		/// <returns>The game executable.</returns>
        public string GetGameExecutable()
        {
			string executablePathRootLevel = "";
			string executablePathTargetLevel = "";

			//unix doesn't need (or have!) the .exe extension. Just start it directly.
			if (ChecksHandler.IsRunningOnUnix())
			{
				//should return something along the lines of "./Game/<ExecutableName>"
				executablePathRootLevel = String.Format(@"{0}{1}", 
				                               GetGamePath(true), 
				                               GetGameName());

				//should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>"
				executablePathTargetLevel = String.Format(@"{0}{1}{3}Binaries{3}{2}{3}{1}", 
				                                          GetGamePath(true), 
				                                          GetGameName(), 
				                                          GetSystemTarget(),
				                                          Path.DirectorySeparatorChar);
			}
			else
			{
				//should return something along the lines of "./Game/<ExecutableName>.exe"
				executablePathRootLevel = String.Format(@"{0}{1}.exe", 
				                                        GetGamePath(true), 
				                               			GetGameName());

				//should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>.exe"
				executablePathTargetLevel = String.Format(@"{0}{1}{3}Binaries{3}{2}{3}{1}.exe", 
				                                          GetGamePath(true), 
				                                          GetGameName(), 
				                                          GetSystemTarget(),
				                                          Path.DirectorySeparatorChar);
			}


			if (File.Exists(executablePathRootLevel))
			{
				return executablePathRootLevel;
			}
			else if (File.Exists(executablePathTargetLevel))
			{
				return executablePathTargetLevel;
			}       
			else
			{
				Console.WriteLine (executablePathRootLevel);
				Console.WriteLine (executablePathTargetLevel);
				throw new FileNotFoundException ("The game executable could not be found.");
			}
        }

		/// <summary>
		/// Gets the local game version.
		/// </summary>
		/// <returns>The local game version.</returns>
		public Version GetLocalGameVersion()
		{
			string GameVersion = "";
			try
			{
				GameVersion = File.ReadAllText(GetGameVersionPath());
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("GetLocalGameVersion(): " + ioex.Message);
			}

			return Version.Parse(GameVersion);
		}

		/// <summary>
		/// Gets the game version path.
		/// </summary>
		/// <returns>The game version path.</returns>
		public string GetGameVersionPath()
		{
			string VersionPath = String.Format(@"{0}{1}GameVersion.txt",
			                                GetGamePath(true), 
			                            	Path.DirectorySeparatorChar);

			return VersionPath;
		}
		/// <summary>
		/// Gets the manifest URL.
		/// </summary>
		/// <returns>The manifest URL.</returns>
        public string GetManifestURL()
        {
            string manifestURL = String.Format("{0}/game/{1}/LauncherManifest.txt", 
                GetFTPUrl(),
                GetSystemTarget());

            return manifestURL;
        }

		/// <summary>
		/// Gets the manifest checksum URL.
		/// </summary>
		/// <returns>The manifest checksum URL.</returns>
        public string GetManifestChecksumURL()
        {
            string manifestChecksumURL = String.Format("{0}/game/{1}/LauncherManifest.checksum", 
                GetFTPUrl(), 
                GetSystemTarget());

            return manifestChecksumURL;
        }

		/// <summary>
		/// Gets the custom launcher download URL.
		/// </summary>
		/// <returns>The custom launcher download URL.</returns>
        public string GetLauncherBinaryURL()
        {
            string launcherURL = String.Format("{0}/launcher/bin/Launchpad.exe", GetFTPUrl());
            return launcherURL;
        }

		/// <summary>
		/// Gets the changelog URL.
		/// </summary>
		/// <returns>The changelog URL.</returns>
        public string GetChangelogURL()
        {
            string changelogURL = String.Format("{0}/launcher/changelog.html", GetFTPUrl());
            return changelogURL;
        }

		/// <summary>
		/// Gets the game URL.
		/// </summary>
		/// <returns>The game URL.</returns>
		/// <param name="bGetSystemGame">If set to <c>true</c> b gets a platform-specific game.</param>
        public string GetGameURL(bool bGetSystemGame)
        {
			string gameURL;
			if (bGetSystemGame)
			{
				gameURL = String.Format ("{0}/game/{1}/bin", 
                    GetFTPUrl (), 
                    GetSystemTarget ());
			}
			else
			{
				gameURL = String.Format("{0}/game", 
                    GetFTPUrl());
			}

            return gameURL;
        }

		/// <summary>
		/// Gets the launcher version. Locks the config file - DO NOT USE INSIDE OTHER LOCKING FUNCTIONS
		/// </summary>
		/// <returns>The launcher version.</returns>
        public Version GetLocalLauncherVersion()
        {
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string launcherVersion = data["Local"]["LauncherVersion"];

					return Version.Parse(launcherVersion);

				}
				catch (IOException ioex)
				{
					Console.Write("GetLauncherVersion: ");
					Console.WriteLine(ioex.Message);
					return new Version ();
				}
			}            
        }

		/// <summary>
		/// Gets the name of the game. Locks the config file - DO NOT USE INSIDE OTHER LOCKING FUNCTIONS
		/// </summary>
		/// <returns>The game name.</returns>
        public string GetGameName()
        {
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());;

					string gameName = data["Local"]["GameName"];

					return gameName;
				}
				catch (IOException ioex)
				{
					Console.Write("GetGameName: ");
					Console.WriteLine(ioex.Message);
					return "";
				}
			}            
        }

		/// <summary>
		/// Sets the name of the game.
		/// </summary>
		/// <param name="GameName">Game name.</param>
		public void SetGameName(string GameName)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Local"]["GameName"] = GameName;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.Write("SetGameName: ");
					Console.WriteLine(ioex.Message);
				}

			}
		}

		/// <summary>
		/// Gets the system target.
		/// </summary>
		/// <returns>The system target.</returns>
        public ESystemTarget GetSystemTarget()
        {
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string systemTarget = data["Local"]["SystemTarget"];

					return Utilities.ParseSystemTarget(systemTarget);
				}
				catch (IOException ioex)
				{
					Console.Write("GetSystemTarget: ");
					Console.WriteLine(ioex.Message);
					return ESystemTarget.Invalid;
				}
			}            
        }

		/// <summary>
		/// Sets the system target.
		/// </summary>
		/// <param name="SystemTarget">System target.</param>
		public void SetSystemTarget(ESystemTarget SystemTarget)
		{
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());                    

					data["Local"]["SystemTarget"] = SystemTarget.ToString();

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.Write("SetSystemTarget: ");
					Console.WriteLine(ioex.Message);                    
				}
			}
		}

		/// <summary>
		/// Gets the FTP username.
		/// </summary>
		/// <returns>The FTP username.</returns>
        public string GetFTPUsername()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string FTPUsername = data["Remote"]["FTPUsername"];

					return FTPUsername;
				}
				catch (IOException ioex)
				{
					Console.Write("GetFTPUsername: ");
					Console.WriteLine(ioex.Message);
					return "";
				}
			}
        }

		/// <summary>
		/// Sets the FTP username.
		/// </summary>
		/// <param name="Username">Username.</param>
		public void SetFTPUsername(string Username)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Remote"]["FTPUsername"] = Username;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.Write("SetFTPUsername: ");
					Console.WriteLine(ioex.Message);
				}
			}
		}

		/// <summary>
		/// Gets the FTP password.
		/// </summary>
		/// <returns>The FTP password.</returns>
        public string GetFTPPassword()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string FTPPassword = data["Remote"]["FTPPassword"];

					return FTPPassword;
				}
				catch (IOException ioex)
				{
					Console.Write("GetFTPPassword: ");
					Console.WriteLine(ioex.Message);
					return "";
				}
			}
        }

		/// <summary>
		/// Sets the FTP password.
		/// </summary>
		/// <param name="Password">Password.</param>
		public void SetFTPPassword(string Password)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Remote"]["FTPPassword"] = Password;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.Write("GetFTPPassword: ");
					Console.WriteLine(ioex.Message);
				}
			}
		}

		/// <summary>
		/// Gets the base FTP URL.
		/// </summary>
		/// <returns>The base FTP URL.</returns>
		public string GetBaseFTPUrl()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string FTPURL = data["Remote"]["FTPUrl"];

					return FTPURL;
				}
				catch (IOException ioex)
				{
					Console.Write("GetBaseFTPURL: ");
					Console.WriteLine(ioex.Message);
					Console.WriteLine (ioex.StackTrace);
					Console.WriteLine (ioex.InnerException);
					return "";
				}
			}
		}


		/// <summary>
		/// Sets the base FTP URL.
		/// </summary>
		/// <param name="Url">URL.</param>
		public void SetBaseFTPUrl(string Url)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Remote"]["FTPUrl"] = Url;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.Write("GetFTPPassword: ");
					Console.WriteLine(ioex.Message);
				}
			}
		}

		/// <summary>
		/// Gets the FTP URL.
		/// </summary>
		/// <returns>The FTP URL.</returns>
        public string GetFTPUrl()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string FTPUrl = data["Remote"]["FTPUrl"];
					string FTPAuthUrl = FTPUrl.Substring(0, 6); // Gets ftp://
					FTPAuthUrl += data["Remote"]["FTPUsername"]; // Add the username
					FTPAuthUrl += ":";
					FTPAuthUrl += data["Remote"]["FTPPassword"]; // Add the password
					FTPAuthUrl += "@";
					FTPAuthUrl += FTPUrl.Substring(6);

					return FTPAuthUrl;
				}
				catch (IOException ioex)
				{
					Console.Write("GetFTPUrl: ");
					Console.WriteLine(ioex.Message);
					return "";
				}
			}
        }

		/// <summary>
		/// Gets if the launcher should receive official updates.
		/// </summary>
		/// <returns><c>true</c>, if the launcher should receive official updates, <c>false</c> otherwise.</returns>
        public bool GetDoOfficialUpdates()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string officialUpdatesStr = data["Launchpad"]["bOfficialUpdates"];

					return bool.Parse(officialUpdatesStr);
				}
				catch (IOException ioex)
				{
					Console.WriteLine (ioex.Message);
					return true;
				}
			}
        }

		/// <summary>
		/// Gets the launcher's unique GUID.
		/// </summary>
		/// <returns>The GUID.</returns>
		public string GetGUID()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string guid = data["Local"]["GUID"];

					return guid;
				}
				catch (IOException ioex)
				{
					Console.WriteLine (ioex.Message);
					return "";
				}
			}
		}

		/// <summary>
		/// Replaces and updates the old pre-unix config.
		/// </summary>
		/// <returns><c>true</c>, if an old config was found, <c>false</c> otherwise.</returns>
		private bool UpdateOldConfig()
		{
			string oldConfigPath = String.Format(@"{0}config{1}launcherConfig.ini", 
			                                     GetLocalDir(), 
			                                     Path.DirectorySeparatorChar);

			string oldConfigDir = String.Format(@"{0}config", GetLocalDir());

            if (ChecksHandler.IsRunningOnUnix())
            {
                //Case sensitive
                //Is there an old config file?
                if (File.Exists(oldConfigPath))
                {
                    lock (ReadLock)
					{
						//Have we not already created the new config dir?
						if (!Directory.Exists(GetConfigDir()))
						{
							//if not, create it.
							Directory.CreateDirectory(GetConfigDir());

							//Copy the old config file to the new location.
							File.Copy(oldConfigPath, GetConfigPath());

							//read our new file.
							FileIniDataParser Parser = new FileIniDataParser();
							IniData data = Parser.ReadFile(GetConfigPath());

							//replace the old invalid keys with new, updated keys.
							string launcherVersion = data["Local"]["launcherVersion"];
							string gameName = data["Local"]["gameName"];
							string systemTarget = data["Local"]["systemTarget"];

							data["Local"].RemoveKey("launcherVersion");
							data["Local"].RemoveKey("gameName");
							data["Local"].RemoveKey("systemTarget");

							data["Local"].AddKey("LauncherVersion", launcherVersion);
							data["Local"].AddKey("GameName", gameName);
							data["Local"].AddKey("SystemTarget", systemTarget);

							WriteConfig (Parser, data);
							//We were successful, so return true.

							File.Delete(oldConfigPath);
							Directory.Delete(oldConfigDir, true);
							return true;
						}
						else
						{
							//Delete the old config
							File.Delete(oldConfigPath);
							Directory.Delete(oldConfigDir, true);
							return false;
						}
					}
                }
                else
                {
                    return false;
                }
            }
            else
            {
				lock (ReadLock)
				{
					//Windows, so direct access without copying.
					//read our new file.
                    if (File.Exists(oldConfigPath))
                    {
                        FileIniDataParser Parser = new FileIniDataParser();
					    IniData data = Parser.ReadFile(GetConfigPath());

					    //replace the old invalid keys with new, updated keys.
					    string launcherVersion = data["Local"]["launcherVersion"];
					    string gameName = data["Local"]["gameName"];
					    string systemTarget = data["Local"]["systemTarget"];

					    data["Local"].RemoveKey("launcherVersion");
					    data["Local"].RemoveKey("gameName");
					    data["Local"].RemoveKey("systemTarget");

					    data["Local"].AddKey("LauncherVersion", launcherVersion);
					    data["Local"].AddKey("GameName", gameName);
					    data["Local"].AddKey("SystemTarget", systemTarget);

					    WriteConfig (Parser, data);

					    //We were successful, so return true.
					    return true;
                    }	
				    else
                    {
                        return false;
                    }

				}               
            }

		}		      

		/// <summary>
		/// Replaces the old update cookie.
		/// </summary>
		private static void ReplaceOldUpdateCookie ()
		{
			string oldUpdateCookie = String.Format (@"{0}{1}.updatecookie", Directory.GetCurrentDirectory (), Path.DirectorySeparatorChar);
			if (File.Exists (oldUpdateCookie))
			{
				string updateCookie = String.Format (@"{0}{1}.update", Directory.GetCurrentDirectory (), Path.DirectorySeparatorChar);
				File.Move (oldUpdateCookie, updateCookie);
			}
		}
    }
}
