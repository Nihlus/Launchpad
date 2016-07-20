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
	public class WindowsBrowser
	{
		[DllImport("user32.dll", EntryPoint = "SetParent")]
		internal static extern IntPtr SetParent([In] IntPtr hWndChild, [In] IntPtr hWndNewParent);

		private readonly WebBrowser browser = new WebBrowser();
		private readonly Viewport viewport = new Viewport();
		private readonly Socket socket = new Socket();

		public Widget WidgetHandle => this.viewport;

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

		private void OnViewportSizeAllocated(object o, SizeAllocatedArgs args)
		{
			this.browser.Width = args.Allocation.Width;
			this.browser.Height = args.Allocation.Height;
		}

		public void Navigate(string url)
		{
			browser.Navigate(url);
		}

		public void LoadHTML(string htmlContent)
		{
			Navigate("about:blank");
			this.browser.Document?.Write(string.Empty);
			this.browser.DocumentText = htmlContent;
		}
	}
}