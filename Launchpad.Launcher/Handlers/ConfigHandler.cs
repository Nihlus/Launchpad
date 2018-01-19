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

using System.IO;
using Config.Net;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// Config handler.
	/// This is a singleton class, and it should always be accessed through <see cref="Instance"/>.
	/// </summary>
	public sealed class ConfigHandler
	{
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
				.UseIniFile(DirectoryHelpers.GetConfigPath())
				.Build();

			InitializeConfigurationFile();
		}

		/// <summary>
		/// Initializes the config by checking for bad values or files.
		/// Run once when the launcher starts, then avoid unless absolutely neccesary.
		/// </summary>
		private void InitializeConfigurationFile()
		{
			if (File.Exists(DirectoryHelpers.GetConfigPath()))
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
	}
}
