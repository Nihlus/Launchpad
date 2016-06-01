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
using Launchpad.Launcher.UnixUI;
using Launchpad.Launcher.WindowsUI;
using Launchpad.Launcher.Handlers;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;

[assembly: CLSCompliant(true)]
namespace Launchpad.Launcher
{
	class Program
	{
		/// <summary>
		/// The config handler reference.
		/// </summary>
		private static readonly ConfigHandler Config = ConfigHandler.Instance;

		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// Bind any unhandled exceptions in the main thread so that they are logged.
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			Log.Info("----------------");
			Log.Info(String.Format("Launchpad v{0} starting...", Config.GetLocalLauncherVersion()));
			Log.Info(String.Format("Current platform: {0} ({1})", ConfigHandler.GetCurrentPlatform(), Environment.Is64BitOperatingSystem ? "x64" : "x86"));

			// Set correct working directory for compatibility with double-clicking
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			if (ChecksHandler.IsRunningOnUnix())
			{
				Log.Info("Initializing GTK UI.");

				// Bind any unhandled exceptions in the GTK UI so that they are logged.
				GLib.ExceptionManager.UnhandledException += OnGLibUnhandledException;

				// Run the GTK UI
				Gtk.Application.Init();
				MainWindow win = new MainWindow();
				win.Show();
				Gtk.Application.Run();
			}
			else
			{
				Log.Info("Initializing WinForms UI.");

				// Bind any unhandled exceptions in the WinForms UI so that they are logged.
				System.Windows.Forms.Application.ThreadException += OnFormsThreadException;

				// run a WinForms UI instead of GTK
				System.Windows.Forms.Application.EnableVisualStyles();
				System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
				System.Windows.Forms.Application.Run(new MainForm());
			}
		}


		/// <summary>
		/// Passes any unhandled exceptions from the Forms UI to the generic handler.
		/// </summary>
		/// <param name="sender">The sending object.</param>
		/// <param name="threadExceptionEventArgs">The event object containing the information about the exception.</param>
		private static void OnFormsThreadException(object sender, ThreadExceptionEventArgs threadExceptionEventArgs)
		{
			OnUnhandledException(sender, new UnhandledExceptionEventArgs(threadExceptionEventArgs.Exception, true));
		}

		/// <summary>
		/// Passes any unhandled exceptions from the GTK UI to the generic handler.
		/// </summary>
		/// <param name="args">The event object containing the information about the exception.</param>
		private static void OnGLibUnhandledException(GLib.UnhandledExceptionArgs args)
		{
			OnUnhandledException(null, new UnhandledExceptionEventArgs(args.ExceptionObject, args.IsTerminating));
		}

		/// <summary>
		///	Event handler for all unhandled exceptions that may be encountered during runtime. While there should never
		/// be any unhandled exceptions in an ideal program, unexpected issues can and will arise. This handler logs
		/// the exception and all relevant information to a logfile and prints it to the console for debugging purposes.
		/// </summary>
		/// <param name="sender">The sending object.</param>
		/// <param name="unhandledExceptionEventArgs">The event object containing the information about the exception.</param>
		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
		{
			Log.Fatal("----------------");
			Log.Fatal("FATAL UNHANDLED EXCEPTION!");
			Log.Fatal("Something has gone terribly, terribly wrong during runtime.");
			Log.Fatal("The following is what information could be gathered by the program before crashing.");
			Log.Fatal("Please report this to <jarl.gullberg@gmail.com> or via GitHub. Include the full log and a " +
			          "description of what you were doing when it happened.");

			Exception unhandledException = unhandledExceptionEventArgs.ExceptionObject as Exception;
			if (unhandledException != null)
			{
				Log.Fatal("Exception type: " + unhandledException.GetType().FullName);
				Log.Fatal("Exception Message: " + unhandledException.Message);
				Log.Fatal("Exception Stacktrace: " + unhandledException.StackTrace);
			}
		}
	}
}
