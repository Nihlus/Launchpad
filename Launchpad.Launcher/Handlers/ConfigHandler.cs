//
//  ConfigHandler.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using IniParser;
using IniParser.Model;
using System;
using System.IO;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Utility;
using Launchpad.Launcher.Handlers.Protocols;
using System.Security.Cryptography;
using System.Text;


namespace Launchpad.Launcher.Handlers
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
		private object ReadLock = new Object();
		/// <summary>
		/// The write lock object.
		/// </summary>
		private object WriteLock = new Object();

		/// <summary>
		/// The singleton Instance. Will always point to one shared object.
		/// </summary>
		public static readonly ConfigHandler _instance = new ConfigHandler();

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
				Parser.WriteFile(GetConfigPath(), Data);
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

			// Get the launcher version from the assembly.
			Version defaultLauncherVersion = typeof(ConfigHandler).Assembly.GetName().Version;

			//Check for pre-unix config. If it exists, fix the values and copy it.
			UpdateOldConfig();

			//Check for old cookie file. If it exists, rename it.
			ReplaceOldUpdateCookie();

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

						data.Sections.AddSection("Local");
						data.Sections.AddSection("Remote");
						data.Sections.AddSection("FTP");
						data.Sections.AddSection("HTTP");
						data.Sections.AddSection("BitTorrent");
						data.Sections.AddSection("Launchpad");

						data["Local"].AddKey("LauncherVersion", defaultLauncherVersion.ToString());
						data["Local"].AddKey("GameName", "LaunchpadExample");
						data["Local"].AddKey("SystemTarget", GetCurrentPlatform().ToString());
						data["Local"].AddKey("GUID", GenerateSeededGUID("LaunchpadExample"));					

						data["Remote"].AddKey("ChangelogURL", "http://directorate.asuscomm.com/launchpad/changelog/changelog.html");
						data["Remote"].AddKey("Protocol", "FTP");
						data["Remote"].AddKey("FileRetries", "2");
						data["Remote"].AddKey("Username", "anonymous");
						data["Remote"].AddKey("Password", "anonymous");

						data["FTP"].AddKey("URL", "ftp://directorate.asuscomm.com");

						data["HTTP"].AddKey("URL", "http://directorate.asuscomm.com/launchpad");

						data["BitTorrent"].AddKey("Magnet", "");

						data["Launchpad"].AddKey("bOfficialUpdates", "true");
						data["Launchpad"].AddKey("bAllowAnonymousStats", "true");

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine("IOException in ConfigHandler.Initialize(): " + ioex.Message);
					}

				}
				else
				{
					/*
						This section is for updating old configuration files 
						with new sections introduced in updates.

						It's good practice to wrap each updating section in a
						small informational header with the date and change.
					*/

					IniData data = Parser.ReadFile(GetConfigPath());

					// Update the user-visible version of the launcher
					data["Local"]["LauncherVersion"] = defaultLauncherVersion.ToString();

					// ...

					// March 22 - 2016: Changed GUID generation to create a unique GUID for each game name
					// Update config files without GUID keys
					string seededGUID = GenerateSeededGUID(data["Local"].GetKeyData("GameName").Value);
					if (!data["Local"].ContainsKey("GUID"))
					{
						data["Local"].AddKey("GUID", seededGUID);
					}
					else
					{
						// Update the game GUID
						data["Local"]["GUID"] = seededGUID;
					}
					// End March 22 - 2016

					// Update config files without protocol keys
					if (!data["Remote"].ContainsKey("Protocol"))
					{
						data["Remote"].AddKey("Protocol", "FTP");
					}

					// Update config files without changelog keys
					if (!data["Remote"].ContainsKey("ChangelogURL"))
					{
						data["Remote"].AddKey("ChangelogURL", "http://directorate.asuscomm.com/launchpad/changelog/changelog.html");
					}

					// March 21 - 2016: Moves FTP url to its own section
					// March 21 - 2016: Renames FTP credential keys
					// March 21 - 2016: Adds sections for FTP, HTTP and BitTorrent.
					// March 21 - 2016: Adds configuration option for number of times to retry broken files
					if (data["Remote"].ContainsKey("FTPUsername"))
					{
						string username = data["Remote"].GetKeyData("FTPUsername").Value;
						data["Remote"].RemoveKey("FTPUsername");

						data["Remote"].AddKey("Username", username);

					}
					if (data["Remote"].ContainsKey("FTPPassword"))
					{
						string password = data["Remote"].GetKeyData("FTPPassword").Value;
						data["Remote"].RemoveKey("FTPPassword");

						data["Remote"].AddKey("Password", password);
					}

					if (!data.Sections.ContainsSection("FTP"))
					{
						data.Sections.AddSection("FTP");
					}

					if (data["Remote"].ContainsKey("FTPUrl"))
					{
						string ftpurl = data["Remote"].GetKeyData("FTPUrl").Value;
						data["Remote"].RemoveKey("FTPUrl");

						data["FTP"].AddKey("URL", ftpurl);
					}

					if (!data.Sections.ContainsSection("HTTP"))
					{
						data.Sections.AddSection("HTTP");
					}

					if (!data["HTTP"].ContainsKey("URL"))
					{
						data["HTTP"].AddKey("URL", "http://directorate.asuscomm.com/launchpad");
					}

					if (!data.Sections.ContainsSection("BitTorrent"))
					{
						data.Sections.AddSection("BitTorrent");
					}

					if (!data["BitTorrent"].ContainsKey("Magnet"))
					{
						data["BitTorrent"].AddKey("Magnet", "");
					}

					if (!data["Launchpad"].ContainsKey("bAllowAnonymousStats"))
					{
						data["Launchpad"].AddKey("bAllowAnonymousStats", "true");
					}

					if (!data["Remote"].ContainsKey("FileRetries"))
					{
						data["Remote"].AddKey("FileRetries", "2");
					}
					// End March 21 - 2016

					// ...
					WriteConfig(Parser, data);
				}
			}

			// Initialize the unique installation GUID, if needed.
			if (!File.Exists(GetInstallGUIDPath()))
			{
				// Make sure all the folders needed exist.
				string GUIDDirectoryPath = Path.GetDirectoryName(GetInstallGUIDPath());
				Directory.CreateDirectory(GUIDDirectoryPath);

				// Generate and store a GUID.
				string GeneratedGUID = Guid.NewGuid().ToString();
				File.WriteAllText(GetInstallGUIDPath(), GeneratedGUID);
			}
			else
			{
				// Make sure the GUID file has been populated
				FileInfo guidInfo = new FileInfo(GetInstallGUIDPath());
				if (guidInfo.Length <= 0)
				{
					// Generate and store a GUID.
					string GeneratedGUID = Guid.NewGuid().ToString();
					File.WriteAllText(GetInstallGUIDPath(), GeneratedGUID);
				}
			}
		}

		/// <summary>
		/// Generates a type-3 deterministic GUID for a specified seed string.
		/// The GUID is not designed to be cryptographically secure, nor is it
		/// designed for any use beyond simple generation of a GUID unique to a
		/// single game. If you use it for anything else, your code is bad and 
		/// you should feel bad.
		/// </summary>
		/// <returns>The seeded GUI.</returns>
		/// <param name="seed">Seed.</param>
		public static string GenerateSeededGUID(string seed)
		{
			using (MD5 md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(seed));
				return new Guid(hash).ToString();
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
			bool bCookieExists = File.Exists(GetUpdateCookiePath());
			if (!bCookieExists)
			{
				File.Create(GetUpdateCookiePath());

				return GetUpdateCookiePath();
			}
			else
			{
				return GetUpdateCookiePath();
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
			bool bCookieExists = File.Exists(GetInstallCookiePath());

			if (!bCookieExists)
			{
				File.Create(GetInstallCookiePath()).Close();

				return GetInstallCookiePath();
			}
			else
			{
				return GetInstallCookiePath();
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
		/// Gets the temporary launcher download directory.
		/// </summary>
		/// <returns>A full path to the directory.</returns>
		public static string GetTempLauncherDownloadDir()
		{
			return String.Format(@"{0}{1}launchpad{1}launcher", 
				Path.GetTempPath(), 
				Path.DirectorySeparatorChar);
		}

		// TODO: Move to ManifestHandler or FTPProtocolHandler
		/// <summary>
		/// Gets the manifests' path on disk.
		/// </summary>
		/// <returns>The manifest path.</returns>
		public static string GetManifestPath()
		{
			string manifestPath = String.Format(@"{0}LauncherManifest.txt", 
				                      GetLocalDir());
			return manifestPath;
		}

		// TODO: Move to ManifestHandler or FTPProtocolHandler

		/// <summary>
		/// Gets the old manifests' path on disk.
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
		public string GetGamePath()
		{
			return String.Format(@"{0}Game{2}{1}{2}", 
				GetLocalDir(),
				GetSystemTarget(),
				Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// Gets the path to the game executable.
		/// </summary>
		/// <returns>The game executable.</returns>
		public string GetGameExecutable()
		{
			string executablePathRootLevel;
			string executablePathTargetLevel;

			//unix doesn't need (or have!) the .exe extension.
			if (ChecksHandler.IsRunningOnUnix())
			{
				//should return something along the lines of "./Game/<ExecutableName>"
				executablePathRootLevel = String.Format(@"{0}{1}", 
					GetGamePath(), 
					GetGameName());

				//should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>"
				executablePathTargetLevel = String.Format(@"{0}{1}{3}Binaries{3}{2}{3}{1}", 
					GetGamePath(), 
					GetGameName(), 
					GetSystemTarget(),
					Path.DirectorySeparatorChar);
			}
			else
			{
				//should return something along the lines of "./Game/<ExecutableName>.exe"
				executablePathRootLevel = String.Format(@"{0}{1}.exe", 
					GetGamePath(), 
					GetGameName());

				//should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>.exe"
				executablePathTargetLevel = String.Format(@"{0}{1}{3}Binaries{3}{2}{3}{1}.exe", 
					GetGamePath(), 
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
				Console.WriteLine("Searched at: " + executablePathRootLevel);
				Console.WriteLine("Searched at: " + executablePathTargetLevel);
				throw new FileNotFoundException("The game executable could not be found.");
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
				Console.WriteLine("IOException in GetLocalGameVersion(): " + ioex.Message);
			}

			try
			{
				gameVersion = Version.Parse(rawGameVersion);
			}
			catch (ArgumentException aex)
			{
				Console.WriteLine("ArgumentException in GetLocalGameVersion(): " + aex.Message);
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
				                          GetGamePath());

			return localVersionPath;
		}

		/// <summary>
		/// Gets the custom launcher download URL.
		/// </summary>
		/// <returns>The custom launcher download URL.</returns>
		public string GetLauncherBinariesURL()
		{
			string launcherURL;
			if (GetDoOfficialUpdates())
			{
				launcherURL = String.Format("{0}/launcher/bin/", 
					"ftp://directorate.asuscomm.com");
			}
			else
			{
				launcherURL = String.Format("{0}/launcher/bin/", 
					GetBaseProtocolURL());
			}

			return launcherURL;
		}

		/// <summary>
		/// Gets the launcher version URL.
		/// </summary>
		/// <returns>The launcher version URL to either the official launchpad 
		/// binaries or a custom launcher, depending on the settings.</returns>
		public string GetLauncherVersionURL()
		{
			string versionURL;
			if (GetDoOfficialUpdates())
			{
				versionURL = String.Format("{0}/launcher/LauncherVersion.txt", 
					"ftp://directorate.asuscomm.com");
			}
			else
			{
				versionURL = String.Format("{0}/launcher/LauncherVersion.txt", 
					GetBaseProtocolURL());
			}

			return versionURL;
		}

		/// <summary>
		/// Gets the game URL.
		/// </summary>
		/// <returns>The game URL.</returns>
		public string GetGameURL()
		{
			return String.Format("{0}/game/{1}/bin/", 
				GetBaseProtocolURL(), 
				GetSystemTarget());
		}

		/// <summary>
		/// Gets the changelog URL.
		/// </summary>
		/// <returns>The changelog URL.</returns>
		public string GetChangelogURL()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string changelogURL = data["Remote"]["ChangelogURL"];

					return changelogURL;

				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetChangelogURL(): " + ioex.Message);
					return null;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine("ArgumentException in GetChangelogURL(): " + aex.Message);
					return null;
				}
			} 
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
					Console.WriteLine("ArgumentException in GetLauncherVersion(): " + aex.Message);
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
					IniData data = Parser.ReadFile(GetConfigPath());

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

		// TODO: More dynamic loading of protocols? Maybe even a plugin system?
		// Could use a static registry list in PatchProtocolHandler where plugin
		// protocols register their keys and types
		//
		// private static readonly List<ProtocolDescriptor> AvailableProtocols = new List<ProtocolDescriptor>();
		// ProtocolDescriptor protocol = new ProtocolDescriptor();
		// protocol.Key = "HyperspaceRTL";
		// protocol.Type = typeof(this);
		//
		// PatchProtocolHandler.RegisterProtocol(protocol);
		// PatchProtocolHandler.UnregisterProtocol(protocol);
		//
		/// <summary>
		/// Gets an instance of the desired patch protocol. Currently, FTP, HTTP and BitTorrent are supported.
		/// </summary>
		/// <returns>The patch protocol.</returns>
		public PatchProtocolHandler GetPatchProtocol()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string patchProtocol = data["Remote"]["Protocol"];

					switch (patchProtocol)
					{
						case "FTP":
							{
								return new FTPProtocolHandler();
							}
						case "HTTP":
							{
								return new HTTPProtocolHandler();
							}
						case "BitTorrent":
							{
								return new BitTorrentProtocolHandler();
							}
						default:
							{
								throw new NotImplementedException(String.Format("Protocol \"{0}\" was not recognized or implemented.", patchProtocol));
							}
					}
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchProtocol(): " + ioex.Message);
					return null;
				}
				catch (NotImplementedException nex)
				{
					Console.WriteLine("NotImplementedException in GetPatchProtocol(): " + nex.Message);
					return null;
				}
			}
		}

		/// <summary>
		/// Gets the set protocol string.
		/// </summary>
		/// <returns>The patch protocol.</returns>
		public string GetPatchProtocolString()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					return data["Remote"]["Protocol"];
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetPatchProtocolString(): " + ioex.Message);
					return null;
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
				lock (WriteLock)
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
					return ESystemTarget.Unknown;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine("ArgumentException in GetSystemTarget(): " + aex.Message);
					return ESystemTarget.Unknown;
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
				lock (WriteLock)
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
		}

		/// <summary>
		/// Gets the username for the remote service.
		/// </summary>
		/// <returns>The remote username.</returns>
		public string GetRemoteUsername()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string remoteUsername = data["Remote"]["Username"];

					return remoteUsername;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetRemoteUsername(): " + ioex.Message);
					return String.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the username for the remote service.
		/// </summary>
		/// <param name="Username">The remote username..</param>
		public void SetRemoteUsername(string Username)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Remote"]["Username"] = Username;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine("IOException in SetRemoteUsername(): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the password for the remote service.
		/// </summary>
		/// <returns>The remote password.</returns>
		public string GetRemotePassword()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string remotePassword = data["Remote"]["Password"];

					return remotePassword;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetRemotePassword(): " + ioex.Message);
					return String.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the password for the remote service.
		/// </summary>
		/// <param name="Password">The remote password.</param>
		public void SetRemotePassword(string Password)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Remote"]["Password"] = Password;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine("IOException in SetRemotePassword(): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the number of times the patching protocol should retry to download files.
		/// </summary>
		/// <returns>The number of file retries..</returns>
		public int GetFileRetries()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string fileRetries = data["Remote"]["FileRetries"];

					int retries;
					if (int.TryParse(fileRetries, out retries))
					{
						return retries;
					}
					else
					{
						return 0;
					}
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetRemotePassword(): " + ioex.Message);
					return 0;
				}
			}
		}

		/// <summary>
		/// Gets the base protocol URL.
		/// </summary>
		/// <returns>The base protocol URL.</returns>
		public string GetBaseProtocolURL()
		{
			switch (GetPatchProtocolString())
			{
				case "FTP":
					{
						return GetBaseFTPUrl();
					}
				case "HTTP":
					{
						return GetBaseHTTPUrl();
					}
				default:
					{
						return GetBaseFTPUrl();
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

					string FTPURL = data["FTP"]["URL"];

					return FTPURL;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetBaseFTPURL(): " + ioex.Message);
					return String.Empty;
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
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["FTP"]["URL"] = Url;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine("IOException in GetFTPPassword(): " + ioex.Message);				
					}
				}
			}
		}

		/// <summary>
		/// Gets the base HTTP URL.
		/// </summary>
		/// <returns>The base HTTP URL.</returns>
		public string GetBaseHTTPUrl()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string FTPURL = data["HTTP"]["URL"];

					return FTPURL;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetBaseHTTPUrl(): " + ioex.Message);
					return String.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the base HTTP URL.
		/// </summary>
		/// <param name="Url">The new URL.</param>
		public void SetBaseHTTPUrl(string Url)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["HTTP"]["URL"] = Url;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine("IOException in SetBaseHTTPUrl(): " + ioex.Message);				
					}
				}
			}
		}

		/// <summary>
		/// Gets the BitTorrent magnet link.
		/// </summary>
		/// <returns>The magnet link.</returns>
		public string GetBitTorrentMagnet()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string FTPURL = data["BitTorrent"]["Magnet"];

					return FTPURL;
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetBitTorrentMagnet(): " + ioex.Message);
					return String.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the BitTorrent magnet link.
		/// </summary>
		/// <param name="Magnet">The new magnet link.</param>
		public void SetBitTorrentMagnet(string Magnet)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["BitTorrent"]["Magnet"] = Magnet;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Console.WriteLine("IOException in SetBitTorrentMagnet(): " + ioex.Message);				
					}
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
					Console.WriteLine("IOException in GetDoOfficialUpdates(): " + ioex.Message);
					return true;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine("ArgumentException in GetDoOfficialUpdates(): " + aex.Message);
					return true;
				}
			}
		}

		/// <summary>
		/// Gets if the launcher is allowed to send usage stats.
		/// </summary>
		/// <returns><c>true</c>, if the launcher is allowed to send usage stats, <c>false</c> otherwise.</returns>
		public bool GetAllowAnonymousStats()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string rawAllowAnonymousStats = data["Launchpad"]["bAllowAnonymousStats"];

					return bool.Parse(rawAllowAnonymousStats);
				}
				catch (IOException ioex)
				{
					Console.WriteLine("IOException in GetAllowAnonymousStats(): " + ioex.Message);
					return true;
				}
				catch (ArgumentException aex)
				{
					Console.WriteLine("ArgumentException in GetAllowAnonymousStats(): " + aex.Message);
					return true;
				}
			}
		}

		/// <summary>
		/// Gets the launcher's unique GUID. This GUID maps to a game and not a user.
		/// </summary>
		/// <returns>The GUID.</returns>
		public string GetGameGUID()
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
					Console.WriteLine("IOException in GetGUID(): " + ioex.Message);
					return String.Empty;
				}
			}
		}


		/// <summary>
		/// Gets the path to the install-unique GUID.
		/// </summary>
		/// <returns>The install GUID path.</returns>
		public string GetInstallGUIDPath()
		{
			return String.Format("{0}/Launchpad/.installguid", 
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
		}

		/// <summary>
		/// Gets the install-unique GUID. This is separate from the launcher GUID, which maps to a game.
		/// </summary>
		/// <returns>The install GUI.</returns>
		public string GetInstallGUID()
		{			
			if (File.Exists(GetInstallGUIDPath()))
			{
				return File.ReadAllText(GetInstallGUIDPath());
			}
			else
			{
				return "";
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

							WriteConfig(Parser, data);
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

						WriteConfig(Parser, data);

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
		private static void ReplaceOldUpdateCookie()
		{
			string oldUpdateCookiePath = String.Format(@"{0}.updatecookie",
				                             GetLocalDir());

			if (File.Exists(oldUpdateCookiePath))
			{
				string updateCookiePath = String.Format(@"{0}.update", 
					                          GetLocalDir());

				File.Move(oldUpdateCookiePath, updateCookiePath);
			}
		}

		/// <summary>
		/// Gets the current platform the launcher is running on.
		/// </summary>
		/// <returns>The current platform.</returns>
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
						return ESystemTarget.Unknown;
					}
			}
		}
	}
}
