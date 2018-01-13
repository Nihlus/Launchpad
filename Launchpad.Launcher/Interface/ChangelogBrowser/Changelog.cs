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
using System.Diagnostics;
using System.Windows.Forms;
using SystemInformation = Launchpad.Common.SystemInformation;
using Gtk;
using WebKit;

namespace Launchpad.Launcher.Interface.ChangelogBrowser
{
	/// <summary>
	/// A GTK-supported changelog browser widget, capable of switching between a WinForms implementation (for Windows)
	/// and a Webkit implementation (for Mac and Linux).
	/// </summary>
	public class Changelog : IDisposable
	{
		/// <summary>
		/// The WinForms browser for Windows.
		/// </summary>
		private readonly WindowsBrowser WindowsBrowser;

		/// <summary>
		/// The webkit browser for Mac and Linux.
		/// </summary>
		private readonly WebView UnixBrowser;

		/// <summary>
		/// Whether or not the changelog is currently navigating to a new page from code.
		/// </summary>
		private bool IsNavigatingFromCode;

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
			if (!SystemInformation.IsRunningOnUnix())
			{
				this.WindowsBrowser = new WindowsBrowser(parentContainer);
				this.WidgetHandle = this.WindowsBrowser.WidgetHandle;

				this.WindowsBrowser.Browser.Navigating += OnWindowsBrowserNavigating;
			}
			else
			{
				this.UnixBrowser = new WebView();
				this.WidgetHandle = this.UnixBrowser;
				this.UnixBrowser.NavigationRequested += OnUnixBrowserNavigating;

				parentContainer.Add(this.WidgetHandle);
			}
		}

		/// <summary>
		/// Handles routing of navigation requests to the users's default browser. Navigation requests from code
		/// are allowed, but links that the user clicks are routed outside of the launcher.
		/// </summary>
		private void OnUnixBrowserNavigating(object o, NavigationRequestedArgs args)
		{
			if (!this.IsNavigatingFromCode)
			{
				this.UnixBrowser.StopLoading();
				args.RetVal = false;

				Process.Start(args.Request.Uri);
			}
			else
			{
				this.IsNavigatingFromCode = false;
			}
		}

		/// <summary>
		/// Handles routing of navigation requests to the users's default browser. Navigation requests from code
		/// are allowed, but links that the user clicks are routed outside of the launcher.
		/// </summary>
		private void OnWindowsBrowserNavigating(object sender, WebBrowserNavigatingEventArgs webBrowserNavigatingEventArgs)
		{
			if (!this.IsNavigatingFromCode)
			{
				webBrowserNavigatingEventArgs.Cancel = true;
				Process.Start(webBrowserNavigatingEventArgs.Url.ToString());
			}
			else
			{
				this.IsNavigatingFromCode = false;
			}
		}

		/// <summary>
		/// Navigates to the specified URL, displaying it in the changelog browser.
		/// </summary>
		/// <param name="url">The URL to navigate to.</param>
		public void Navigate(string url)
		{
			this.IsNavigatingFromCode = true;

			this.WindowsBrowser?.Navigate(url);
			this.UnixBrowser?.Open(url);
		}

		/// <summary>
		/// Loads the specified HTML string as a webpage, and sets the current webpage to the provided URL.
		/// </summary>
		/// <param name="html">The HTML string to load.</param>
		/// <param name="url">The base URL for the page source.</param>
		public void LoadHTML(string html, string url)
		{
			this.WindowsBrowser?.LoadHTML(html);
			this.UnixBrowser?.LoadHtmlString(html, url);
		}

		/// <summary>
		/// Disposes the object, releasing any unmanaged resources to the system.
		/// </summary>
		public void Dispose()
		{
			this.WindowsBrowser?.Dispose();
			this.UnixBrowser?.Dispose();
		}
	}
}