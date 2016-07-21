//
//  Program.cs
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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gtk;

namespace Launchpad.Launcher.Interface.ChangelogBrowser
{
	/// <summary>
	/// A GTK-supported wrapper around a WinForms <see cref="WebBrowser"/> widget. This class creates a WebBrowser, and
	/// sets its parent to a <see cref="Socket"/> which is then bound to an input <see cref="Container"/> in the GTK UI.
	/// </summary>
	public class WindowsBrowser
	{
		/// <summary>
		/// Imported unmanaged function for setting the parent of a window. In our case, it's used for setting the parent
		/// of a <see cref="WebBrowser"/> to a <see cref="Socket"/>.
		/// </summary>
		[DllImport("user32.dll", EntryPoint = "SetParent")]
		private static extern IntPtr SetParent([In] IntPtr hWndChild, [In] IntPtr hWndNewParent);

		/// <summary>
		/// The <see cref="WebBrowser"/> we're using for rendering.
		/// </summary>
		private readonly WebBrowser browser = new WebBrowser();

		/// <summary>
		/// The <see cref="Viewport"/> the <see cref="browser"/> is rendered inside, using the <see cref="socket"/>.
		/// </summary>
		private readonly Viewport viewport = new Viewport();

		/// <summary>
		/// The <see cref="Socket"/> which the <see cref="browser"/> is bound to.
		/// </summary>
		private readonly Socket socket = new Socket();

		/// <summary>
		/// A public-facing handle that the UI can use to move the browser around, once it's been created. This points
		/// to the viewport which has the socket inside it.
		/// </summary>
		public Widget WidgetHandle => this.viewport;

		/// <summary>
		/// Creates a new <see cref="WindowsBrowser"/> object, binding a <see cref="WebBrowser"/> to
		/// the input <see cref="Container"/>.
		/// </summary>
		/// <param name="parentContainer">The parent container which will hold the browser.</param>
		public WindowsBrowser(Container parentContainer)
		{
			parentContainer.Add(this.viewport);

			viewport.SizeAllocated += OnViewportSizeAllocated;
			viewport.Add(socket);

			socket.Realize();

			IntPtr browserHandle = browser.Handle;
			IntPtr socketHandle = (IntPtr) socket.Id;

			SetParent(browserHandle, socketHandle);
		}

		/// <summary>
		/// Handles resizing the <see cref="browser"/> widget when the viewport gets a new size allocated to it.
		/// </summary>
		private void OnViewportSizeAllocated(object o, SizeAllocatedArgs args)
		{
			this.browser.Width = args.Allocation.Width;
			this.browser.Height = args.Allocation.Height;
		}

		/// <summary>
		/// Navigates to the specified URL, displaying it in the changelog browser.
		/// </summary>
		/// <param name="url">The URL to navigate to.</param>
		public void Navigate(string url)
		{
			browser.Navigate(url);
		}

		/// <summary>
		/// Loads the specified HTML string as a webpage, and sets the current webpage to the provided URL.
		/// </summary>
		/// <param name="htmlContent">The HTML string to load.</param>
		public void LoadHTML(string htmlContent)
		{
			Navigate("about:blank");
			this.browser.Document?.Write(string.Empty);
			this.browser.DocumentText = htmlContent;
		}
	}
}