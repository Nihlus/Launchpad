using System;
using Gtk;

namespace Launchpad_Launcher
{
	public partial class MainWindow : Gtk.Window
	{
		public MainWindow () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}
	}
}

