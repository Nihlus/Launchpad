using System;
using System.Collections.Generic;
using Gtk;

namespace Launchpad_Launcher
{
	/// <summary>
	/// Settings dialog box.
	/// </summary>
	public partial class SettingsDialog : Gtk.Dialog
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// The checks handler reference.
		/// </summary>
		ChecksHandler Checks = new ChecksHandler ();

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.SettingsDialog"/> class.
		/// </summary>
		public SettingsDialog ()
		{
			this.Build ();
			//fill in Local settings
			GameName_entry.Text = Config.GetGameName ();
			SystemTarget_entry.Text = Config.GetSystemTarget ();

			//fill in remote settings
			FTPURL_entry.Text = Config.GetBaseFTPUrl ();
			FTPUsername_entry.Text = Config.GetFTPUsername ();
			FTPPassword_entry.Text = Config.GetFTPPassword ();

			progressbar3.Text = "Idle";

		}

		/// <summary>
		/// Raises the button ok clicked event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnButtonOkClicked (object sender, EventArgs e)
		{
			Gtk.Application.Invoke(delegate 
			                       {
				progressbar3.Text = "Verifying...";			
			});

			bool bAreAllSettingsOK = true;
			Config.SetGameName (GameName_entry.Text);

			var AllowedValues = new List<string> () { "Win64", "Win32", "Linux", "Mac" };
			if (AllowedValues.Contains(SystemTarget_entry.Text))
			{
				Config.SetSystemTarget (SystemTarget_entry.Text);
			}
			else
			{
				bAreAllSettingsOK = false;
				Gdk.Color col = new Gdk.Color(255, 128, 128);
				SystemTarget_entry.ModifyBase(Gtk.StateType.Normal, col);
				SystemTarget_entry.TooltipText = "The system target needs to be one of the following:" + 
					"\"Win64\", \"Win32\", \"Linux\" or \"Mac\". Please correct the target.";
			}

			if (FTPURL_entry.Text.StartsWith ("ftp://"))
			{
				Config.SetBaseFTPUrl (FTPURL_entry.Text);
			} 
			else
			{
				bAreAllSettingsOK = false;
				Gdk.Color col = new Gdk.Color(255, 128, 128);
				FTPURL_entry.ModifyBase(Gtk.StateType.Normal, col);
				FTPURL_entry.TooltipText = "The URL needs to begin with \"ftp://\". Please correct the URL.";
			}

			Config.SetFTPPassword (FTPPassword_entry.Text);
			Config.SetFTPUsername (FTPUsername_entry.Text);


			if (bAreAllSettingsOK)
			{
				if (Checks.CanConnectToFTP ())
				{
					this.Destroy ();
				}
				else
				{
					MessageDialog dialog = new MessageDialog (
						null, DialogFlags.Modal, 
						MessageType.Warning, 
						ButtonsType.Ok, 
						"Failed to connect to the FTP server. Please check your FTP settings.");

					dialog.Run ();
					dialog.Destroy ();
				}
			}

			progressbar3.Text = "Idle";
		}

		/// <summary>
		/// Raises the button cancel clicked event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnButtonCancelClicked (object sender, EventArgs e)
		{
			this.Destroy ();
		}

		/// <summary>
		/// Raises the system target entry changed event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnSystemTargetEntryChanged (object sender, EventArgs e)
		{
			//Set the base colour back to normal
			SystemTarget_entry.ModifyBase (Gtk.StateType.Normal);
		}

		/// <summary>
		/// Raises the FTPURL entry changed event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnFTPURLEntryChanged (object sender, EventArgs e)
		{
			//Set the base colour back to normal
			FTPURL_entry.ModifyBase (Gtk.StateType.Normal);
		}
	}
}

