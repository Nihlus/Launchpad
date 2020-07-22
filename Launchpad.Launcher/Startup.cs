//
//  Startup.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
//

using GLib;
using Gtk;
using Launchpad.Launcher.Interface;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace Launchpad.Launcher
{
    /// <summary>
    /// Represents startup procedures for the application.
    /// </summary>
    public class Startup
    {
        private readonly Application _app;
        private readonly MainWindow _mainWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="app">The application information.</param>
        /// <param name="mainWindow">The main application window.</param>
        public Startup(Application app, MainWindow mainWindow)
        {
            _app = app;
            _mainWindow = mainWindow;
        }

        /// <summary>
        /// Starts the application.
        /// </summary>
        public void Start()
        {
            _app.Register(Cancellable.Current);
            _app.AddWindow(_mainWindow);

            _mainWindow.Show();

            Idle.Add
            (
                () =>
                {
                    async void Initialize() => await InitializeAsync();

                    Initialize();
                    return false;
                }
            );

            _mainWindow.DeleteEvent += DeletedEvent;
        }

        /// <summary>
        /// Asynchronously initializes the application.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            await _mainWindow.InitializeAsync();
        }

        private void DeletedEvent(object o, DeleteEventArgs args)
        {
            Application.Quit();
        }
    }
}
