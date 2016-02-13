using System;
using System.Collections.Generic;
using System.IO;

namespace Launchpad.Launcher
{
	internal sealed class ManifestHandler
	{
        private string WhichManifest;
		private List<ManifestEntry> manifest = new List<ManifestEntry> ();

		/// <summary>
		/// Gets the manifest. Call sparsely, as it loads the entire manifest from disk each time 
		/// this property is accessed.
		/// </summary>
		/// <value>The manifest.</value>
		public List<ManifestEntry> Manifest
		{
			get
			{
				LoadManifest ( WhichManifest );
				return manifest;
			}
		}

		private List<ManifestEntry> oldManifest = new List<ManifestEntry> ();

		/// <summary>
		/// Gets the old manifest. Call sparsely, as it loads the entire manifest from disk each time
		/// this property is accessed.
		/// </summary>
		/// <value>The old manifest.</value>
		public List<ManifestEntry> OldManifest
		{
			get
			{
				LoadOldManifest ( WhichManifest );
				return oldManifest;
			}
		}

		private object ManifestLock = new object ();
		private object OldManifestLock = new object ();

		public ManifestHandler ( string ManifestName )
		{
            WhichManifest = ManifestName;
		}

		/// <summary>
		/// Loads the manifest from disk.
		/// </summary>
		private void LoadManifest ( string WhichManifest )
		{
			try
			{
				lock (ManifestLock)
				{
					if (File.Exists (ConfigHandler.GetManifestPath ( WhichManifest )))
					{
						string[] rawManifest = File.ReadAllLines (ConfigHandler.GetManifestPath ( WhichManifest ));
						foreach (string rawEntry in rawManifest)
						{
							ManifestEntry newEntry = new ManifestEntry ();
							if (ManifestEntry.TryParse (rawEntry, out newEntry))
							{
								manifest.Add (newEntry);
							}
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in LoadManifest(): " + ioex.Message);
			}
		}

		/// <summary>
		/// Loads the old manifest from disk.
		/// </summary>
		private void LoadOldManifest ( string WhichManifest )
		{
			try
			{
				lock (OldManifestLock)
				{
					if (File.Exists (ConfigHandler.GetOldManifestPath ( WhichManifest )))
					{
						string[] rawOldManifest = File.ReadAllLines (ConfigHandler.GetOldManifestPath ( WhichManifest ));
						foreach (string rawEntry in rawOldManifest)
						{
							ManifestEntry newEntry = new ManifestEntry ();
							if (ManifestEntry.TryParse (rawEntry, out newEntry))
							{
								oldManifest.Add (newEntry);
							}
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine ("IOException in LoadOldManifest(): " + ioex.Message);
			}
		}
	}

	/// <summary>
	/// A manifest entry derived from the raw unformatted string.
	/// Contains the relative path of the referenced file, as well as
	/// its MD5 hash and size in bytes.
	/// </summary>
	internal sealed class ManifestEntry : IEquatable<ManifestEntry>
	{
		public string RelativePath
		{
			get;
			set;
		}

		public string Hash
		{
			get;
			set;
		}

		public long Size
		{
			get;
			set;
		}

		public ManifestEntry ()
		{
			RelativePath = String.Empty;
			Hash = String.Empty;
			Size = 0;
		}

		/// <summary>
		/// Attempts to parse an entry from a raw input.
		/// The input is expected to be in [path]:[hash]:[size] format.
		/// </summary>
		/// <returns><c>true</c>, if the input was successfully parse, <c>false</c> otherwise.</returns>
		/// <param name="rawInput">Raw input.</param>
		/// <param name="entry">The resulting entry.</param>
		public static bool TryParse (string rawInput, out ManifestEntry inEntry)
		{
			//clear out the entry for the new data
			inEntry = new ManifestEntry ();

			if (!String.IsNullOrEmpty (rawInput))
			{
				//remove any and all bad characters from the input string, 
				//such as \0, \n and \r.
				string cleanInput = Utilities.Clean (rawInput);

				//split the string into its three components - file, hash and size
				string[] entryElements = cleanInput.Split (':');

				//if we have three elements (which we should always have), set them in the provided entry
				if (entryElements.Length == 3)
				{
					//clean the manifest path, converting \ to / on unix and / to \ on Windows.
					if (ChecksHandler.IsRunningOnUnix ())
					{
						inEntry.RelativePath = entryElements [0].Replace ("\\", "/");
					}
					else
					{
						inEntry.RelativePath = entryElements [0].Replace ("/", "\\");
					}

					//set the hash to the second element
					inEntry.Hash = entryElements [1];

					//attempt to parse the final element as a long-type byte count.
					long parsedSize = 0;
					if (long.TryParse (entryElements [2], out parsedSize))
					{
						inEntry.Size = parsedSize;
						return true;
					}
					else
					{
						//could not parse the size, parsing has failed.
						return false;
					}
				}
				else
				{
					//wrong number of raw entry elements, parsing has failed.
					return false;
				}
			}
			else
			{
				//no input, parsing has failed
				return false;
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Launchpad.ManifestEntry"/>.
		/// The returned value matches a raw in-manifest representation of the entry, in the form of
		/// [path]:[hash]:[size]
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Launchpad.ManifestEntry"/>.</returns>
		public override string ToString ()
		{
			return RelativePath + ":" + Hash + ":" + Size.ToString ();
		}

		public bool Equals (ManifestEntry Other)
		{
			return this.RelativePath == Other.RelativePath &&
			this.Hash == Other.Hash &&
			this.Size == Other.Size;
		}
	}
}

