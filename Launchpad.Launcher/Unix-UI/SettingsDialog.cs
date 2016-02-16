using Gtk;
using System;

namespace Launchpad.Launcher
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
		ChecksHandler Checks = new ChecksHandler ();

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad.SettingsDialog"/> class.
		/// </summary>
		public SettingsDialog ()
		{
			this.Build ();
			//fill in Local settings
			GameName_entry.Text = Config.GetGameName ();

			combobox_SystemTarget.Active = (int)Config.GetSystemTarget();

			//fill in remote settings
			FTPURL_entry.Text = Config.GetBasePatchUrl ();
			FTPUsername_entry.Text = Config.GetPatchUsername ();
			FTPPassword_entry.Text = Config.GetPatchPassword ();

			progressbar3.Text = Mono.Unix.Catalog.GetString("Idle");
			buttonOk.Label = Mono.Unix.Catalog.GetString ("OK");
			buttonCancel.Label = Mono.Unix.Catalog.GetString ("Cancel");

		}

		/// <summary>
		/// Raises the button ok clicked event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnButtonOkClicked (object sender, EventArgs e)
		{
			Application.Invoke(delegate 
			                       {
				progressbar3.Text = Mono.Unix.Catalog.GetString("Verifying...");			
			});

			bool bAreAllSettingsOK = true;
			Config.SetGameName (GameName_entry.Text);

			ESystemTarget SystemTarget = Utilities.ParseSystemTarget (combobox_SystemTarget.ActiveText);
			if (SystemTarget != ESystemTarget.Invalid)
			{
				Config.SetSystemTarget (SystemTarget);
			}
			else
			{
				bAreAllSettingsOK = false;
			}

			if (FTPURL_entry.Text.StartsWith ("ftp://"))
			{
				Config.SetBasePatchUrl (FTPURL_entry.Text);
			} 
			else
			{
				bAreAllSettingsOK = false;
				Gdk.Color col = new Gdk.Color(255, 128, 128);
				FTPURL_entry.ModifyBase(StateType.Normal, col);
				FTPURL_entry.TooltipText = Mono.Unix.Catalog.GetString("The URL needs to begin with \"ftp://\". Please correct the URL.");
			}

			Config.SetPatchPassword (FTPPassword_entry.Text);
			Config.SetPatchUsername (FTPUsername_entry.Text);


			if (bAreAllSettingsOK)
			{
				if (Checks.CanConnectToPatchServer ())
				{
					Destroy ();
				}
				else
				{
					MessageDialog dialog = new MessageDialog (
						null, DialogFlags.Modal, 
						MessageType.Warning, 
						ButtonsType.Ok, 
						Mono.Unix.Catalog.GetString("Failed to connect to the FTP server. Please check your FTP settings."));

					dialog.Run ();
					dialog.Destroy ();
				}
			}

			progressbar3.Text = Mono.Unix.Catalog.GetString("Idle");
		}

		/// <summary>
		/// Raises the button cancel clicked event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnButtonCancelClicked (object sender, EventArgs e)
		{
			Destroy ();
		}

		/// <summary>
		/// Raises the FTPURL entry changed event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnFTPURLEntryChanged (object sender, EventArgs e)
		{
			//Set the base colour back to normal
			FTPURL_entry.ModifyBase (StateType.Normal);
		}
	}
}

