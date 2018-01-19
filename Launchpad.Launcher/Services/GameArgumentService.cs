//
//  GameArgumentService.cs
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.Services
{
	/// <summary>
	/// A service providing access to arguments that should be passed to the game.
	/// </summary>
	public class GameArgumentService
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GameArgumentService"/> class.
		/// </summary>
		public GameArgumentService()
		{
			InitializeGameArgumentsFile();
		}

		/// <summary>
		/// Creates a configuration file where the user or developer can add runtime switches for the installed game.
		/// If the file already exists, this method does nothing.
		/// </summary>
		private static void InitializeGameArgumentsFile()
		{
			// Initialize the game arguments file, if needed
			if (File.Exists(DirectoryHelpers.GetGameArgumentsPath()))
			{
				return;
			}

			using (var fs = File.Create(DirectoryHelpers.GetGameArgumentsPath()))
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
		/// Gets a list of command-line arguments that are passed to the game when it starts.
		/// </summary>
		/// <returns>The arguments.</returns>
		public IEnumerable<string> GetGameArguments()
		{
			if (!File.Exists(DirectoryHelpers.GetGameArgumentsPath()))
			{
				return new List<string>();
			}

			var gameArguments = new List<string>(File.ReadAllLines(DirectoryHelpers.GetGameArgumentsPath()));

			// Return the list of lines in the argument file, except the ones starting with a hash or empty lines
			return gameArguments.Where(s => !s.StartsWith("#") && !string.IsNullOrEmpty(s)).ToList();
		}
	}
}
