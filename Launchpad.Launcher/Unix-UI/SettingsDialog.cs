//
//  SettingsDialog.cs
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
using System;
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.UI
{
	/// <summary>
	/// Settings dialog box.
	/// </summary>
	[CLSCompliant(false)]
	public partial class SettingsDialog : Dialog
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// The checks handler reference.
		/// </summary>
		ChecksHandler Checks = new ChecksHandler();

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad.Launcher.UI.SettingsDialog"/> class.
		/// </summary>
		public SettingsDialog()
		{
			this.Build();

			//fill in Local settings
			GameName_entry.Text = Config.GetGameName();

			combobox_SystemTarget.Active = (int)Config.GetSystemTarget();

			//fill in remote settings
			FTPURL_entry.Text = Config.GetBaseFTPUrl();
			FTPUsername_entry.Text = Config.GetRemoteUsername();
			FTPPassword_entry.Text = Config.GetRemotePassword();

			progressbar3.Text = Mono.Unix.Catalog.GetString("Idle");
			buttonOk.Label = Mono.Unix.Catalog.GetString("OK");
			buttonCancel.Label = Mono.Unix.Catalog.GetString("Cancel");

		}

		/// <summary>
		/// Raises the button ok clicked event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnButtonOkClicked(object sender, EventArgs e)
		{
			Application.Invoke(delegate
				{
					progressbar3.Text = Mono.Unix.Catalog.GetString("Verifying...");			
				});

			bool bAreAllSettingsOK = true;
			Config.SetGameName(GameName_entry.Text);

			ESystemTarget SystemTarget = Utilities.ParseSystemTarget(combobox_SystemTarget.ActiveText);
			if (SystemTarget != ESystemTarget.Unknown)
			{
				Config.SetSystemTarget(SystemTarget);
			}
			else
			{
				bAreAllSettingsOK = false;
			}

			if (FTPURL_entry.Text.StartsWith("ftp://"))
			{
				Config.SetBaseFTPUrl(FTPURL_entry.Text);
			}
			else
			{
				bAreAllSettingsOK = false;
				Gdk.Color col = new Gdk.Color(255, 128, 128);
				FTPURL_entry.ModifyBase(StateType.Normal, col);
				FTPURL_entry.TooltipText = Mono.Unix.Catalog.GetString("The URL needs to begin with \"ftp://\". Please correct the URL.");
			}

			Config.SetRemotePassword(FTPPassword_entry.Text);
			Config.SetRemoteUsername(FTPUsername_entry.Text);


			if (bAreAllSettingsOK)
			{
				if (Checks.CanPatch())
				{
					Destroy();
				}
				else
				{
					MessageDialog dialog = new MessageDialog(
						                       null, DialogFlags.Modal, 
						                       MessageType.Warning, 
						                       ButtonsType.Ok, 
						                       Mono.Unix.Catalog.GetString("Failed to connect to the FTP server. Please check your FTP settings."));

					dialog.Run();
					dialog.Destroy();
				}
			}

			progressbar3.Text = Mono.Unix.Catalog.GetString("Idle");
		}

		/// <summary>
		/// Raises the button cancel clicked event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnButtonCancelClicked(object sender, EventArgs e)
		{
			Destroy();
		}

		/// <summary>
		/// Raises the FTPURL entry changed event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnFTPURLEntryChanged(object sender, EventArgs e)
		{
			//Set the base colour back to normal
			FTPURL_entry.ModifyBase(StateType.Normal);
		}
	}
}

