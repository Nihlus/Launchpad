using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gtk;

namespace Launchpad_Launcher
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			ChecksHandler Checks = new ChecksHandler ();
			if (Checks.IsRunningOnUnix ())
			{
				Gtk.Application.Init ();

				MainWindow win = new MainWindow ();

				win.Show ();
				Gtk.Application.Run ();
			}
			else
			{
				System.Windows.Forms.Application.EnableVisualStyles();
				System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
				System.Windows.Forms.Application.Run(new Form1());
			}
            
        }
    }
}
