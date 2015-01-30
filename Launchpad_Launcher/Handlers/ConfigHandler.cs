using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IniParser;
using IniParser.Model;
using System.IO;

namespace Launchpad_Launcher
{
    public class ConfigHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Launchpad_Launcher.ConfigHandler"/> class.
        /// </summary>
        public ConfigHandler()
        {
            FileIniDataParser Parser = new FileIniDataParser();

            string configDir = GetConfigDir();
            string configPath = GetConfigPath();

            //Major release 0.1.1, linux support
            string defaultLauncherVersion = "0.0.4";

			//Check for pre-unix config. If it exists, fix the values and copy it.
			CheckForOldConfig ();

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            if (!File.Exists(configPath) && !IsRunningOnUnix())
            {
                //here we create a new empty file
                FileStream configStream = File.Create(configPath);
                configStream.Close();

                //read the file as an INI file
                try
                {
                    IniData data = Parser.ReadFile(configPath);
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

                    Parser.WriteFile(configPath, data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }

            }
            else
            {
                IniData data = Parser.ReadFile(configPath);
                data["Local"]["LauncherVersion"] = defaultLauncherVersion;
				if (!data ["Local"].ContainsKey ("GUID"))
				{
					string GeneratedGUID = Guid.NewGuid ().ToString ();
					data ["Local"].AddKey ("GUID", GeneratedGUID);
				}

                Parser.WriteFile(configPath, data);
            }
        }

		/// <summary>
		/// Gets the config path.
		/// </summary>
		/// <returns>The config path.</returns>
        private string GetConfigPath()
        {
			string configPath = String.Format(@"{0}Config{1}LauncherConfig.ini", 
			                                  GetLocalDir(), 
			                                  Path.DirectorySeparatorChar);
            Console.WriteLine(configPath);
            
            return configPath;
        }

		/// <summary>
		/// Gets the config dir.
		/// </summary>
		/// <returns>The config dir.</returns>
        private string GetConfigDir()
        {
			string configDir = String.Format(@"{0}Config", GetLocalDir());
            return configDir;
        }

		/// <summary>
		/// Gets the update cookie.
		/// </summary>
		/// <returns>The update cookie.</returns>
        public string GetUpdateCookie()
        {
			string updateCookie = String.Format(@"{0}{1}.updatecookie", 
			                                    Directory.GetCurrentDirectory(), 
			                                    Path.DirectorySeparatorChar);
            return updateCookie;
        }

		/// <summary>
		/// Creates the update cookie.
		/// </summary>
		/// <returns>The update cookie's path.</returns>
		public string CreateUpdateCookie()
		{
			bool bCookieExists = File.Exists (String.Format (@"{0}{1}.updatecookie", 
			                                                Directory.GetCurrentDirectory (), 
			                                                Path.DirectorySeparatorChar));
			if (!bCookieExists)
			{
				File.Create (String.Format(@"{0}{1}.updatecookie", 
				                           Directory.GetCurrentDirectory(), 
				                           Path.DirectorySeparatorChar));

				return String.Format(@"{0}{1}.updatecookie", 
				                      Directory.GetCurrentDirectory(), 
				                      Path.DirectorySeparatorChar);
			}
			else
			{
				return GetUpdateCookie ();
			}
		}

		/// <summary>
		/// Gets the local dir.
		/// </summary>
		/// <returns>The local dir.</returns>
        public string GetLocalDir()
        {
			string localDir = String.Format(@"{0}{1}", Directory.GetCurrentDirectory(), Path.DirectorySeparatorChar);
            return localDir;
        }

		/// <summary>
		/// Gets the temp dir.
		/// </summary>
		/// <returns>The temp dir.</returns>
        public string GetTempDir()
        {
			string tempDir = Path.GetTempPath ();
            return tempDir;
        }

		/// <summary>
		/// Gets the manifest path.
		/// </summary>
		/// <returns>The manifest path.</returns>
        public string GetManifestPath()
        {
            string manifestPath = String.Format(@"{0}LauncherManifest.txt", GetLocalDir());
            return manifestPath;
        }

		/// <summary>
		/// Gets the game path.
		/// </summary>
		/// <returns>The game path.</returns>
        public string GetGamePath()
        {
			string gamePath = String.Format(@"{0}Game", GetLocalDir());
            return gamePath;
        }

		/// <summary>
		/// Gets the game executable.
		/// </summary>
		/// <returns>The game executable.</returns>
        public string GetGameExecutable()
        {
			string executablePath = String.Format(@"{0}{2}{1}.exe", 
			                                      GetGamePath(), 
			                                      GetGameName(),  
			                                      Path.DirectorySeparatorChar);
            return executablePath;
        }

		/// <summary>
		/// Gets the manifest URL.
		/// </summary>
		/// <returns>The manifest URL.</returns>
        public string GetManifestURL()
        {
            string manifestURL = String.Format("{0}/launcher/LauncherManifest.txt", GetFTPUrl());
            return manifestURL;
        }

		/// <summary>
		/// Gets the manifest checksum URL.
		/// </summary>
		/// <returns>The manifest checksum URL.</returns>
        public string GetManifestChecksumURL()
        {
            string manifestChecksumURL = String.Format("{0}/launcher/LauncherManifest.checksum", GetFTPUrl());
            return manifestChecksumURL;
        }

		/// <summary>
		/// Gets the custom launcher download URL.
		/// </summary>
		/// <returns>The custom launcher download URL.</returns>
        public string GetLauncherURL()
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
				gameURL = String.Format ("{0}/game/{1}", GetFTPUrl (), GetSystemTarget ());
			}
			else
			{
				gameURL = String.Format("{0}/game", GetFTPUrl());
			}

            return gameURL;
        }

		/// <summary>
		/// Gets the launcher version.
		/// </summary>
		/// <returns>The launcher version.</returns>
        public string GetLauncherVersion()
        {
            try
            {
                FileIniDataParser Parser = new FileIniDataParser();
                IniData data = Parser.ReadFile(GetConfigPath());

                string launcherVersion = data["Local"]["LauncherVersion"];

                return launcherVersion;
            }
            catch (Exception ex)
            {
                Console.Write("GetLauncherVersion: ");
                Console.WriteLine(ex.StackTrace);
                return "";
            }

        }

		/// <summary>
		/// Gets the name of the game.
		/// </summary>
		/// <returns>The game name.</returns>
        public string GetGameName()
        {
            try
            {
                FileIniDataParser Parser = new FileIniDataParser();
                IniData data = Parser.ReadFile(GetConfigPath());

                string gameName = data["Local"]["GameName"];

                return gameName;
            }
            catch (Exception ex)
            {
                Console.Write("GetGameName: ");
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }

		/// <summary>
		/// Sets the name of the game.
		/// </summary>
		/// <param name="GameName">Game name.</param>
		public void SetGameName(string GameName)
		{
			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				data["Local"]["GameName"] = GameName;
				Parser.WriteFile(GetConfigPath(), data);
			}
			catch (Exception ex)
			{
				Console.Write("SetGameName: ");
				Console.WriteLine(ex.StackTrace);
			}
		}

		/// <summary>
		/// Gets the system target.
		/// </summary>
		/// <returns>The system target.</returns>
        public string GetSystemTarget()
        {
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac

            try
            {
                FileIniDataParser Parser = new FileIniDataParser();
                IniData data = Parser.ReadFile(GetConfigPath());

                string systemTarget = data["Local"]["SystemTarget"];

                return systemTarget;
            }
            catch (Exception ex)
            {
                Console.Write("GetSystemTarget: ");
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }

		/// <summary>
		/// Sets the system target.
		/// </summary>
		/// <param name="SystemTarget">System target.</param>
		public void SetSystemTarget(string SystemTarget)
		{
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac

			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				data["Local"]["SystemTarget"] = SystemTarget;

				Parser.WriteFile(GetConfigPath(), data);
			}
			catch (Exception ex)
			{
				Console.Write("SetSystemTarget: ");
				Console.WriteLine(ex.StackTrace);
			}
		}

		/// <summary>
		/// Gets the FTP username.
		/// </summary>
		/// <returns>The FTP username.</returns>
        public string GetFTPUsername()
        {
            try
            {
                FileIniDataParser Parser = new FileIniDataParser();
                IniData data = Parser.ReadFile(GetConfigPath());

                string FTPUsername = data["Remote"]["FTPUsername"];

                return FTPUsername;
            }
            catch (Exception ex)
            {
                Console.Write("GetFTPUsername: ");
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }

		/// <summary>
		/// Sets the FTP username.
		/// </summary>
		/// <param name="Username">Username.</param>
		public void SetFTPUsername(string Username)
		{
			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				data["Remote"]["FTPUsername"] = Username;
				Parser.WriteFile(GetConfigPath(), data);
			}
			catch (Exception ex)
			{
				Console.Write("SetFTPUsername: ");
				Console.WriteLine(ex.StackTrace);
			}

		}

		/// <summary>
		/// Gets the FTP password.
		/// </summary>
		/// <returns>The FTP password.</returns>
        public string GetFTPPassword()
        {
            try
            {
                FileIniDataParser Parser = new FileIniDataParser();
                IniData data = Parser.ReadFile(GetConfigPath());

                string FTPPassword = data["Remote"]["FTPPassword"];

                return FTPPassword;
            }
            catch (Exception ex)
            {
                Console.Write("GetFTPPassword: ");
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }

		/// <summary>
		/// Sets the FTP password.
		/// </summary>
		/// <param name="Password">Password.</param>
		public void SetFTPPassword(string Password)
		{
			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				data["Remote"]["FTPPassword"] = Password;
				Parser.WriteFile(GetConfigPath(), data);
			}
			catch (Exception ex)
			{
				Console.Write("GetFTPPassword: ");
				Console.WriteLine(ex.StackTrace);
			}

		}

		/// <summary>
		/// Gets the base FTP URL.
		/// </summary>
		/// <returns>The base FTP URL.</returns>
		public string GetBaseFTPUrl()
		{
			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				string FTPURL = data["Remote"]["FTPUrl"];

				return FTPURL;
			}
			catch (Exception ex)
			{
				Console.Write("GetRawFTPURL: ");
				Console.WriteLine(ex.StackTrace);
				return "";
			}
		}


		/// <summary>
		/// Sets the base FTP URL.
		/// </summary>
		/// <param name="Url">URL.</param>
		public void SetBaseFTPUrl(string Url)
		{
			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				data["Remote"]["FTPUrl"] = Url;
				Parser.WriteFile(GetConfigPath(), data);
			}
			catch (Exception ex)
			{
				Console.Write("GetFTPPassword: ");
				Console.WriteLine(ex.StackTrace);
			}
		}

		/// <summary>
		/// Gets the FTP URL.
		/// </summary>
		/// <returns>The FTP URL.</returns>
        public string GetFTPUrl()
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
            catch (Exception ex)
            {
                Console.Write("GetFTPUrl: ");
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }

		/// <summary>
		/// Gets if the launcher should receive official updates.
		/// </summary>
		/// <returns><c>true</c>, if the launcher should receive official updates, <c>false</c> otherwise.</returns>
        public bool GetDoOfficialUpdates()
        {
            try
            {
                FileIniDataParser Parser = new FileIniDataParser();
                IniData data = Parser.ReadFile(GetConfigPath());

                string officialUpdatesStr = data["Launchpad"]["bOfficialUpdates"];
                return bool.Parse(officialUpdatesStr);
            }
            catch (Exception ex)
            {
				Console.WriteLine (ex.StackTrace);
                return true;
            }
        }

		/// <summary>
		/// Gets the launcher's unique GUID.
		/// </summary>
		/// <returns>The GUID.</returns>
		public string GetGUID()
		{
			try
			{
				FileIniDataParser Parser = new FileIniDataParser();
				IniData data = Parser.ReadFile(GetConfigPath());

				string guid = data["Local"]["GUID"];
				return guid;
			}
			catch (Exception ex)
			{
				Console.WriteLine (ex.StackTrace);
				return "";
			}
		}

		/// <summary>
		/// Checks for old config.
		/// </summary>
		/// <returns><c>true</c>, if an old config was found, <c>false</c> otherwise.</returns>
		private bool CheckForOldConfig()
		{
			string oldConfigPath = String.Format(@"{0}config{1}launcherConfig.ini", 
			                                     GetLocalDir(), 
			                                     Path.DirectorySeparatorChar);

			string oldConfigDir = String.Format(@"{0}config", GetLocalDir());

            if (IsRunningOnUnix())
            {
                //Case sensitive
                //Is there an old config file?
                if (File.Exists(oldConfigPath))
                {
                    //Have not we already created the new config dir?
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

                        Parser.WriteFile(GetConfigPath(), data);
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
                else
                {
                    return false;
                }
            }
            else
            {
                //Windows, so direct access without copying.
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

                Parser.WriteFile(GetConfigPath(), data);
                //We were successful, so return true.
                return true;
            }
			
		}

        private bool IsRunningOnUnix()
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
    }
}
