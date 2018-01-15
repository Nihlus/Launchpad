//
//  CLIOptions.cs
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

using CommandLine;
using Launchpad.Common.Enums;

namespace Launchpad.Utilities.Options
{
	public class CLIOptions
	{
		[Option('b', "batch", Required = false,
		 HelpText = "Run the utilities in batch mode without a UI.")]
		public bool RunBatchProcessing
		{
			get;
			set;
		}

		[Option('d', "directory", Required = false,
		 HelpText = "The target directory from which the manifest should be generated.")]
		public string TargetDirectory
		{
			get;
			set;
		}

		[Option('m', "manifest", Required = false,
		 HelpText = "The type of manifest that should be generated. This only affects the output filename, " +
		            "and not the actual file content. " +
		 			"Valid manifests: Game or Launcher")]
		public EManifestType ManifestType
		{
			get;
			set;
		}
	}
}