//
//  Changelog.cs
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
using Gtk;
using Launchpad.Launcher.Handlers;
using WebKit;

namespace Launchpad.Launcher.Interface.ChangelogBrowser
{
	/// <summary>
	/// A GTK-supported changelog browser widget, capable of switching between a WinForms implementation (for Windows)
	/// and a Webkit implementation (for Mac and Linux).
	/// </summary>
	[CLSCompliant(false)]
	public class Changelog
	{
		/// <summary>
		/// The WinForms browser for Windows.
		/// </summary>
		private readonly WindowsBrowser windowsBrowser;

		/// <summary>
		/// The webkit browser for Mac and Linux.
		/// </summary>
		private readonly WebView unixBrowser;

		/// <summary>
		/// A handle for the underlying widget used. For windows, this will be a <see cref="Viewport"/>. For
		/// Mac and Linux, it will be a <see cref="WebView"/>.
		/// </summary>
		private Widget WidgetHandle
		{
			get;
		}

		/// <summary>
		/// Creates a new <see cref="Changelog"/> object, and adds the visible changelog widget to the provided
		/// parent container.
		/// </summary>
		/// <param name="parentContainer">The parent GTK container where the changelog should be added.</param>
		public Changelog(Container parentContainer)
		{
			if (!ChecksHandler.IsRunningOnUnix())
			{
				this.windowsBrowser = new WindowsBrowser(parentContainer);
				this.WidgetHandle = windowsBrowser.WidgetHandle;
			}
			else
			{
				this.unixBrowser = new WebView();
				this.WidgetHandle = this.unixBrowser;

				parentContainer.Add(WidgetHandle);
			}
		}

		/// <summary>
		/// Navigates to the specified URL, displaying it in the changelog browser.
		/// </summary>
		/// <param name="url">The URL to navigate to.</param>
		public void Navigate(string url)
		{
			this.windowsBrowser?.Navigate(url);
			this.unixBrowser?.Open(url);
		}

		/// <summary>
		/// Loads the specified HTML string as a webpage, and sets the current webpage to the provided URL.
		/// </summary>
		/// <param name="html">The HTML string to load.</param>
		/// <param name="url">The base URL for the page source.</param>
		public void LoadHTML(string html, string url)
		{
			this.windowsBrowser?.LoadHTML(html);
			this.unixBrowser?.LoadHtmlString(html, url);
		}
	}
}