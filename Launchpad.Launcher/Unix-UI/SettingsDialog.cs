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
			HTTPURL_entry.Text = Config.GetBaseHTTPUrl ();
			HTTPUsername_entry.Text = Config.GetHTTPUsername ();
			HTTPPassword_entry.Text = Config.GetHTTPPassword ();

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

			if (HTTPURL_entry.Text.StartsWith ("HTTP://"))
			{
				Config.SetBaseHTTPUrl (HTTPURL_entry.Text);
			} 
			else
			{
				bAreAllSettingsOK = false;
				Gdk.Color col = new Gdk.Color(255, 128, 128);
				HTTPURL_entry.ModifyBase(StateType.Normal, col);
				HTTPURL_entry.TooltipText = Mono.Unix.Catalog.GetString("The URL needs to begin with \"HTTP://\". Please correct the URL.");
			}

			Config.SetHTTPPassword (HTTPPassword_entry.Text);
			Config.SetHTTPUsername (HTTPUsername_entry.Text);


			if (bAreAllSettingsOK)
			{
				if (Checks.CanConnectToHTTP ())
				{
					Destroy ();
				}
				else
				{
					MessageDialog dialog = new MessageDialog (
						null, DialogFlags.Modal, 
						MessageType.Warning, 
						ButtonsType.Ok, 
						Mono.Unix.Catalog.GetString("Failed to connect to the HTTP server. Please check your HTTP settings."));

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
		/// Raises the HTTPURL entry changed event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnHTTPURLEntryChanged (object sender, EventArgs e)
		{
			//Set the base colour back to normal
			HTTPURL_entry.ModifyBase (StateType.Normal);
		}
	}
}

