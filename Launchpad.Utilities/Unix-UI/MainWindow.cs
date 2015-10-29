using System;
using Gtk;

using Launchpad.Utilities.Events.Arguments;

namespace Launchpad.Utilities
{
	[CLSCompliant (false)]
	public partial class MainWindow : Gtk.Window
	{
		public MainWindow () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();
			fileChooser.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.DesktopDirectory));
			progressLabel.Text = "Idle";
		}

		/// <summary>
		/// Exits the application properly when the window is deleted.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}

		protected void OnGenerateManifestButtonClicked (object sender, EventArgs e)
		{
			generateManifestButton.Sensitive = false;

			string TargetDirectory = fileChooser.CurrentFolder;

			ManifestHandler Manifest = new ManifestHandler (TargetDirectory);

			Manifest.ManifestGenerationProgressChanged += OnGenerateManifestProgressChanged;
			Manifest.ManifestGenerationFinished += OnGenerateManifestFinished;

			Manifest.GenerateManifest ();
		}

		/// <summary>
		/// Updates the UI when a file is entered into the manifest.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Arguments containing information about the entered file.</param>
		protected void OnGenerateManifestProgressChanged (object sender, ManifestGenerationProgressChangedEventArgs e)
		{
			Application.Invoke (delegate
			{
				progressLabel.Text = String.Format ("{0} : {1} out of {2}", e.Filepath, e.CompletedFiles, e.TotalFiles);

				progressbar.Fraction = (double)e.CompletedFiles / (double)e.TotalFiles;
			});
		}

		/// <summary>
		/// Updates the UI when the manifest is complete.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Empty arguments</param>
		protected void OnGenerateManifestFinished (object sender, EventArgs e)
		{
			Application.Invoke (delegate
			{
				progressLabel.Text = "Finished";
				generateManifestButton.Sensitive = true;
			});
		}
	}
}

