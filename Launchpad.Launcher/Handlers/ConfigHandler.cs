using IniParser;
using IniParser.Model;
using System;
using System.IO;


namespace Launchpad.Launcher
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
		/// Writes the config data to disk. This method is thread-blocking, and all write operations 
		/// are synchronized via lock(WriteLock).
		/// </summary>
		/// <param name="Parser">The parser dealing with the current data.</param>
		/// <param name="Data">The data which should be written to file.</param>
		private void WriteConfig(FileIniDataParser Parser, IniData Data)
		{
			lock (WriteLock)
			{
				Parser.WriteFile (GetConfigPath (), Data);
			}
		}

		/// <summary>
		/// Gets the path to the config file on disk.
		/// </summary>
		/// <returns>The config path.</returns>
        private static string GetConfigPath()
        {
			string configPath = String.Format(@"{0}LauncherConfig.ini", 
			                                  GetConfigDir());
            
            return configPath;
        }

		/// <summary>
		/// Gets the path to the config directory.
		/// </summary>
		/// <returns>The config dir, terminated with a directory separator.</returns>
        private static string GetConfigDir()
        {
			string configDir = String.Format(@"{0}Config{1}", 
			                                 GetLocalDir(),
			                                 Path.DirectorySeparatorChar);
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

			//Minor release 0.1.1, bugfixes
			Version defaultLauncherVersion = new Version("0.1.0");

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

						//generate a new GUID for this install instance of the launcher
						string GeneratedGUID = Guid.NewGuid ().ToString ();

						data.Sections.AddSection("Local");
						data.Sections.AddSection("Remote");
						data.Sections.AddSection("Launchpad");

						data["Local"].AddKey("LauncherVersion", defaultLauncherVersion.ToString());
						data["Local"].AddKey("GameName", "LaunchpadExample");

                        //set the default system target to what the launcher is running on. Developers will need 
                        //to alter this in the config, based on where they're deploying to.
						data["Local"].AddKey("SystemTarget", GetCurrentPlatform().ToString());
						data["Local"].AddKey("GUID", GeneratedGUID);

						data["Remote"].AddKey("PatchUsername", "anonymous");
						data["Remote"].AddKey("PatchPassword", "anonymous");
						data["Remote"].AddKey("PatchUrl", "ftp://directorate.asuscomm.com");

						data["Launchpad"].AddKey("bOfficialUpdates", "true");

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine ("IOException in ConfigHandler.Initialize(): " + ioex.Message);
					}

				}
				else
				{
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Local"]["LauncherVersion"] = defaultLauncherVersion.ToString();
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
		/// Gets the path to the update cookie on disk.
		/// </summary>
		/// <returns>The update cookie.</returns>
        public static string GetUpdateCookiePath()
        {
			string updateCookie = String.Format(@"{0}.update", 
			                                    GetLocalDir());
            return updateCookie;
        }

		/// <summary>
		/// Creates the update cookie.
		/// </summary>
		/// <returns>The update cookie's path.</returns>
		public static string CreateUpdateCookie()
		{
			bool bCookieExists = File.Exists (GetUpdateCookiePath());
			if (!bCookieExists)
			{
				File.Create (GetUpdateCookiePath());

				return GetUpdateCookiePath ();
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
			string installCookie = String.Format(@"{0}.install", 
			                                    GetLocalDir());
			return installCookie;
		}

		/// <summary>
		/// Creates the install cookie.
		/// </summary>
		/// <returns>The install cookie's path.</returns>
		public static string CreateInstallCookie()
		{
			bool bCookieExists = File.Exists (GetInstallCookiePath());

			if (!bCookieExists)
			{
				File.Create (GetInstallCookiePath()).Close();

				return GetInstallCookiePath ();
			}
			else
			{
				return GetInstallCookiePath ();
			}
		}

		/// <summary>
		/// Gets the local dir.
		/// </summary>
		/// <returns>The local dir, terminated by a directory separator.</returns>
        public static string GetLocalDir()
        {
			string localDir = String.Format(@"{0}{1}", 
			                                Directory.GetCurrentDirectory(), 
			                                Path.DirectorySeparatorChar);
            return localDir;
        }

		/// <summary>
		/// Gets the temporary files directory.
		/// </summary>
		/// <returns>The temporary files directory, terminated by a directory separator.</returns>
        public static string GetTempDir()
        {
			string tempDir = Path.GetTempPath ();
            return tempDir;
        }

		/// <summary>
		/// Gets the manifest's path on disk.
		/// </summary>
		/// <returns>The manifest path.</returns>
        public static string GetManifestPath()
        {
            string manifestPath = String.Format(@"{0}LauncherManifest.txt", 
			                                    GetLocalDir());
            return manifestPath;
        }

		/// <summary>
		/// Gets the old manifest's path on disk.
		/// </summary>
		/// <returns>The old manifest's path.</returns>
		public static string GetOldManifestPath()
		{
			string oldManifestPath = String.Format(@"{0}LauncherManifest.txt.old", 
			                                    GetLocalDir());
			return oldManifestPath;
		}

		/// <summary>
		/// Gets the game path.
		/// </summary>
		/// <returns>The game path, terminated by a directory separator.</returns>
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
		/// Gets the path to the game executable.
		/// </summary>
		/// <returns>The game executable.</returns>
        public string GetGameExecutable()
        {
			string executablePathRootLevel = String.Empty;
			string executablePathTargetLevel = String.Empty;

			//unix doesn't need (or have!) the .exe extension.
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
				Console.WriteLine ("Searched at: " + executablePathRootLevel);
				Console.WriteLine ("Searched at: " + executablePathTargetLevel);
				throw new FileNotFoundException ("The game executable could not be found.");
			}
        }

		/// <summary>
		/// Gets the local game version.
		/// </summary>
		/// <returns>The local game version.</returns>
		public Version GetLocalGameVersion()
		{
			string rawGameVersion = String.Empty;
			Version gameVersion = null;
			try
			{
				rawGameVersion = File.ReadAllText(GetGameVersionPath());
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in GetLocalGameVersion(): " + ioex.Message);
			}

			try
			{
				gameVersion = Version.Parse(rawGameVersion);
			}
			catch (ArgumentException aex)
			{
				Console.WriteLine ("ArgumentException in GetLocalGameVersion(): " + aex.Message);
			}

			return gameVersion;
		}

		/// <summary>
		/// Gets the game version path.
		/// </summary>
		/// <returns>The game version path.</returns>
		public string GetGameVersionPath()
		{
			string localVersionPath = String.Format(@"{0}GameVersion.txt",
			                                GetGamePath(true));

			return localVersionPath;
		}
		/// <summary>
		/// Gets the manifest URL.
		/// </summary>
		/// <returns>The manifest URL.</returns>
        public string GetManifestURL()
        {
            string manifestURL = String.Format("{0}/game/{1}/LauncherManifest.txt", 
                GetPatchUrl(),
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
                GetPatchUrl(), 
                GetSystemTarget());

            return manifestChecksumURL;
        }

		/// <summary>
		/// Gets the custom launcher download URL.
		/// </summary>
		/// <returns>The custom launcher download URL.</returns>
        public string GetLauncherBinariesURL()
        {
            string launcherURL = String.Format("{0}/launcher/bin/", 
			                                   GetPatchUrl());
            return launcherURL;
        }

		/// <summary>
		/// Gets the changelog URL.
		/// </summary>
		/// <returns>The changelog URL.</returns>
        public string GetChangelogURL()
        {
            string changelogURL = String.Format("{0}/launcher/changelog.html", 
			                                    GetPatchUrl());
            return changelogURL;
        }

		/// <summary>
		/// Gets the game URL.
		/// </summary>
		/// <returns>The game URL.</returns>
		/// <param name="bGetSystemGame">If set to <c>true</c> b gets a platform-specific game.</param>
        public string GetGameURL(bool bIncludeSystemTarget)
        {
			string gameURL = String.Empty;
			if (bIncludeSystemTarget)
			{
				gameURL = String.Format ("{0}/game/{1}/bin/", 
                    GetPatchUrl (), 
                    GetSystemTarget ());
			}
			else
			{
				gameURL = String.Format("{0}/game/", 
                    GetPatchUrl());
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
					Console.WriteLine("IOException in GetLauncherVersion(): " + ioex.Message);
					return null;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine ("ArgumentException in GetLauncherVersion(): " + aex.Message);
					return null;
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
					Console.WriteLine("IOException in GetGameName(): " + ioex.Message);
					return String.Empty;
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
					Console.WriteLine("IOException in SetGameName(): " + ioex.Message);
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
					Console.WriteLine("IOException in GetSystemTarget(): " + ioex.Message);
					return ESystemTarget.Invalid;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine("ArgumentException in GetSystemTarget(): " + aex.Message);
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
					Console.WriteLine("IOException in SetSystemTarget(): " + ioex.Message);                  
				}
			}
		}

		/// <summary>
		/// Gets the Patch username.
		/// </summary>
		/// <returns>The Patch username.</returns>
        public string GetPatchUsername()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string PatchUsername = data["Remote"]["PatchUsername"];

					return PatchUsername;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchUsername(): " + ioex.Message);
					return String.Empty;
				}
			}
        }

		/// <summary>
		/// Sets the Patch username.
		/// </summary>
		/// <param name="Username">Username.</param>
		public void SetPatchUsername(string Username)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Remote"]["PatchUsername"] = Username;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in SetPatchUsername(): " + ioex.Message);
				}
			}
		}

		/// <summary>
		/// Gets the Patch password.
		/// </summary>
		/// <returns>The Patch password.</returns>
        public string GetPatchPassword()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string PatchPassword = data["Remote"]["PatchPassword"];

					return PatchPassword;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchPassword: " + ioex.Message);
					return String.Empty;
				}
			}
        }

		/// <summary>
		/// Sets the Patch password.
		/// </summary>
		/// <param name="Password">Password.</param>
		public void SetPatchPassword(string Password)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Remote"]["PatchPassword"] = Password;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchPassword(): " + ioex.Message);
				}
			}
		}

		/// <summary>
		/// Gets the base Patch URL.
		/// </summary>
		/// <returns>The base Patch URL.</returns>
		public string GetBasePatchUrl()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string PatchURL = data["Remote"]["PatchUrl"];

					return PatchURL;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetBasePatchURL(): " + ioex.Message);
					return String.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the base Patch URL.
		/// </summary>
		/// <param name="Url">URL.</param>
		public void SetBasePatchUrl(string Url)
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					data["Remote"]["PatchUrl"] = Url;

					WriteConfig(Parser, data);
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchPassword(): " + ioex.Message);				
				}
			}
		}

		/// <summary>
		/// Gets the Patch URL.
		/// </summary>
		/// <returns>The Patch URL.</returns>
        public string GetPatchUrl()
        {
            lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string PatchUrl = data["Remote"]["PatchUrl"];

                    // If we need to massage the URL, do it here.

                    string PatchAuthUrl = PatchUrl;

					return PatchAuthUrl;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchUrl(): " + ioex.Message);
					return String.Empty;
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

					string rawDoOfficialUpdates = data["Launchpad"]["bOfficialUpdates"];

					return bool.Parse(rawDoOfficialUpdates);
				}
				catch (IOException ioex)
				{
					Console.WriteLine ("IOException in GetDoOfficialUpdates(): " + ioex.Message);
					return true;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine ("ArgumentException in GetDoOfficialUpdates(): " + aex.Message);
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
					Console.WriteLine ("IOException in GetGUID(): " + ioex.Message);
					return String.Empty;
				}
			}
		}

        /// <summary>
        /// Parses the Protocol URL to determine if we are using HTTP or FTP. may have to change to a form of enum if otehrs are added.
        /// </summary>
        /// <returns>true if HTTP</returns>
        public bool bUseHTTP()
        {
            lock (ReadLock)
            {
                try
                {
                    FileIniDataParser Parser = new FileIniDataParser();
                    IniData data = Parser.ReadFile(GetConfigPath());

                    string url = data["Remote"]["PatchUrl"];

                    bool returnVal = url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase);

                    return url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase);
                }
                catch (IOException ioex)
                {
                    Console.WriteLine("IOException in bUseHTTP(): " + ioex.Message);
                    return true;
                }
            }
        }
        /// <summary>
        /// Replaces and updates the old pre-unix config.
        /// </summary>
        /// <returns><c>true</c>, if an old config was copied over to the new format, <c>false</c> otherwise.</returns>
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
							//The new config dir already exists, so we'll just toss out the old one.
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
					//Windows is not case sensitive, so we'll use direct access without copying.
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
			string oldUpdateCookiePath = String.Format (@"{0}.updatecookie",
			                                        GetLocalDir());

			if (File.Exists (oldUpdateCookiePath))
			{
				string updateCookiePath = String.Format (@"{0}.update", 
				                                     GetLocalDir());

				File.Move (oldUpdateCookiePath, updateCookiePath);
			}
		}

        public static ESystemTarget GetCurrentPlatform()
        {
            string platformID = Environment.OSVersion.Platform.ToString();
            if (platformID.Contains("Win"))
            {
                platformID = "Windows";
            }

            switch (platformID)
            {
                case "MacOSX":
                    {
                        return ESystemTarget.Mac;
                    }
                case "Unix":
                    {
                        //Mac may sometimes be detected as Unix, so do an additional check for some Mac-only directories
                        if (Directory.Exists("/Applications") && Directory.Exists("/System") && Directory.Exists("/Users") && Directory.Exists("/Volumes"))
                        {
                            return ESystemTarget.Mac;
                        }
                        else
                        {
                            return ESystemTarget.Linux;
                        }                                                               
                    }
                case "Windows":
                    {
                        if (Environment.Is64BitOperatingSystem)
                        {
                            return ESystemTarget.Win64;
                        }
                        else
                        {
                            return ESystemTarget.Win32;
                        }
                    }
                default:
                    {
                        return ESystemTarget.Invalid;
                    }
            }
        }
    }
}
