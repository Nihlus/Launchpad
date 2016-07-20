//
//  ChangelogBrowser.cs
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

using Gtk;
using Launchpad.Launcher.Handlers;
using WebKit;

namespace Launchpad.Launcher.Interface.ChangelogBrowser
{
	public class ChangelogBrowser
	{
		private readonly WindowsBrowser windowsBrowser;
		private readonly WebView unixBrowser;

		public Widget WidgetHandle
		{
			get;
			private set;
		}

		public ChangelogBrowser(Container parentContainer)
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

		public void Navigate(string url)
		{
			this.windowsBrowser?.Navigate(url);
			this.unixBrowser?.Open(url);
		}

		public void LoadHTML(string html, string url)
		{
			this.windowsBrowser?.LoadHTML(html);
			this.unixBrowser?.LoadHtmlString(html, url);
		}
	}
}