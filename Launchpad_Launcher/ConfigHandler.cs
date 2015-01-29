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
        //constructor
        public ConfigHandler()
        {
            FileIniDataParser Parser = new FileIniDataParser();

            string configDir = GetConfigDir();
            string configPath = GetConfigPath();

            //release 0.0.
            string defaultLauncherVersion = "0.0.4";

			//Check for pre-unix config. If it exists, fix the values and copy it.
			CheckForOldConfig ();

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
                    IniData data = Parser.ReadFile(configPath);

                    data.Sections.AddSection("Local");
                    data.Sections.AddSection("Remote");
                    data.Sections.AddSection("Launchpad");

                    data["Local"].AddKey("LauncherVersion", defaultLauncherVersion);
                    data["Local"].AddKey("GameName", "Example");
                    data["Local"].AddKey("SystemTarget", "Win64");

                    data["Remote"].AddKey("FTPUsername", "anonymous");
                    data["Remote"].AddKey("FTPPassword", "anonymous");
                    data["Remote"].AddKey("FTPUrl", "ftp://example.example.com");

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

                Parser.WriteFile(configPath, data);
            }
        }

        private string GetConfigPath()
        {
			string configPath = String.Format(@"{0}Config{1}LauncherConfig.ini", GetLocalDir(), Path.DirectorySeparatorChar);
            return configPath;
        }

        private string GetConfigDir()
        {
			string configDir = String.Format(@"{0}Config", GetLocalDir());
            return configDir;
        }

        public string GetUpdateCookie()
        {
            string updateCookie = String.Format(@"{0}.updatecookie", Directory.GetCurrentDirectory());
            return updateCookie;
        }

        public string GetLocalDir()
        {
			string localDir = String.Format(@"{0}{1}", Directory.GetCurrentDirectory(), Path.DirectorySeparatorChar);
            return localDir;
        }

        public string GetTempDir()
        {
			string tempDir = Path.GetTempPath ();
            return tempDir;
        }

        public string GetManifestPath()
        {
            string manifestPath = String.Format(@"{0}LauncherManifest.txt", GetLocalDir());
            return manifestPath;
        }

        public string GetGamePath()
        {
			string gamePath = String.Format(@"{0}Game", GetLocalDir());
            return gamePath;
        }

        public string GetGameExecutable()
        {
			string executablePath = String.Format(@"{0}{3}{1}{3}Binaries{3}{2}{3}{1}.exe", GetGamePath(), GetGameName(), GetSystemTarget(), Path.DirectorySeparatorChar);
            return executablePath;
        }

        public string GetManifestURL()
        {
            string manifestURL = String.Format("{0}/Launcher/LauncherManifest.txt", GetFTPUrl());
            return manifestURL;
        }

        public string GetManifestChecksumURL()
        {
            string manifestChecksumURL = String.Format("{0}/Launcher/LauncherManifest.checksum", GetFTPUrl());
            return manifestChecksumURL;
        }

        public string GetLauncherURL()
        {
            string launcherURL = String.Format("{0}/Launcher/bin/Launchpad.exe", GetFTPUrl());
            return launcherURL;
        }

        public string GetChangelogURL()
        {
            string changelogURL = String.Format("{0}/Launcher/changelog.html", GetFTPUrl());
            return changelogURL;
        }

        public string GetGameURL(bool bGetSystemGame)
        {
			string gameURL;
			if (bGetSystemGame)
			{
				gameURL = String.Format ("{0}/Game/{1}", GetFTPUrl (), GetSystemTarget ());
			}
			else
			{
				gameURL = String.Format("{0}/Game", GetFTPUrl());
			}

            return gameURL;
        }

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

        public string GetSystemTarget()
        {
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

		private bool CheckForOldConfig()
		{
			string oldConfigPath = String.Format(@"{0}config{1}launcherConfig.ini", GetLocalDir(), Path.DirectorySeparatorChar);
			string oldConfigDir = String.Format(@"{0}config", GetLocalDir());

			//Is there an old config file?
			if (File.Exists (oldConfigPath))
			{
				//Have not we already created the new config dir?
				if (!Directory.Exists (GetConfigDir ()))
				{
					//if not, create it.
					Directory.CreateDirectory (GetConfigDir ());

					//Copy the old config file to the new location.
					File.Copy (oldConfigPath, GetConfigPath ());

					//read our new file.
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					//replace the old invalid keys with new, updated keys.
					string launcherVersion = data["Local"]["launcherVersion"];
					string gameName = data["Local"]["gameName"];
					string systemTarget = data["Local"]["systemTarget"];

					data ["Local"].RemoveKey ("launcherVersion");
					data ["Local"].RemoveKey ("gameName");
					data ["Local"].RemoveKey ("systemTarget");

					data ["Local"].AddKey ("LauncherVersion", launcherVersion);
					data ["Local"].AddKey ("GameName", gameName);
					data ["Local"].AddKey ("SystemTarget", systemTarget);

					Parser.WriteFile(GetConfigPath(), data);
					//We were successful, so return true.

					File.Delete (oldConfigPath);
					Directory.Delete (oldConfigDir, true);
					return true;
				}
				else
				{
					//Delete the old config
					File.Delete (oldConfigPath);
					Directory.Delete (oldConfigDir, true);
					return false;
				}
			} 
			else
			{
				return false;
			}
		}
    }
}
