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
using Config.Net;
using IniParser;
using IniParser.Model;

using Launchpad.Common;
using Launchpad.Common.Enums;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Handlers.Protocols.Manifest;
using log4net;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// Config handler.
	/// This is a singleton class, and it should always be accessed through <see cref="Instance"/>.
	/// </summary>
	public sealed class ConfigHandler
	{
		private const string OfficialBaseAddress = "ftp://sharkman.asuscomm.com";

		private const string ConfigurationFolderName = "Config";
		private const string ConfigurationFileName = "LauncherConfig";
		private const string GameArgumentsFileName = "GameArguments";

		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigHandler));

		/// <summary>
		/// The singleton Instance. Will always point to one shared object.
		/// </summary>
		public static readonly ConfigHandler Instance = new ConfigHandler();

		/// <summary>
		/// Gets the configuration instance.
		/// </summary>
		public ILaunchpadConfiguration Configuration { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigHandler"/> class and initalizes it.
		/// </summary>
		private ConfigHandler()
		{
			this.Configuration = new ConfigurationBuilder<ILaunchpadConfiguration>()
				.UseIniFile(GetConfigPath())
				.Build();

			InitializeConfigurationFile();
			InitializeGameArgumentsFile();
		}

		/// <summary>
		/// Gets the expected path to the config file on disk.
		/// </summary>
		/// <returns>The config path.</returns>
		private static string GetConfigPath()
		{
			return Path.Combine(GetConfigDir(), $"{ConfigurationFileName}.ini");
		}

		/// <summary>
		/// Gets the expected path to the argument file on disk.
		/// </summary>
		private static string GetGameArgumentsPath()
		{
			return Path.Combine(GetConfigDir(), $"{GameArgumentsFileName}.txt");
		}

		/// <summary>
		/// Gets the path to the config directory.
		/// </summary>
		/// <returns>The config dir, terminated with a directory separator.</returns>
		private static string GetConfigDir()
		{
			return Path.Combine(GetLocalLauncherDirectory(), ConfigurationFolderName);
		}

		/// <summary>
		/// Initializes the config by checking for bad values or files.
		/// Run once when the launcher starts, then avoid unless absolutely neccesary.
		/// </summary>
		private void InitializeConfigurationFile()
		{
			if (File.Exists(GetConfigPath()))
			{
				return;
			}

			// Get the default values and write them back to the file, forcing it to be written to disk
			foreach (var property in typeof(ILaunchpadConfiguration).GetProperties())
			{
				var value = property.GetValue(this.Configuration);
				property.SetValue(this.Configuration, value);
			}
		}

		/// <summary>
		/// Creates a configuration file where the user or developer can add runtime switches for the installed game.
		/// If the file already exists, this method does nothing.
		/// </summary>
		private static void InitializeGameArgumentsFile()
		{
			// Initialize the game arguments file, if needed
			if (File.Exists(GetGameArgumentsPath()))
			{
				return;
			}

			using (var fs = File.Create(GetGameArgumentsPath()))
			{
				using (var sw = new StreamWriter(fs))
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

		/// <summary>
		/// Gets the path to the update cookie on disk.
		/// </summary>
		/// <returns>The update cookie.</returns>
		public static string GetLauncherCookiePath()
		{
			return Path.Combine(GetLocalLauncherDirectory(), ".launcher");
		}

		/// <summary>
		/// Creates the update cookie.
		/// </summary>
		public static void CreateLauncherCookie()
		{
			var doesCookieExist = File.Exists(GetLauncherCookiePath());
			if (!doesCookieExist)
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
			return Path.Combine(GetLocalLauncherDirectory(), ".game");
		}

		/// <summary>
		/// Creates the install cookie.
		/// </summary>
		public static void CreateGameCookie()
		{
			var doesCookieExist = File.Exists(GetGameCookiePath());
			if (!doesCookieExist)
			{
				File.Create(GetGameCookiePath()).Close();
			}
		}

		/// <summary>
		/// Gets the local directory where the launcher is stored.
		/// </summary>
		/// <returns>The local directory.</returns>
		public static string GetLocalLauncherDirectory()
		{
			var codeBaseURI = new UriBuilder(Assembly.GetExecutingAssembly().Location).Uri;
			return Path.GetDirectoryName(Uri.UnescapeDataString(codeBaseURI.AbsolutePath));
		}

		/// <summary>
		/// Gets the temporary launcher download directory.
		/// </summary>
		/// <returns>A full path to the directory.</returns>
		public static string GetTempLauncherDownloadPath()
		{
			return Path.Combine(Path.GetTempPath(), "launchpad", "launcher");
		}

		/// <summary>
		/// Gets the game path.
		/// </summary>
		/// <returns>The game path</returns>
		public string GetLocalGamePath()
		{
			return Path.Combine(GetLocalLauncherDirectory(), "Game", this.Configuration.SystemTarget.ToString());
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

			var gameArguments = new List<string>(File.ReadAllLines(GetGameArgumentsPath()));

			// Return the list of lines in the argument file, except the ones starting with a hash or empty lines
			return gameArguments.Where(s => !s.StartsWith("#") && !string.IsNullOrEmpty(s)).ToList();
		}

		/// <summary>
		/// Gets the local game version.
		/// </summary>
		/// <returns>The local game version.</returns>
		public Version GetLocalGameVersion()
		{
			try
			{
				var rawGameVersion = File.ReadAllText(GetLocalGameVersionPath());

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
		public string GetLocalGameVersionPath()
		{
			return Path.Combine(GetLocalGamePath(), "GameVersion.txt");
		}

		/// <summary>
		/// Gets the remote path to where launcher binaries are stored.
		/// </summary>
		/// <returns>The path.</returns>
		public string GetRemoteLauncherBinariesPath()
		{
			string launcherURL;
			if (this.Configuration.UseOfficialUpdates)
			{
				launcherURL = $"{OfficialBaseAddress}/launcher/bin/";
			}
			else
			{
				launcherURL = $"{this.Configuration.RemoteAddress}/launcher/bin/";
			}

			return launcherURL;
		}

		/// <summary>
		/// Gets the remote path of the launcher version.
		/// </summary>
		/// <returns>
		/// The path to either the official launchpad binaries or a custom launcher, depending on the settings.
		/// </returns>
		public string GetRemoteLauncherVersionPath()
		{
			string versionURL;
			if (this.Configuration.UseOfficialUpdates)
			{
				versionURL = $"{OfficialBaseAddress}/launcher/LauncherVersion.txt";
			}
			else
			{
				versionURL = $"{this.Configuration.RemoteAddress}/launcher/LauncherVersion.txt";
			}

			return versionURL;
		}

		/// <summary>
		/// Gets the remote path where the game is stored..
		/// </summary>
		/// <returns>The path.</returns>
		public string GetRemoteGamePath()
		{
			return $"{this.Configuration.RemoteAddress}/game/{this.Configuration.SystemTarget}/bin/";
		}

		/// <summary>
		/// Gets the current platform the launcher is running on.
		/// </summary>
		/// <returns>The current platform.</returns>
		public static ESystemTarget GetCurrentPlatform()
		{
			var platformID = Environment.OSVersion.Platform.ToString();
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

					return ESystemTarget.Linux;
				}
				case "Windows":
				{
					return Environment.Is64BitOperatingSystem ? ESystemTarget.Win64 : ESystemTarget.Win32;
				}
				default:
				{
					return ESystemTarget.Unknown;
				}
			}
		}

		/// <summary>
		/// Gets the local launcher version.
		/// </summary>
		/// <returns>The version.</returns>
		public static Version GetLocalLauncherVersion()
		{
			return typeof(ConfigHandler).Assembly.GetName().Version;
		}
	}
}
