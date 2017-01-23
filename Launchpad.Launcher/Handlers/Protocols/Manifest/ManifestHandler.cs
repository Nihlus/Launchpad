//
//  ManifestHandler.cs
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

using System.Collections.Generic;
using System.IO;
using log4net;

namespace Launchpad.Launcher.Handlers.Protocols.Manifest
{
	internal sealed class ManifestHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(ManifestHandler));

		public static readonly ManifestHandler Instance = new ManifestHandler();

		private readonly object GameManifestLock = new object();
		private readonly object OldGameManifestLock = new object();

		private readonly object LaunchpadManifestLock = new object();
		private readonly object OldLaunchpadManifestLock = new object();

		/// <summary>
		/// The config handler reference.
		/// </summary>
		private static readonly ConfigHandler Config = ConfigHandler.Instance;

		private readonly List<ManifestEntry> gameManifest = new List<ManifestEntry>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ManifestHandler"/> class.
		/// This constructor also serves to updated outdated file paths for the manifests.
		/// </summary>
		private ManifestHandler()
		{
			ReplaceDeprecatedManifest();
		}

		/// <summary>
		/// Gets the game manifest. Call sparsely, as it loads the entire manifest from disk each time
		/// this property is accessed.
		/// </summary>
		/// <value>The manifest.</value>
		public List<ManifestEntry> GameManifest
		{
			get
			{
				LoadGameManifest();
				return this.gameManifest;
			}
		}

		private readonly List<ManifestEntry> oldGameManifest = new List<ManifestEntry>();

		/// <summary>
		/// Gets the old game manifest. Call sparsely, as it loads the entire manifest from disk each time
		/// this property is accessed.
		/// </summary>
		/// <value>The old manifest.</value>
		public List<ManifestEntry> OldGameManifest
		{
			get
			{
				LoadOldGameManifest();
				return this.oldGameManifest;
			}
		}

		private readonly List<ManifestEntry> launchpadManifest = new List<ManifestEntry>();
		public List<ManifestEntry> LaunchpadManifest
		{
			get
			{
				LoadLaunchpadManifest();
				return this.launchpadManifest;
			}
		}

		private readonly List<ManifestEntry> oldLaunchpadManifest = new List<ManifestEntry>();
		public List<ManifestEntry> OldLaunchpadManifest
		{
			get
			{
				LoadOldLaunchpadManifest();
				return this.oldLaunchpadManifest;
			}
		}

		/// <summary>
		/// Loads the game manifest from disk.
		/// </summary>
		private void LoadGameManifest()
		{
			try
			{
				lock (this.GameManifestLock)
				{
					if (!File.Exists(GetGameManifestPath()))
					{
						return;
					}

					this.gameManifest.Clear();

					string[] rawGameManifest = File.ReadAllLines(GetGameManifestPath());
					foreach (string rawEntry in rawGameManifest)
					{
						ManifestEntry newEntry;
						if (ManifestEntry.TryParse(rawEntry, out newEntry))
						{
							this.gameManifest.Add(newEntry);
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Could not load game manifest (IOException): " + ioex.Message);
			}
		}

		/// <summary>
		/// Loads the old game manifest from disk.
		/// </summary>
		private void LoadOldGameManifest()
		{
			try
			{
				lock (this.OldGameManifestLock)
				{
					if (!File.Exists(GetOldGameManifestPath()))
					{
						return;
					}

					this.oldGameManifest.Clear();

					string[] rawOldGameManifest = File.ReadAllLines(GetOldGameManifestPath());
					foreach (string rawEntry in rawOldGameManifest)
					{
						ManifestEntry newEntry;
						if (ManifestEntry.TryParse(rawEntry, out newEntry))
						{
							this.oldGameManifest.Add(newEntry);
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Could not load old game manifest (IOException): " + ioex.Message);
			}
		}


		/// <summary>
		/// Loads the launchpad manifest from disk.
		/// </summary>
		private void LoadLaunchpadManifest()
		{
			try
			{
				lock (this.LaunchpadManifestLock)
				{
					if (!File.Exists(GetLaunchpadManifestPath()))
					{
						return;
					}

					this.launchpadManifest.Clear();

					string[] rawLaunchpadManifest = File.ReadAllLines(GetLaunchpadManifestPath());
					foreach (string rawEntry in rawLaunchpadManifest)
					{
						ManifestEntry newEntry;
						if (ManifestEntry.TryParse(rawEntry, out newEntry))
						{
							this.launchpadManifest.Add(newEntry);
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Could not load launcher manifest (IOException): " + ioex.Message);
			}
		}

		/// <summary>
		/// Loads the old launchpad manifest from disk.
		/// </summary>
		private void LoadOldLaunchpadManifest()
		{
			try
			{
				lock (this.OldLaunchpadManifestLock)
				{
					if (!File.Exists(GetOldGameManifestPath()))
					{
						return;
					}

					this.oldLaunchpadManifest.Clear();

					string[] rawOldLaunchpadManifest = File.ReadAllLines(GetOldGameManifestPath());
					foreach (string rawEntry in rawOldLaunchpadManifest)
					{
						ManifestEntry newEntry;
						if (ManifestEntry.TryParse(rawEntry, out newEntry))
						{
							this.oldLaunchpadManifest.Add(newEntry);
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Could not load old launcher manifest (IOException): " + ioex.Message);
			}
		}

		/// <summary>
		/// Gets the game manifests' path on disk.
		/// </summary>
		/// <returns>The game manifest path.</returns>
		public static string GetGameManifestPath()
		{
			string manifestPath = $@"{ConfigHandler.GetLocalDir()}GameManifest.txt";
			return manifestPath;
		}

		/// <summary>
		/// Gets the old game manifests' path on disk.
		/// </summary>
		/// <returns>The old game manifest's path.</returns>
		public static string GetOldGameManifestPath()
		{
			string oldManifestPath = $@"{ConfigHandler.GetLocalDir()}GameManifest.txt.old";
			return oldManifestPath;
		}

		/// <summary>
		/// Gets the launchpad manifests' path on disk.
		/// </summary>
		/// <returns>The launchpad manifest path.</returns>
		public static string GetLaunchpadManifestPath()
		{
			string manifestPath = $@"{ConfigHandler.GetLocalDir()}LaunchpadManifest.txt";
			return manifestPath;
		}

		/// <summary>
		/// Gets the old launchpad manifests' path on disk.
		/// </summary>
		/// <returns>The old launchpad manifest's path.</returns>
		public static string GetOldLaunchpadManifestPath()
		{
			string oldManifestPath = $@"{ConfigHandler.GetLocalDir()}LaunchpadManifest.txt.old";
			return oldManifestPath;
		}

		/// <summary>
		/// Gets the deprecated manifests' path on disk.
		/// </summary>
		/// <returns>The deprecated manifest path.</returns>
		private static string GetDeprecatedGameManifestPath()
		{
			string manifestPath = $@"{ConfigHandler.GetLocalDir()}LauncherManifest.txt";
			return manifestPath;
		}

		/// <summary>
		/// Gets the deprecated old manifests' path on disk.
		/// </summary>
		/// <returns>The deprecated old manifest's path.</returns>
		private static string GetDeprecatedOldGameManifestPath()
		{
			string oldManifestPath = $@"{ConfigHandler.GetLocalDir()}LauncherManifest.txt.old";
			return oldManifestPath;
		}

		/// <summary>
		/// Gets the game manifest URL.
		/// </summary>
		/// <returns>The game manifest URL.</returns>
		public static string GetGameManifestURL()
		{
			string manifestURL = $"{Config.GetBaseProtocolURL()}/game/{Config.GetSystemTarget()}/GameManifest.txt";

			return manifestURL;
		}

		/// <summary>
		/// Gets the game manifest checksum URL.
		/// </summary>
		/// <returns>The game manifest checksum URL.</returns>
		public static string GetGameManifestChecksumURL()
		{
			string manifestChecksumURL = $"{Config.GetBaseProtocolURL()}/game/{Config.GetSystemTarget()}/GameManifest.checksum";

			return manifestChecksumURL;
		}

		/// <summary>
		/// Gets the launchpad manifest URL.
		/// </summary>
		/// <returns>The launchpad manifest URL.</returns>
		public static string GetLaunchpadManifestURL()
		{
			string manifestURL = $"{Config.GetBaseProtocolURL()}/launcher/LaunchpadManifest.txt";

			return manifestURL;
		}

		/// <summary>
		/// Gets the launchpad manifest checksum URL.
		/// </summary>
		/// <returns>The launchpad manifest checksum URL.</returns>
		public static string GetLaunchpadManifestChecksumURL()
		{
			string manifestChecksumURL = $"{Config.GetBaseProtocolURL()}/launcher/LaunchpadManifest.checksum";

			return manifestChecksumURL;
		}

		/// <summary>
		/// Replaces the deprecated manifest, moving LauncherManifest to GameManifest (if present).
		/// This function should only be called once per launcher start.
		/// </summary>
		private void ReplaceDeprecatedManifest()
		{
			if (File.Exists(GetDeprecatedGameManifestPath()))
			{
				Log.Info("Found deprecated game manifest in install folder. Moving to new filename.");
				lock (this.GameManifestLock)
				{
					File.Move(GetDeprecatedGameManifestPath(), GetGameManifestPath());
				}
			}

			if (File.Exists(GetDeprecatedOldGameManifestPath()))
			{
				Log.Info("Found deprecated old game manifest in install folder. Moving to new filename.");
				lock (this.OldGameManifestLock)
				{
					File.Move(GetDeprecatedOldGameManifestPath(), GetOldGameManifestPath());
				}
			}
		}
	}
}

