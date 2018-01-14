//
//  ConfigHandler.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using IniParser;
using IniParser.Model;

using Launchpad.Common;
using Launchpad.Common.Enums;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Handlers.Protocols.Manifest;
using log4net;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// TODO: Change to read-once config initialization instead of rereading the whole config each method call
	/// TODO: Allow for creation with a provided config data block
	/// Config handler. This class handles reading and writing to the launcher's configuration.
	/// Read and write operations are synchronized by locks, so it should be threadsafe.
	/// This is a singleton class, and it should always be accessed through <see cref="Instance"/>.
	/// </summary>
	public sealed class ConfigHandler
	{
		/*
			Constants for different default configuration options. Changing one option here should be reflected with
			a corresponding update block in the initialization.
		*/

		private const string DefaultGameName = "LaunchpadExample";
		private const string DefaultChangelogURL = "http://sharkman.asuscomm.com/launchpad/changelog/changelog.html";
		private const string DefaultProtocol = "FTP";
		private const string DefaultFileRetries = "2";
		private const string DefaultUsername = "anonymous";
		private const string DefaultPassword = "anonymous";
		private const string DefaultFTPAddress = "ftp://sharkman.asuscomm.com";
		private const string DefaultHTTPAddress = "http://sharkman.asuscomm.com/launchpad";
		private const string DefaultUseOfficialUpdates = "true";
		private const string DefaultAllowAnonymousStatus = "true";
		private const string DefaultBufferSize = "8192";

		private const string ConfigurationFolderName = "Config";
		private const string ConfigurationFileName = "LauncherConfig";
		private const string GameArgumentsFileName = "GameArguments";

		private const string SectionNameLocal = "Local";
		private const string SectionNameRemote = "Remote";
		private const string SectionNameFTP = "FTP";
		private const string SectionNameHTTP = "HTTP";
		private const string SectionNameBitTorrent = "BitTorrent";
		private const string SectionNameLaunchpad = "Launchpad";

		private const string LocalVersionKey = "LauncherVersion";
		private const string LocalGameNameKey = "GameName";
		private const string LocalSystemTargetKey = "SystemTarget";
		private const string LocalGameGUIDKey = "GUID";
		private const string LocalMainExecutableNameKey = "MainExecutableName";

		private const string RemoteChangelogURLKey = "ChangelogURL";
		private const string RemoteProtocolKey = "Protocol";
		private const string RemoteFileRetriesKey = "FileRetries";
		private const string RemoteUsernameKey = "Username";
		private const string RemotePasswordKey = "Password";
		private const string RemoteBufferSizeKey = "BufferSize";

		private const string FTPAddressKey = "URL";
		private const string HTTPAddressKey = "URL";

		private const string LaunchpadOfficialUpdatesKey = "bOfficialUpdates";
		private const string LaunchpadAnonymousStatsKey = "bAllowAnonymousStats";

		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigHandler));

		/// <summary>
		/// The config lock object.
		/// </summary>
		private readonly object ReadLock = new object();

		/// <summary>
		/// The write lock object.
		/// </summary>
		private readonly object WriteLock = new object();

		/// <summary>
		/// The singleton Instance. Will always point to one shared object.
		/// </summary>
		public static readonly ConfigHandler Instance = new ConfigHandler();

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigHandler"/> class and initalizes it.
		/// </summary>
		private ConfigHandler()
		{
			Initialize();
		}

		/// <summary>
		/// Writes the config data to disk. This method is thread-blocking, and all write operations
		/// are synchronized via lock(<see cref="WriteLock"/>).
		/// </summary>
		/// <param name="parser">The parser dealing with the current data.</param>
		/// <param name="data">The data which should be written to file.</param>
		private void WriteConfig(FileIniDataParser parser, IniData data)
		{
			lock (this.WriteLock)
			{
				parser.WriteFile(GetConfigPath(), data);
			}
		}

		/// <summary>
		/// Gets the expected path to the config file on disk.
		/// </summary>
		/// <returns>The config path.</returns>
		private static string GetConfigPath()
		{
			string configPath = $@"{GetConfigDir()}{ConfigurationFileName}.ini";

			return configPath;
		}

		/// <summary>
		/// Gets the expected path to the argument file on disk.
		/// </summary>
		private static string GetGameArgumentsPath()
		{
			return $"{GetConfigDir()}{GameArgumentsFileName}.txt";
		}

		/// <summary>
		/// Gets the path to the config directory.
		/// </summary>
		/// <returns>The config dir, terminated with a directory separator.</returns>
		private static string GetConfigDir()
		{
			string configDir = $@"{GetLocalDir()}{ConfigurationFolderName}{Path.DirectorySeparatorChar}";
			return configDir;
		}

		/// <summary>
		/// Initializes the config by checking for bad values or files.
		/// Run once when the launcher starts, then avoid unless absolutely neccesary.
		/// </summary>
		private void Initialize()
		{
			// Since Initialize will write to the config, we'll create the parser here and load the file later
			FileIniDataParser parser = new FileIniDataParser();

			string configDir = GetConfigDir();
			string configPath = GetConfigPath();

			// Get the launcher version from the assembly.
			Version defaultLauncherVersion = typeof(ConfigHandler).Assembly.GetName().Version;

			// Check for pre-unix config. If it exists, fix the values and move it.
			UpdateAndMovePreUnixConfig();

			// Check for old cookie files and update their names and contents.
			MoveOrUpdateCookieFiles();

			// Lock the configuration file to make sure no other threads will try and
			// read from it during creation or updating of values.
			lock (this.ReadLock)
			{
				if (!Directory.Exists(configDir))
				{
					Directory.CreateDirectory(configDir);
				}

				InitializeInstallationGUID();
				InitializeGameArgumentsFile();

				if (!File.Exists(configPath))
				{
					FileStream configStream = File.Create(configPath);
					configStream.Close();

					try
					{
						IniData data = parser.ReadFile(GetConfigPath());

						data.Sections.AddSection(SectionNameLocal);
						data.Sections.AddSection(SectionNameRemote);
						data.Sections.AddSection(SectionNameFTP);
						data.Sections.AddSection(SectionNameHTTP);
						data.Sections.AddSection(SectionNameBitTorrent);
						data.Sections.AddSection(SectionNameLaunchpad);

						data[SectionNameLocal].AddKey(LocalVersionKey, defaultLauncherVersion.ToString());
						data[SectionNameLocal].AddKey(LocalGameNameKey, DefaultGameName);
						data[SectionNameLocal].AddKey(LocalSystemTargetKey, GetCurrentPlatform().ToString());
						data[SectionNameLocal].AddKey(LocalGameGUIDKey, GenerateSeededGUID(DefaultGameName));
						data[SectionNameLocal].AddKey(LocalMainExecutableNameKey, DefaultGameName);

						data[SectionNameRemote].AddKey(RemoteChangelogURLKey, DefaultChangelogURL);
						data[SectionNameRemote].AddKey(RemoteProtocolKey, DefaultProtocol);
						data[SectionNameRemote].AddKey(RemoteFileRetriesKey, DefaultFileRetries);
						data[SectionNameRemote].AddKey(RemoteUsernameKey, DefaultUsername);
						data[SectionNameRemote].AddKey(RemotePasswordKey, DefaultPassword);
						data[SectionNameRemote].AddKey(RemoteBufferSizeKey, DefaultBufferSize);

						data[SectionNameFTP].AddKey(FTPAddressKey, DefaultFTPAddress);

						data[SectionNameHTTP].AddKey(HTTPAddressKey, DefaultHTTPAddress);

						data[SectionNameBitTorrent].AddKey("Magnet", string.Empty);

						data[SectionNameLaunchpad].AddKey(LaunchpadOfficialUpdatesKey, DefaultUseOfficialUpdates);
						data[SectionNameLaunchpad].AddKey(LaunchpadAnonymousStatsKey, DefaultAllowAnonymousStatus);

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Failed to create configuration file (IOException): " + ioex.Message);
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

					IniData data = parser.ReadFile(GetConfigPath());

					// Update the user-visible version of the launcher
					data[SectionNameLocal][LocalVersionKey] = defaultLauncherVersion.ToString();

					// ...

					// March 22 - 2016: Changed GUID generation to create a unique GUID for each game name
					// Update config files without GUID keys
					string seededGUID = GenerateSeededGUID(data[SectionNameLocal].GetKeyData(LocalGameNameKey).Value);
					if (!data[SectionNameLocal].ContainsKey(LocalGameGUIDKey))
					{
						data[SectionNameLocal].AddKey(LocalGameGUIDKey, seededGUID);
					}
					else
					{
						// Update the game GUID
						data[SectionNameLocal][LocalGameGUIDKey] = seededGUID;
					}
					// End March 22 - 2016

					// Update config files without protocol keys
					if (!data[SectionNameRemote].ContainsKey(RemoteProtocolKey))
					{
						data[SectionNameRemote].AddKey(RemoteProtocolKey, DefaultProtocol);
					}

					// Update config files without changelog keys
					if (!data[SectionNameRemote].ContainsKey(RemoteChangelogURLKey))
					{
						data[SectionNameRemote].AddKey(RemoteChangelogURLKey, DefaultChangelogURL);
					}

					// March 21 - 2016: Moves FTP url to its own section
					// March 21 - 2016: Renames FTP credential keys
					// March 21 - 2016: Adds sections for FTP, HTTP and BitTorrent.
					// March 21 - 2016: Adds configuration option for number of times to retry broken files
					if (data[SectionNameRemote].ContainsKey("FTPUsername"))
					{
						string username = data[SectionNameRemote].GetKeyData("FTPUsername").Value;
						data[SectionNameRemote].RemoveKey("FTPUsername");

						data[SectionNameRemote].AddKey(RemoteUsernameKey, username);
					}
					if (data[SectionNameRemote].ContainsKey("FTPPassword"))
					{
						string password = data[SectionNameRemote].GetKeyData("FTPPassword").Value;
						data[SectionNameRemote].RemoveKey("FTPPassword");

						data[SectionNameRemote].AddKey(RemotePasswordKey, password);
					}

					if (!data.Sections.ContainsSection(SectionNameFTP))
					{
						data.Sections.AddSection(SectionNameFTP);
					}

					if (data[SectionNameRemote].ContainsKey("FTPUrl"))
					{
						string ftpurl = data[SectionNameRemote].GetKeyData("FTPUrl").Value;
						data[SectionNameRemote].RemoveKey("FTPUrl");

						data[SectionNameFTP].AddKey(FTPAddressKey, ftpurl);
					}

					if (!data.Sections.ContainsSection(SectionNameHTTP))
					{
						data.Sections.AddSection(SectionNameHTTP);
					}

					if (!data[SectionNameHTTP].ContainsKey(HTTPAddressKey))
					{
						data[SectionNameHTTP].AddKey(HTTPAddressKey, "http://sharkman.asuscomm.com/launchpad");
					}

					if (!data.Sections.ContainsSection(SectionNameBitTorrent))
					{
						data.Sections.AddSection(SectionNameBitTorrent);
					}

					if (!data[SectionNameBitTorrent].ContainsKey("Magnet"))
					{
						data[SectionNameBitTorrent].AddKey("Magnet", string.Empty);
					}

					if (!data[SectionNameLaunchpad].ContainsKey("bAllowAnonymousStats"))
					{
						data[SectionNameLaunchpad].AddKey("bAllowAnonymousStats", "true");
					}

					if (!data[SectionNameRemote].ContainsKey(RemoteFileRetriesKey))
					{
						data[SectionNameRemote].AddKey(RemoteFileRetriesKey, "2");
					}
					// End March 21 - 2016

					// June 2 - 2016: Adds main executable redirection option
					if (!data[SectionNameLocal].ContainsKey("MainExecutuableName"))
					{
						string gameName = data[SectionNameLocal][LocalGameNameKey];
						data[SectionNameLocal].AddKey(LocalMainExecutableNameKey, gameName);
					}
					//End June 2

					// January 20 - 2017
					if (!data[SectionNameRemote].ContainsKey(RemoteBufferSizeKey))
					{
						data[SectionNameRemote].AddKey(RemoteBufferSizeKey, DefaultBufferSize);
					}
					// End January 20

					// ...
					WriteConfig(parser, data);
				}
			}
		}

		/// <summary>
		/// Creates a file with a unique GUID for the computer the launcher has been started on.
		/// If the file already exists, this method does nothing.
		/// </summary>
		private static void InitializeInstallationGUID()
		{
			// Initialize the unique installation GUID, if needed.
			if (!File.Exists(GetInstallGUIDPath()))
			{
				// Make sure all the folders needed exist.
				string installGUIDDirectoryPath = Path.GetDirectoryName(GetInstallGUIDPath());
				if (string.IsNullOrEmpty(installGUIDDirectoryPath))
				{
					Log.Error
					(
						"Could not get a valid path for the creation of the install GUID folder.\n" +
						"This is most likely due to a fault in the operating system."
					);
				}
				else
				{
					Directory.CreateDirectory(installGUIDDirectoryPath);
				}

				// Generate and store a GUID.
				string generatedInstallGUID = Guid.NewGuid().ToString();
				File.WriteAllText(GetInstallGUIDPath(), generatedInstallGUID);
			}
			else
			{
				// Make sure the GUID file has been populated
				FileInfo guidInfo = new FileInfo(GetInstallGUIDPath());
				if (guidInfo.Length <= 0)
				{
					// Generate and store a GUID.
					string generatedInstallGUID = Guid.NewGuid().ToString();
					File.WriteAllText(GetInstallGUIDPath(), generatedInstallGUID);
				}
			}
		}

		/// <summary>
		/// Creates a configuration file where the user or developer can add runtime switches for the installed game.
		/// If the file already exists, this method does nothing.
		/// </summary>
		private static void InitializeGameArgumentsFile()
		{
			// Initialize the game arguments file, if needed
			if (!File.Exists(GetGameArgumentsPath()))
			{
				using (FileStream fs = File.Create(GetGameArgumentsPath()))
				{
					using (StreamWriter sw = new StreamWriter(fs))
					{
						sw.WriteLine("# This file contains all the arguments passed to the game executable on startup.");
						sw.WriteLine("# Lines beginning with a hash character (#) are ignored and considered comments.");
						sw.WriteLine("# Everything else is passed line-by-line to the game executable on startup.");
						sw.WriteLine("# Multiple arguments can be on the same line in this file.");
						sw.WriteLine("# Each line will have a space appended at the end when passed to the game executable.");
						sw.WriteLine(string.Empty);
					}
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
		private static string GenerateSeededGUID(string seed)
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
		public static string GetLauncherCookiePath()
		{
			string updateCookie = $@"{GetLocalDir()}.launcher";
			return updateCookie;
		}

		/// <summary>
		/// Creates the update cookie.
		/// </summary>
		public static void CreateLauncherCookie()
		{
			bool bCookieExists = File.Exists(GetLauncherCookiePath());
			if (!bCookieExists)
			{
				File.Create(GetLauncherCookiePath());
			}
		}

		/// <summary>
		/// Gets the install cookie.
		/// </summary>
		/// <returns>The install cookie.</returns>
		public static string GetGameCookiePath()
		{
			string installCookie = $@"{GetLocalDir()}.game";
			return installCookie;
		}

		/// <summary>
		/// Creates the install cookie.
		/// </summary>
		public static void CreateGameCookie()
		{
			bool bCookieExists = File.Exists(GetGameCookiePath());

			if (!bCookieExists)
			{
				File.Create(GetGameCookiePath()).Close();
			}
		}

		/// <summary>
		/// Gets the local dir.
		/// </summary>
		/// <returns>The local dir, terminated by a directory separator.</returns>
		public static string GetLocalDir()
		{
			Uri codeBaseURI = new UriBuilder(Assembly.GetExecutingAssembly().Location).Uri;
			return Path.GetDirectoryName(Uri.UnescapeDataString(codeBaseURI.AbsolutePath)) + Path.DirectorySeparatorChar;
		}

		/// <summary>
		/// Gets the temporary launcher download directory.
		/// </summary>
		/// <returns>A full path to the directory.</returns>
		public static string GetTempLauncherDownloadPath()
		{
			return $@"{Path.GetTempPath()}{Path.DirectorySeparatorChar}launchpad{Path.DirectorySeparatorChar}launcher";
		}

		/// <summary>
		/// Gets the game path.
		/// </summary>
		/// <returns>The game path, terminated by a directory separator.</returns>
		public string GetGamePath()
		{
			return $@"{GetLocalDir()}Game{Path.DirectorySeparatorChar}{GetSystemTarget()}{Path.DirectorySeparatorChar}";
		}

		/// <summary>
		/// Gets a list of command-line arguments that are passed to the game when it starts.
		/// </summary>
		/// <returns>The arguments.</returns>
		public static IEnumerable<string> GetGameArguments()
		{
			if (!File.Exists(GetGameArgumentsPath()))
			{
				return new List<string>();
			}

			List<string> gameArguments = new List<string>(File.ReadAllLines(GetGameArgumentsPath()));

			// Return the list of lines in the argument file, except the ones starting with a hash or empty lines
			return gameArguments.Where(s => !s.StartsWith("#") && !string.IsNullOrEmpty(s)).ToList();
		}

		/// <summary>
		/// Gets the path to the game executable.
		/// </summary>
		/// <returns>The game executable.</returns>
		/// <exception cref="FileNotFoundException">
		/// A FileNotFoundException will be thrown if no file exists at the searched file paths.
		/// </exception>
		public string GetGameExecutable()
		{
			string executablePathRootLevel;
			string executablePathTargetLevel;

			// While not recommended nor supported, the user may add an executable extension to the executable name.
			// We strip it out here (if it exists) just to be safe.
			string executableName = GetMainExecutableName().Replace(".exe", string.Empty);

			// Unix doesn't need (or have) the .exe extension.
			if (SystemInformation.IsRunningOnUnix())
			{
				// Should return something along the lines of "./Game/<ExecutableName>"
				executablePathRootLevel = $@"{GetGamePath()}{executableName}";

				// Should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>"
				executablePathTargetLevel =
					$@"{GetGamePath()}{GetGameName()}{Path.DirectorySeparatorChar}Binaries" +
					$"{Path.DirectorySeparatorChar}{GetSystemTarget()}{Path.DirectorySeparatorChar}{executableName}";
			}
			else
			{
				// Should return something along the lines of "./Game/<ExecutableName>.exe"
				executablePathRootLevel = $@"{GetGamePath()}{executableName}.exe";

				// Should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>.exe"
				executablePathTargetLevel =
					$@"{GetGamePath()}{GetGameName()}{Path.DirectorySeparatorChar}Binaries" +
					$"{Path.DirectorySeparatorChar}{GetSystemTarget()}{Path.DirectorySeparatorChar}{executableName}.exe";
			}

			if (File.Exists(executablePathRootLevel))
			{
				return executablePathRootLevel;
			}

			if (File.Exists(executablePathTargetLevel))
			{
				return executablePathTargetLevel;
			}

			Log.Warn("Could not find the game executable. " +
				"\n\tSearched at: " + executablePathRootLevel +
				"\n\tSearched at: " + executablePathTargetLevel);

			throw new FileNotFoundException("The game executable could not be found.");
		}

		/// <summary>
		/// Gets the local game version.
		/// </summary>
		/// <returns>The local game version.</returns>
		public Version GetLocalGameVersion()
		{
			try
			{
				string rawGameVersion = File.ReadAllText(GetGameVersionPath());

				if (Version.TryParse(rawGameVersion, out var gameVersion))
				{
					return gameVersion;
				}

				Log.Warn("Could not parse local game version. Contents: " + rawGameVersion);
				return new Version("0.0.0");
			}
			catch (IOException ioex)
			{
				Log.Warn("Could not read local game version (IOException): " + ioex.Message);
				return null;
			}
		}

		/// <summary>
		/// Gets the game version path.
		/// </summary>
		/// <returns>The game version path.</returns>
		public string GetGameVersionPath()
		{
			string localVersionPath = $@"{GetGamePath()}GameVersion.txt";

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
				launcherURL = $"{GetOfficialBaseProtocolURL()}/launcher/bin/";
			}
			else
			{
				launcherURL = $"{GetBaseProtocolURL()}/launcher/bin/";
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
				versionURL = $"{GetOfficialBaseProtocolURL()}/launcher/LauncherVersion.txt";
			}
			else
			{
				versionURL = $"{GetBaseProtocolURL()}/launcher/LauncherVersion.txt";
			}

			return versionURL;
		}

		/// <summary>
		/// Gets the game URL.
		/// </summary>
		/// <returns>The game URL.</returns>
		public string GetGameURL()
		{
			return $"{GetBaseProtocolURL()}/game/{GetSystemTarget()}/bin/";
		}

		/// <summary>
		/// Gets the changelog URL.
		/// </summary>
		/// <returns>The changelog URL.</returns>
		public string GetChangelogURL()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string changelogURL = data[SectionNameRemote][RemoteChangelogURLKey];
					return changelogURL;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read changelog URL (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string launcherVersion = data[SectionNameLocal][LocalVersionKey];

					if (Version.TryParse(launcherVersion, out var localLauncherVersion))
					{
						return localLauncherVersion;
					}

					Log.Warn("Failed to parse local launcher version. Returning default version of 0.0.0.");
					return new Version("0.0.0");
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read local launcher version (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string gameName = data[SectionNameLocal][LocalGameNameKey];

					return gameName;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the game name (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Gets an instance of the desired patch protocol. Currently, FTP, HTTP and BitTorrent are supported.
		/// </summary>
		/// <returns>The patch protocol.</returns>
		public PatchProtocolHandler GetPatchProtocol()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string patchProtocol = data[SectionNameRemote][RemoteProtocolKey];

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
							Log.Error($"Failed to load protocol handler: Protocol \"{patchProtocol}\" was not recognized or implemented.");
							return null;
						}
					}
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read desired protocol (IOException): " + ioex.Message);
					return null;
				}
			}
		}

		/// <summary>
		/// Gets the set protocol string.
		/// </summary>
		/// <returns>The patch protocol.</returns>
		private string GetPatchProtocolString()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					return data[SectionNameRemote][RemoteProtocolKey];
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read the protocol string (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the name of the game.
		/// </summary>
		/// <param name="gameName">Game name.</param>
		public void SetGameName(string gameName)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameLocal][LocalGameNameKey] = gameName;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the game name (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string rawSystemTarget = data[SectionNameLocal][LocalSystemTargetKey];

					if (Enum.TryParse(rawSystemTarget, out ESystemTarget systemTarget))
					{
						return systemTarget;
					}

					Log.Warn("Could not parse the system target. Installation of games will not be possible.");

					return ESystemTarget.Unknown;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the system target (IOException): " + ioex.Message);
					return ESystemTarget.Unknown;
				}
			}
		}

		/// <summary>
		/// Sets the system target.
		/// </summary>
		/// <param name="systemTarget">System target.</param>
		public void SetSystemTarget(ESystemTarget systemTarget)
		{
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameLocal][LocalSystemTargetKey] = systemTarget.ToString();

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the system target (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string remoteUsername = data[SectionNameRemote][RemoteUsernameKey];

					return remoteUsername;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the remote username (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the username for the remote service.
		/// </summary>
		/// <param name="username">The remote username.</param>
		public void SetRemoteUsername(string username)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameRemote][RemoteUsernameKey] = username;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the remote username (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string remotePassword = data[SectionNameRemote][RemotePasswordKey];

					return remotePassword;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the remote password (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the password for the remote service.
		/// </summary>
		/// <param name="password">The remote password.</param>
		public void SetRemotePassword(string password)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameRemote][RemotePasswordKey] = password;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the remote password (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the number of times the patching protocol should retry to download files.
		/// </summary>
		/// <returns>The number of file retries.</returns>
		public int GetFileRetries()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string fileRetries = data[SectionNameRemote][RemoteFileRetriesKey];

					if (int.TryParse(fileRetries, out var retries))
					{
						return retries;
					}

					return 0;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the maximum file retries (IOException): " + ioex.Message);
					return 0;
				}
			}
		}

		/// <summary>
		/// Gets the size of the download buffer that should be allocated for remote files.
		/// </summary>
		/// <returns>The buffer size.</returns>
		public int GetDownloadBufferSize()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string fileRetries = data[SectionNameRemote][RemoteBufferSizeKey];

					if (int.TryParse(fileRetries, out var retries))
					{
						return retries;
					}

					return int.Parse(DefaultBufferSize);
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the download bufferr size (IOException): " + ioex.Message);
					return int.Parse(DefaultBufferSize);
				}
			}
		}

		/// <summary>
		/// Gets the base protocol URL.
		/// </summary>
		/// <returns>The base protocol URL.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the protocol set in the configuration file is not a valid value.
		/// </exception>
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
					throw new ArgumentOutOfRangeException(nameof(GetPatchProtocolString), null,
						"Invalid protocol set in the configuration file.");
				}
			}
		}

		/// <summary>
		/// Gets the official Launchpad base protocol URL.
		/// </summary>
		/// <returns>The official base protocol url.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Will be thrown if the protocol set in the configuration file is not a valid value.
		/// </exception>
		private string GetOfficialBaseProtocolURL()
		{
			switch (GetPatchProtocolString())
			{
				case "FTP":
				{
					return "ftp://sharkman.asuscomm.com";
				}
				case "HTTP":
				{
					return "http://sharkman.asuscomm.com/launchpad";
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(GetPatchProtocolString), null,
						"Invalid protocol set in the configuration file.");
				}
			}
		}

		/// <summary>
		/// Gets the base FTP URL.
		/// </summary>
		/// <returns>The base FTP URL.</returns>
		public string GetBaseFTPUrl()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = parser.ReadFile(configPath);

					string url = data[SectionNameFTP][FTPAddressKey];

					return url;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the base FTP URL (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the base FTP URL.
		/// </summary>
		/// <param name="url">URL.</param>
		public void SetBaseFTPUrl(string url)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameFTP][FTPAddressKey] = url;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the base FTP URL (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = parser.ReadFile(configPath);

					string url = data[SectionNameHTTP][HTTPAddressKey];

					return url;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the base HTTP URL (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the base HTTP URL.
		/// </summary>
		/// <param name="url">The new URL.</param>
		public void SetBaseHTTPUrl(string url)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameHTTP][HTTPAddressKey] = url;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the base HTTP URL (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = parser.ReadFile(configPath);

					string magnetLink = data[SectionNameBitTorrent]["Magnet"];

					return magnetLink;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the BitTorrent magnet link (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the BitTorrent magnet link.
		/// </summary>
		/// <param name="magnet">The new magnet link.</param>
		public void SetBitTorrentMagnet(string magnet)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameBitTorrent]["Magnet"] = magnet;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the BitTorrent magnet link (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the name of the main executable.
		/// </summary>
		/// <returns>The name of the main executable.</returns>
		private string GetMainExecutableName()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = parser.ReadFile(configPath);

					string mainExecutableName = data[SectionNameLocal][LocalMainExecutableNameKey];

					return mainExecutableName;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the main executable name (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the name of the main executable.
		/// </summary>
		/// <param name="mainExecutableName">The new main executable name.</param>
		public void SetMainExecutableName(string mainExecutableName)
		{
			lock (this.ReadLock)
			{
				lock (this.WriteLock)
				{
					try
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						data[SectionNameLocal][LocalMainExecutableNameKey] = mainExecutableName;

						WriteConfig(parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the main executable name (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string rawDoOfficialUpdates = data[SectionNameLaunchpad]["bOfficialUpdates"];

					if (bool.TryParse(rawDoOfficialUpdates, out var doOfficialUpdates))
					{
						return doOfficialUpdates;
					}

					Log.Warn("Could not parse if we should use official updates. Allowing by default.");
					return true;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not determine if we should use official updates (IOException): " + ioex.Message);
					return true;
				}
			}
		}

		/// <summary>
		/// Gets if the launcher is allowed to send usage stats.
		/// </summary>
		/// <returns><c>true</c>, if the launcher is allowed to send usage stats, <c>false</c> otherwise.</returns>
		public bool ShouldAllowAnonymousStats()
		{
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string rawAllowAnonymousStats = data[SectionNameLaunchpad]["bAllowAnonymousStats"];

					if (bool.TryParse(rawAllowAnonymousStats, out var allowAnonymousStats))
					{
						return allowAnonymousStats;
					}

					Log.Warn("Could not parse if we were allowed to send anonymous stats. Allowing by default.");
					return true;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not determine if we were allowed to send anonymous stats (IOException): " + ioex.Message);
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
			lock (this.ReadLock)
			{
				try
				{
					FileIniDataParser parser = new FileIniDataParser();
					IniData data = parser.ReadFile(GetConfigPath());

					string guid = data[SectionNameLocal][LocalGameGUIDKey];

					return guid;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not load the game GUID (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Gets the path to the install-unique GUID.
		/// </summary>
		/// <returns>The install GUID path.</returns>
		private static string GetInstallGUIDPath()
		{
			return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Launchpad/.installguid";
		}

		/// <summary>
		/// Gets the install-unique GUID. This is separate from the launcher GUID, which maps to a game.
		/// </summary>
		/// <returns>The install GUI.</returns>
		public static string GetInstallGUID()
		{
			if (File.Exists(GetInstallGUIDPath()))
			{
				return File.ReadAllText(GetInstallGUIDPath());
			}

			return string.Empty;
		}

		/// <summary>
		/// Replaces and updates the old pre-unix config.
		/// </summary>
		private void UpdateAndMovePreUnixConfig()
		{
			string oldConfigPath = $@"{GetLocalDir()}config{Path.DirectorySeparatorChar}launcherConfig.ini";

			string oldConfigDir = $@"{GetLocalDir()}config";

			if (SystemInformation.IsRunningOnUnix())
			{
				// Case sensitive
				// Is there an old config file?
				if (File.Exists(oldConfigPath))
				{
					lock (this.ReadLock)
					{
						// Have we not already created the new config dir?
						if (!Directory.Exists(GetConfigDir()))
						{
							// If not, create it.
							Directory.CreateDirectory(GetConfigDir());

							// Copy the old config file to the new location.
							File.Copy(oldConfigPath, GetConfigPath());

							// Read our new file.
							FileIniDataParser parser = new FileIniDataParser();
							IniData data = parser.ReadFile(GetConfigPath());

							// Replace the old invalid keys with new, updated keys.
							string launcherVersion = data[SectionNameLocal]["launcherVersion"];
							string gameName = data[SectionNameLocal]["gameName"];
							string systemTarget = data[SectionNameLocal]["systemTarget"];

							data[SectionNameLocal].RemoveKey("launcherVersion");
							data[SectionNameLocal].RemoveKey("gameName");
							data[SectionNameLocal].RemoveKey("systemTarget");

							data[SectionNameLocal].AddKey(LocalVersionKey, launcherVersion);
							data[SectionNameLocal].AddKey(LocalGameNameKey, gameName);
							data[SectionNameLocal].AddKey(LocalSystemTargetKey, systemTarget);

							WriteConfig(parser, data);

							File.Delete(oldConfigPath);
							Directory.Delete(oldConfigDir, true);
						}
						else
						{
							// The new config dir already exists, so we'll just toss out the old one.
							// Delete the old config
							File.Delete(oldConfigPath);
							Directory.Delete(oldConfigDir, true);
						}
					}
				}
			}
			else
			{
				lock (this.ReadLock)
				{
					// Windows is not case sensitive, so we'll use direct access without copying.
					if (File.Exists(oldConfigPath))
					{
						FileIniDataParser parser = new FileIniDataParser();
						IniData data = parser.ReadFile(GetConfigPath());

						// Replace the old invalid keys with new, updated keys.
						string launcherVersion = data[SectionNameLocal]["launcherVersion"];
						string gameName = data[SectionNameLocal]["gameName"];
						string systemTarget = data[SectionNameLocal]["systemTarget"];

						data[SectionNameLocal].RemoveKey("launcherVersion");
						data[SectionNameLocal].RemoveKey("gameName");
						data[SectionNameLocal].RemoveKey("systemTarget");

						data[SectionNameLocal].AddKey(LocalVersionKey, launcherVersion);
						data[SectionNameLocal].AddKey(LocalGameNameKey, gameName);
						data[SectionNameLocal].AddKey(LocalSystemTargetKey, systemTarget);

						WriteConfig(parser, data);
					}
				}
			}
		}

		/// <summary>
		/// Renames or moves the old cookie files as needed.
		/// </summary>
		private static void MoveOrUpdateCookieFiles()
		{
			// This is the really old path from way back.
			string veryOldUpdateCookiePath = $@"{GetLocalDir()}.updatecookie";
			if (File.Exists(veryOldUpdateCookiePath))
			{
				string launcherInstallCookiePath = $@"{GetLocalDir()}.launcher";

				File.Move(veryOldUpdateCookiePath, launcherInstallCookiePath);
			}

			// August 1 - 2016: Renamed update cookie to launcher cookie
			string oldUpdateCookiePath = $@"{GetLocalDir()}.update";
			if (File.Exists(oldUpdateCookiePath))
			{
				string launcherInstallCookiePath = $@"{GetLocalDir()}.launcher";

				File.Move(oldUpdateCookiePath, launcherInstallCookiePath);
			}

			// August 1 - 2016: Renamed install cookie to game cookie
			string oldInstallCookiePath = $@"{GetLocalDir()}.install";
			if (File.Exists(oldInstallCookiePath))
			{
				string gameInstallCookiePath = $@"{GetLocalDir()}.game";

				File.Move(oldInstallCookiePath, gameInstallCookiePath);
			}
			// End August 1 - 2016
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
						// Mac may sometimes be detected as Unix, so do an additional check for some Mac-only directories
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
