//
//  GameHandler.cs
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
using System.Diagnostics;
using System.IO;
using Launchpad.Common;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using Microsoft.Extensions.Logging;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace Launchpad.Launcher.Handlers
{
    /// <summary>
    /// This class has a lot of async stuff going on. It handles installing the game
    /// and updating it when it needs to.
    ///
    /// The download protocol is selected based on the configuration each time this is
    /// instantiated, and control is then handed over to whatever the protocol needs
    /// to do.
    ///
    /// Since this class starts new threads in which it does the larger computations,
    /// there must be no usage of UI code in this class. Keep it clean.
    /// </summary>
    public sealed class GameHandler
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private readonly ILogger<GameHandler> _log;

        /// <summary>
        /// Event raised whenever the progress of installing or updating the game changes.
        /// </summary>
        public event EventHandler<ModuleProgressChangedArgs>? ProgressChanged;

        /// <summary>
        /// Event raised whenever the game finishes downloading, regardless of whether or not it's updating
        /// or installing.
        /// </summary>
        public event EventHandler? DownloadFinished;

        /// <summary>
        /// Event raised whenever the game fails to download, regardless of whether or not it's updating
        /// or installing.
        /// </summary>
        public event EventHandler? DownloadFailed;

        /// <summary>
        /// Event raised whenever the game fails to launch.
        /// </summary>
        public event EventHandler? LaunchFailed;

        /// <summary>
        /// Event raised whenever the game exits.
        /// </summary>
        public event EventHandler<int>? GameExited;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly ILaunchpadConfiguration _configuration;

        /// <summary>
        /// The patch protocol.
        /// </summary>
        private readonly PatchProtocolHandler _patch;

        /// <summary>
        /// The game argument service.
        /// </summary>
        private readonly GameArgumentService _gameArgumentService;

        /// <summary>
        /// The directory helpers.
        /// </summary>
        private readonly DirectoryHelpers _directoryHelpers;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameHandler"/> class.
        /// </summary>
        /// <param name="log">The logging instance.</param>
        /// <param name="patch">The patch protocol.</param>
        /// <param name="gameArgumentService">The game argument service.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="directoryHelpers">The directory helpers.</param>
        public GameHandler
        (
            ILogger<GameHandler> log,
            PatchProtocolHandler patch,
            GameArgumentService gameArgumentService,
            ILaunchpadConfiguration configuration,
            DirectoryHelpers directoryHelpers
        )
        {
            _log = log;
            _patch = patch;
            _gameArgumentService = gameArgumentService;
            _configuration = configuration;
            _directoryHelpers = directoryHelpers;

            _patch.ModuleDownloadProgressChanged += OnModuleInstallProgressChanged;
            _patch.ModuleVerifyProgressChanged += OnModuleInstallProgressChanged;
            _patch.ModuleUpdateProgressChanged += OnModuleInstallProgressChanged;

            _patch.ModuleInstallationFinished += OnModuleInstallationFinished;
            _patch.ModuleInstallationFailed += OnModuleInstallationFailed;
        }

        /// <summary>
        /// Starts an asynchronous game installation task.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public async Task InstallGameAsync()
        {
            _log.LogInformation($"Starting installation of game files using protocol \"{_patch.GetType().Name}\"");
            await _patch.InstallGameAsync();
        }

        /// <summary>
        /// Starts an asynchronous game update task.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateGameAsync()
        {
            _log.LogInformation($"Starting update of game files using protocol \"{_patch.GetType().Name}\"");
            await _patch.UpdateModuleAsync(EModule.Game);
        }

        /// <summary>
        /// Starts an asynchronous game verification task.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task VerifyGameAsync()
        {
            _log.LogInformation("Beginning verification of game files.");
            await _patch.VerifyModuleAsync(EModule.Game);
        }

        /// <summary>
        /// Deletes all local data and installs the game again.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ReinstallGameAsync()
        {
            _log.LogInformation("Beginning full reinstall of game files.");
            if (Directory.Exists(_directoryHelpers.GetLocalGameDirectory()))
            {
                _log.LogInformation("Deleting existing game files.");
                Directory.Delete(_directoryHelpers.GetLocalGameDirectory(), true);
            }

            if (File.Exists(_directoryHelpers.GetGameTagfilePath()))
            {
                _log.LogInformation("Deleting install progress cookie.");
                File.Delete(_directoryHelpers.GetGameTagfilePath());
            }

            await _patch.InstallGameAsync();
        }

        /// <summary>
        /// Launches the game.
        /// </summary>
        public void LaunchGame()
        {
            try
            {
                var executable = Path.Combine(_directoryHelpers.GetLocalGameDirectory(), _configuration.ExecutablePath);
                if (!File.Exists(executable))
                {
                    throw new FileNotFoundException($"Game executable at path (\"{executable}\") not found.");
                }

                var executableDir = Path.GetDirectoryName(executable) ?? DirectoryHelpers.GetLocalLauncherDirectory();

                // Do not move the argument assignment inside the gameStartInfo initializer.
                // It causes a TargetInvocationException crash through black magic.
                var gameArguments = string.Join(" ", _gameArgumentService.GetGameArguments());
                var gameStartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = gameArguments,
                    WorkingDirectory = executableDir
                };

                _log.LogInformation($"Launching game. \n\tExecutable path: {gameStartInfo.FileName}");

                var gameProcess = new Process
                {
                    StartInfo = gameStartInfo,
                    EnableRaisingEvents = true
                };

                gameProcess.Exited += (sender, args) =>
                {
                    if (gameProcess.ExitCode != 0)
                    {
                        _log.LogInformation
                        (
                            $"The game exited with an exit code of {gameProcess.ExitCode}. " +
                            "There may have been issues during runtime, or the game may not have started at all."
                        );
                    }

                    OnGameExited(gameProcess.ExitCode);

                    // Manual disposing
                    gameProcess.Dispose();
                };

                // Make sure the game executable is flagged as such on Unix
                if (PlatformHelpers.IsRunningOnUnix())
                {
                    Process.Start("chmod", $"+x {gameStartInfo.FileName}");
                }

                gameProcess.Start();
            }
            catch (FileNotFoundException fex)
            {
                _log.LogWarning($"Game launch failed (FileNotFoundException): {fex.Message}");
                _log.LogWarning("If the game executable is there, try overriding the executable name in the configuration file.");

                OnGameLaunchFailed();
            }
            catch (IOException ioex)
            {
                _log.LogWarning($"Game launch failed (IOException): {ioex.Message}");

                OnGameLaunchFailed();
            }
        }

        /// <summary>
        /// Passes the internal event in the protocol handler to the outward-facing
        /// event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void OnModuleInstallProgressChanged(object? sender, ModuleProgressChangedArgs e)
        {
            this.ProgressChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// Passes the internal event in the protocol handler to the outward-facing
        /// event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void OnModuleInstallationFinished(object? sender, EModule e)
        {
            this.DownloadFinished?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Passes the internal event in the protocol handler to the outward-facing
        /// event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void OnModuleInstallationFailed(object? sender, EModule e)
        {
            this.DownloadFailed?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the Game Launch Failed event.
        /// </summary>
        private void OnGameLaunchFailed()
        {
            this.LaunchFailed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the Game Exited event.
        /// </summary>
        private void OnGameExited(int exitCode)
        {
            this.GameExited?.Invoke(this, exitCode);
        }
    }
}
