//
//  Program.cs
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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using GLib;
using Launchpad.Common.Handlers.Manifest;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Handlers;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Handlers.Protocols.Manifest;
using Launchpad.Launcher.Interface;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NGettext;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace Launchpad.Launcher
{
    /// <summary>
    /// The main program class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Holds the logging instance for this class.
        /// </summary>
        private static ILogger<Program>? Log;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static async Task Main(string[] args)
        {
            ExceptionManager.UnhandledException += OnUnhandledGLibException;

            Application.Init();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Environment.SetEnvironmentVariable("GSETTINGS_SCHEMA_DIR", "share\\glib-2.0\\schemas\\");
            }

            const string configurationName = "Launchpad.Launcher.log4net.config";
            var logConfig = new XmlDocument();
            await using (var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(configurationName))
            {
                if (configStream is null)
                {
                    throw new InvalidOperationException("The log4net configuration stream could not be found.");
                }

                logConfig.Load(configStream);
            }

            var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
            XmlConfigurator.Configure(repo, logConfig["log4net"]);

            var host = CreateHostBuilder(args).Build();

            Log = host.Services.GetRequiredService<ILogger<Program>>();
            var app = host.Services.GetRequiredService<Startup>();

            app.Start();
            Application.Run();
        }

        /// <summary>
        /// Passes any unhandled exceptions from the GTK UI to the generic handler.
        /// </summary>
        /// <param name="args">The event object containing the information about the exception.</param>
        private static void OnUnhandledGLibException(UnhandledExceptionArgs args)
        {
            Log.LogError((Exception)args.ExceptionObject, "Unhandled GLib exception.");
        }

        private static IHostBuilder CreateHostBuilder(string[] args) => new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "config"));
                config.AddJsonFile("appsettings.json");
            })
            .ConfigureServices((hostingContext, services) =>
            {
                services
                    .AddSingleton<FTPProtocolHandler>()
                    .AddSingleton<HTTPProtocolHandler>()
                    .AddSingleton<ChecksHandler>()
                    .AddSingleton<ConfigHandler>()
                    .AddSingleton<GameHandler>()
                    .AddSingleton<LauncherHandler>()
                    .AddSingleton<GameArgumentService>()
                    .AddSingleton<LocalVersionService>()
                    .AddSingleton<TagfileService>()
                    .AddSingleton<DirectoryHelpers>()
                    .AddSingleton
                    (
                        s =>
                        {
                            var configuration = s.GetRequiredService<ILaunchpadConfiguration>();
                            return new ManifestHandler
                            (
                                DirectoryHelpers.GetLocalLauncherDirectory(),
                                configuration.RemoteAddress,
                                configuration.SystemTarget
                            );
                        }
                    )
                    .AddSingleton(s => s.GetRequiredService<ConfigHandler>().Configuration)
                    .AddSingleton<ICatalog>(s => new Catalog("Launchpad", "./Content/locale"))
                    .AddSingleton<PatchProtocolHandler>
                    (
                        s =>
                        {
                            var configuration = s.GetRequiredService<ILaunchpadConfiguration>();
                            var remoteAddress = configuration.RemoteAddress;
                            switch (remoteAddress.Scheme.ToLowerInvariant())
                            {
                                case "ftp":
                                {
                                    return s.GetRequiredService<FTPProtocolHandler>();
                                }
                                case "http":
                                case "https":
                                {
                                    return s.GetRequiredService<HTTPProtocolHandler>();
                                }
                                default:
                                {
                                    throw new ArgumentException
                                    (
                                        $"No compatible protocol handler found for a URI of the form " +
                                        $"\"{remoteAddress}\"."
                                    );
                                }
                            }
                        }
                    )
                    .AddSingleton(new Application("net.Launchpad.Launchpad", ApplicationFlags.None))
                    .AddSingleton<Startup>();

                services
                    .AddTransient(MainWindow.Create);
            })
            .ConfigureLogging(l =>
            {
                l.ClearProviders();
                l.AddLog4Net();
            });
    }
}
