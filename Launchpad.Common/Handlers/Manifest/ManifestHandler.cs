//
//  ManifestHandler.cs
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

using Launchpad.Common.Enums;

namespace Launchpad.Common.Handlers.Manifest
{
	/// <summary>
	/// Handler class for the game manifest.
	/// </summary>
	public sealed class ManifestHandler
	{
		private readonly object ManifestsLock = new object();

		/// <summary>
		/// The local base directory of the running assembly. Used to produce relative paths for manifest-related
		/// files and folders.
		/// </summary>
		private readonly string LocalBaseDirectory;

		/// <summary>
		/// The remote <see cref="Uri"/> where the manifest files are expected to be.
		/// </summary>
		private readonly Uri RemoteURL;

		/// <summary>
		/// The target system for which the handler should retrieve files.
		/// </summary>
		private readonly ESystemTarget SystemTarget;

		private readonly Dictionary<EManifestType, IReadOnlyList<ManifestEntry>> Manifests = new Dictionary<EManifestType, IReadOnlyList<ManifestEntry>>();
		private readonly Dictionary<EManifestType, IReadOnlyList<ManifestEntry>> OldManifests = new Dictionary<EManifestType, IReadOnlyList<ManifestEntry>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ManifestHandler"/> class.
		/// This constructor also serves to updated outdated file paths for the manifests.
		/// </summary>
		/// <param name="localBaseDirectory">The local base directory of the launcher installation.</param>
		/// <param name="remoteURL">The remote <see cref="Uri"/> where the manifest files are expected to be.</param>
		/// <param name="systemTarget">The target system for which the handler should retrieve files.</param>
		public ManifestHandler(string localBaseDirectory, Uri remoteURL, ESystemTarget systemTarget)
		{
			this.LocalBaseDirectory = localBaseDirectory;
			this.RemoteURL = remoteURL;
			this.SystemTarget = systemTarget;
		}

		/// <summary>
		/// Gets the specifed manifest currently held by the launcher. The return value of this method may be null if no
		/// manifest could be retrieved.
		/// </summary>
		/// <param name="manifestType">The type of manifest to retrieve, that is, the manifest for a specific component.</param>
		/// <param name="getOldManifest">Whether or not the old manifest or the new manifest should be retrieved.</param>
		/// <returns>A list of <see cref="ManifestEntry"/> objects.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="manifestType"/> is not a known value.</exception>
		public IReadOnlyList<ManifestEntry> GetManifest(EManifestType manifestType, bool getOldManifest)
		{
			switch (manifestType)
			{
				case EManifestType.Game:
				case EManifestType.Launchpad:
				{
					lock (this.ManifestsLock)
					{
						if (getOldManifest)
						{
							if (this.OldManifests.ContainsKey(manifestType))
							{
								return this.OldManifests[manifestType];
							}
						}
						else
						{
							if (this.Manifests.ContainsKey(manifestType))
							{
								return this.Manifests[manifestType];
							}
						}
					}

					return null;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(manifestType), "An unknown manifest type was requested.");
				}
			}
		}

		/// <summary>
		/// Reloads all manifests of the specifed type from disk.
		/// </summary>
		/// <param name="manifestType">The type of manifest to reload.</param>
		public void ReloadManifests(EManifestType manifestType)
		{
			lock (this.ManifestsLock)
			{
				var newManifestPath = GetManifestPath(manifestType, false);
				var oldManifestPath = GetManifestPath(manifestType, true);

				// Reload new manifests
				if (!File.Exists(newManifestPath))
				{
					this.Manifests.AddOrUpdate(manifestType, null);
				}
				else
				{
					this.Manifests.AddOrUpdate(manifestType, LoadManifest(newManifestPath));
				}

				// Reload old manifests
				if (!File.Exists(oldManifestPath))
				{
					this.OldManifests.AddOrUpdate(manifestType, null);
				}
				else
				{
					this.OldManifests.AddOrUpdate(manifestType, LoadManifest(oldManifestPath));
				}
			}
		}

		/// <summary>
		/// Loads a manifest from a file on disk.
		/// </summary>
		/// <param name="manifestPath">The path to a manifest file.</param>
		/// <returns>A list of <see cref="ManifestEntry"/> objects.</returns>
		public static IReadOnlyList<ManifestEntry> LoadManifest(string manifestPath)
		{
			using (var fileStream = File.OpenRead(manifestPath))
			{
				return LoadManifest(fileStream);
			}
		}

		/// <summary>
		/// Loads a manifest from a <see cref="Stream"/>.
		/// </summary>
		/// <param name="manifestStream">A stream containing a manifest."/></param>
		/// <returns>A read-only list of <see cref="ManifestEntry"/> objects.</returns>
		public static IReadOnlyList<ManifestEntry> LoadManifest(Stream manifestStream)
		{
			var rawManifest = new List<string>();
			using (var sr = new StreamReader(manifestStream))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					rawManifest.Add(line);
				}
			}

			var manifest = new List<ManifestEntry>();
			foreach (var rawEntry in rawManifest)
			{
				if (ManifestEntry.TryParse(rawEntry, out var newEntry))
				{
					manifest.Add(newEntry);
				}
			}

			return manifest;
		}

		/// <summary>
		/// Gets the specified manifest's path on disk. The presence of the manifest is not guaranteed at
		/// this point.
		/// </summary>
		/// <param name="manifestType">The type of manifest to get the path to.</param>
		/// <param name="getOldManifestPath">Whether or not the path should specify an old manifest.</param>
		/// <returns>A fully qualified path to where a manifest should be.</returns>
		public string GetManifestPath(EManifestType manifestType, bool getOldManifestPath)
		{
			var manifestPath = Path.Combine(this.LocalBaseDirectory, $"{manifestType}Manifest.txt");

			if (getOldManifestPath)
			{
				manifestPath += ".old";
			}

			return manifestPath;
		}

		/// <summary>
		/// Gets the manifest URL for the specified manifest type.
		/// </summary>
		/// <param name="manifestType">The type of manifest to get the URL of.</param>
		/// <returns>The game manifest URL.</returns>
		public string GetManifestURL(EManifestType manifestType)
		{
			if (manifestType == EManifestType.Launchpad)
			{
				return $"{this.RemoteURL}/launcher/{manifestType}Manifest.txt";
			}

			return $"{this.RemoteURL}/game/{this.SystemTarget}/{manifestType}Manifest.txt";
		}

		/// <summary>
		/// Gets the manifest URL for the specified manifest type.
		/// </summary>
		/// <param name="manifestType">The type of manifest to get the URL of.</param>
		/// <returns>The game manifest URL.</returns>
		public string GetManifestChecksumURL(EManifestType manifestType)
		{
			if (manifestType == EManifestType.Launchpad)
			{
				return $"{this.RemoteURL.LocalPath}/launcher/{manifestType}Manifest.checksum";
			}

			return $"{this.RemoteURL.LocalPath}/game/{this.SystemTarget}/{manifestType}Manifest.checksum";
		}
	}
}
