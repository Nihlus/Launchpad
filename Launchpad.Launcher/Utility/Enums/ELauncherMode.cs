//
//  ELauncherMode.cs
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

namespace Launchpad.Launcher.Utility.Enums
{
	/// <summary>
	/// The mode the launcher is in.
	/// </summary>
	internal enum ELauncherMode
	{
		/// <summary>
		/// The launcher can install or is installing a game.
		/// </summary>
		Install,

		/// <summary>
		/// The launcher can update or is updating a game.
		/// </summary>
		Update,

		/// <summary>
		/// The launcher can repair or is repairing a game.
		/// </summary>
		Repair,

		/// <summary>
		/// The launcher can launch or is launching a game.
		/// </summary>
		Launch,

		/// <summary>
		/// The launcher can't do or isn't doing anything.
		/// </summary>
		Inactive
	}
}
