//
//  MainWindow.UI.cs
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
using Gtk;
using UIElement = Gtk.Builder.ObjectAttribute;

// ReSharper disable UnassignedReadonlyField
#pragma warning disable 169
#pragma warning disable 649
#pragma warning disable 1591
#pragma warning disable SA1134 // Each attribute should be placed on its own line of code
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields

namespace Launchpad.Utilities.Interface
{
	public partial class MainWindow
	{
		[UIElement] private readonly FileChooserWidget FolderChooser;

		[UIElement] private readonly Label StatusLabel;

		[UIElement] private readonly ProgressBar MainProgressBar;

		[UIElement] private readonly Button GenerateLaunchpadManifestButton;
		[UIElement] private readonly Button GenerateGameManifestButton;

		/// <summary>
		/// Creates a new instance of the <see cref="MainWindow"/> class, loading its interface definition from file.
		/// </summary>
		/// <returns>An instance of the main window widget.</returns>
		public static MainWindow Create()
		{
			using (var builder = new Builder(Assembly.GetExecutingAssembly(), "Launchpad.Utilities.Interface.Launchpad.Utilities.glade", null))
			{
				return new MainWindow(builder, builder.GetObject(nameof(MainWindow)).Handle);
			}
		}

		/// <summary>
		/// Binds UI-related events.
		/// </summary>
		private void BindUIEvents()
		{
			this.DeleteEvent += OnDeleteEvent;

			this.GenerateLaunchpadManifestButton.Clicked += OnGenerateLaunchpadManifestButtonClicked;
			this.GenerateGameManifestButton.Clicked += OnGenerateGameManifestButtonClicked;
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		private void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			this.TokenSource?.Cancel();

			Application.Quit();
			a.RetVal = true;
		}
	}
}
