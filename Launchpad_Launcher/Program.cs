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
			Program main = new Program ();
			if (main.IsRunningOnUnix ())
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

		private bool IsRunningOnUnix()
		{
			int p = (int) Environment.OSVersion.Platform;
			if ((p == 4) || (p == 6) || (p == 128)) 
			{
				Console.WriteLine ("Running on Unix");
				return true;
			} 
			else 
			{
				Console.WriteLine ("Not running on Unix");
				return false;
			}
		}
    }
}
