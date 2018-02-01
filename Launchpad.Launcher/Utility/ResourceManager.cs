//
//  ResourceManager.cs
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

using System.Reflection;
using Gdk;

namespace Launchpad.Launcher.Utility
{
	/// <summary>
	/// Manages embedded resources of the application.
	/// </summary>
	public static class ResourceManager
	{
		/// <summary>
		/// Gets the application icon as a pixel buffer.
		/// </summary>
		public static Pixbuf ApplicationIcon { get; }

		private static readonly Assembly Assembly;

		static ResourceManager()
		{
			Assembly = Assembly.GetExecutingAssembly();

			ApplicationIcon = Pixbuf.LoadFromResource("Launchpad.Launcher.Resources.Icon.png");
		}
	}
}
