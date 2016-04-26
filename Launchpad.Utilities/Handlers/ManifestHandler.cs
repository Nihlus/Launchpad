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

using System;
using Launchpad.Utilities.Utility.Events;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Launchpad.Utilities.Handlers
{
	public class ManifestHandler
	{
		public event ManifestGenerationProgressChangedEventHandler ManifestGenerationProgressChanged;
		public event ManifestGenerationFinishedEventHandler ManifestGenerationFinished;

		ManifestGenerationProgressChangedEventArgs ProgressArgs = new ManifestGenerationProgressChangedEventArgs();

		private string TargetPath;
		EManifestType ManifestType;

		/// <summary>
		/// Generates a manifest containing the relative path, MD5 hash and file size from
		/// all files in the provided root path.
		/// </summary>
		/// <param name="rootPath">The root path of the directory the manifest should represent.</param>
		/// <param name="InManifestType">The type of manifest that should be generated.</param>
		public void GenerateManifest(string rootPath, EManifestType InManifestType)
		{			
			TargetPath = rootPath;
			ManifestType = InManifestType;

			Thread t = new Thread(GenerateManifest_Implementation);
			t.Start();
		}

		/// <summary>
		/// The asynchronous implementation of the GenerateManifest function.
		/// </summary>
		private void GenerateManifest_Implementation()
		{
			string parentDirectory = Directory.GetParent(TargetPath).ToString();
			string manifestPath = String.Format(@"{0}{1}{2}Manifest.txt", parentDirectory, Path.DirectorySeparatorChar, ManifestType);
			string manifestChecksumPath = String.Format(@"{0}{1}{2}Manifest.checksum", parentDirectory, Path.DirectorySeparatorChar, ManifestType);

			List<string> Files = new List<string>(Directory
				.EnumerateFiles(TargetPath, "*", SearchOption.AllDirectories)
				.Where(s => !s.EndsWith(".install") && !s.EndsWith(".update")));

			using (TextWriter tw = new StreamWriter(File.Create(manifestPath)))
			{
				int completedFiles = 0;
				foreach (string file in Files)
				{			
					// Calculate the MD5 hash of the file
					string hash;
					using (FileStream fileStream = File.OpenRead(file))
					{
						hash = MD5Handler.GetStreamHash(fileStream);
					}

					// Get the file size on disk
					FileInfo Info = new FileInfo(file);
					long fileSize = Info.Length;

					// Get the relative path of the file
					string relativeFilePath = file.Substring(TargetPath.Length);

					// Write the entry to the manifest
					ManifestEntry Entry = new ManifestEntry();
					Entry.RelativePath = relativeFilePath;
					Entry.Hash = hash;
					Entry.Size = fileSize;

					tw.WriteLine(Entry);

					completedFiles++;

					ProgressArgs.Filepath = relativeFilePath;
					ProgressArgs.TotalFiles = Files.Count;
					ProgressArgs.CompletedFiles = completedFiles;
					ProgressArgs.MD5 = hash;
					ProgressArgs.Filesize = fileSize;
					OnManifestGenerationProgressChanged();					
				}
			}

			// Create a checksum file for the manifest.
			using (Stream manifestStream = File.OpenRead(manifestPath))
			{
				string manifestHash = MD5Handler.GetStreamHash(manifestStream);

				using (FileStream checksumStream = File.Create(manifestChecksumPath))
				{
					using (TextWriter tw = new StreamWriter(checksumStream))
					{
						tw.WriteLine(manifestHash);
						tw.Close();
					}
				}
			}

			OnManifestGenerationFinished();
		}

		private void OnManifestGenerationProgressChanged()
		{
			if (ManifestGenerationProgressChanged != null)
			{
				ManifestGenerationProgressChanged(this, ProgressArgs);
			}	
		}

		private void OnManifestGenerationFinished()
		{
			if (ManifestGenerationFinished != null)
			{
				ManifestGenerationFinished(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Enum defining the type of manifest.
	/// </summary>
	public enum EManifestType : byte
	{
		Launchpad,
		Game
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

		public ManifestEntry()
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
		/// <param name="inEntry">The resulting entry.</param>
		public static bool TryParse(string rawInput, out ManifestEntry inEntry)
		{
			//clear out the entry for the new data
			inEntry = new ManifestEntry();

			if (!String.IsNullOrEmpty(rawInput))
			{
				//remove any and all bad characters from the input string, 
				//such as \0, \n and \r.
				string cleanInput = CleanInput(rawInput);

				//split the string into its three components - file, hash and size
				string[] entryElements = cleanInput.Split(':');

				//if we have three elements (which we should always have), set them in the provided entry
				if (entryElements.Length == 3)
				{
					//clean the manifest path, converting \ to / on unix and / to \ on Windows.
					if (ChecksHandler.IsRunningOnUnix())
					{
						inEntry.RelativePath = entryElements[0].Replace("\\", "/");
					}
					else
					{
						inEntry.RelativePath = entryElements[0].Replace("/", "\\");
					}

					//set the hash to the second element
					inEntry.Hash = entryElements[1];

					//attempt to parse the final element as a long-type byte count.
					long parsedSize;
					if (long.TryParse(entryElements[2], out parsedSize))
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
		/// Clean the specified input from newlines and nulls (\r, \n and \0)
		/// </summary>
		/// <param name="input">Input string.</param>
		private static string CleanInput(string input)
		{
			return input.Replace("\n", String.Empty).Replace("\0", String.Empty).Replace("\r", String.Empty);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Launchpad.Utilities.Handlers.ManifestEntry"/>.
		/// The returned value matches a raw in-manifest representation of the entry, in the form of
		/// [path]:[hash]:[size]
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Launchpad.Utilities.Handlers.ManifestEntry"/>.</returns>
		public override string ToString()
		{
			return RelativePath + ":" + Hash + ":" + Size;
		}

		public bool Equals(ManifestEntry Other)
		{
			return this.RelativePath == Other.RelativePath &&
			this.Hash == Other.Hash &&
			this.Size == Other.Size;
		}
	}
}

