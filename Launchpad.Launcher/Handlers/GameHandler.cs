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
using System.Threading;

using Launchpad.Common;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using NLog;

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
	/// there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class GameHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Event raised whenever the progress of installing or updating the game changes.
		/// </summary>
		public event EventHandler<ModuleProgressChangedArgs> ProgressChanged;

		/// <summary>
		/// Event raised whenever the game finishes downloading, regardless of whether or not it's updating
		/// or installing.
		/// </summary>
		public event EventHandler DownloadFinished;

		/// <summary>
		/// Event raised whenever the game fails to download, regardless of whether or not it's updating
		/// or installing.
		/// </summary>
		public event EventHandler DownloadFailed;

		/// <summary>
		/// Event raised whenever the game fails to launch.
		/// </summary>
		public event EventHandler LaunchFailed;

		/// <summary>
		/// Event raised whenever the game exits.
		/// </summary>
		public event EventHandler<int> GameExited;

		// ...
		private static readonly ILaunchpadConfiguration Configuration = ConfigHandler.Instance.Configuration;

		private readonly PatchProtocolHandler Patch;

		private readonly GameArgumentService GameArgumentService = new GameArgumentService();

		/// <summary>
		/// Initializes a new instance of the <see cref="GameHandler"/> class.
		/// </summary>
		public GameHandler()
		{
			this.Patch = PatchProtocolProvider.GetHandler();
			if (this.Patch == null)
			{
				return;
			}

			this.Patch.ModuleDownloadProgressChanged += OnModuleInstallProgressChanged;
			this.Patch.ModuleVerifyProgressChanged += OnModuleInstallProgressChanged;
			this.Patch.ModuleUpdateProgressChanged += OnModuleInstallProgressChanged;

			this.Patch.ModuleInstallationFinished += OnModuleInstallationFinished;
			this.Patch.ModuleInstallationFailed += OnModuleInstallationFailed;
		}

		/// <summary>
		/// Starts an asynchronous game installation task.
		/// </summary>
		public void InstallGame()
		{
			Log.Info($"Starting installation of game files using protocol \"{this.Patch.GetType().Name}\"");
			var t = new Thread(this.Patch.InstallGame)
			{
				Name = "InstallGame",
				IsBackground = true
			};

			t.Start();
		}

		/// <summary>
		/// Starts an asynchronous game update task.
		/// </summary>
		public void UpdateGame()
		{
			Log.Info($"Starting update of game files using protocol \"{this.Patch.GetType().Name}\"");
			var t = new Thread(() => this.Patch.UpdateModule(EModule.Game))
			{
				Name = "UpdateGame",
				IsBackground = true
			};

			t.Start();
		}

		/// <summary>
		/// Starts an asynchronous game verification task.
		/// </summary>
		public void VerifyGame()
		{
			Log.Info("Beginning verification of game files.");
			var t = new Thread(() => this.Patch.VerifyModule(EModule.Game))
			{
				Name = "VerifyGame",
				IsBackground = true
			};

			t.Start();
		}

		/// <summary>
		/// Deletes all local data and installs the game again.
		/// </summary>
		public void ReinstallGame()
		{
			Log.Info("Beginning full reinstall of game files.");
			if (Directory.Exists(DirectoryHelpers.GetLocalGameDirectory()))
			{
				Log.Info("Deleting existing game files.");
				Directory.Delete(DirectoryHelpers.GetLocalGameDirectory(), true);
			}

			if (File.Exists(DirectoryHelpers.GetGameTagfilePath()))
			{
				Log.Info("Deleting install progress cookie.");
				File.Delete(DirectoryHelpers.GetGameTagfilePath());
			}

			var t = new Thread(() => this.Patch.InstallGame())
			{
				Name = "ReinstallGame",
				IsBackground = true
			};

			t.Start();
		}

		/// <summary>
		/// Launches the game.
		/// </summary>
		public void LaunchGame()
		{
			try
			{
				var executable = Path.Combine(DirectoryHelpers.GetLocalGameDirectory(), Configuration.ExecutablePath);
				if (!File.Exists(executable))
				{
					throw new FileNotFoundException($"Game executable at path (\"{executable}\") not found.");
				}

				var executableDir = Path.GetDirectoryName(executable) ?? DirectoryHelpers.GetLocalLauncherDirectory();

				// Do not move the argument assignment inside the gameStartInfo initializer.
				// It causes a TargetInvocationException crash through black magic.
				var gameArguments = string.Join(" ", this.GameArgumentService.GetGameArguments());
				var gameStartInfo = new ProcessStartInfo
				{
					FileName = executable,
					Arguments = gameArguments,
					WorkingDirectory = executableDir
				};

				Log.Info($"Launching game. \n\tExecutable path: {gameStartInfo.FileName}");

				var gameProcess = new Process
				{
					StartInfo = gameStartInfo,
					EnableRaisingEvents = true
				};

				gameProcess.Exited += (sender, args) =>
				{
					if (gameProcess.ExitCode != 0)
					{
						Log.Info
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
				Log.Warn($"Game launch failed (FileNotFoundException): {fex.Message}");
				Log.Warn("If the game executable is there, try overriding the executable name in the configuration file.");

				OnGameLaunchFailed();
			}
			catch (IOException ioex)
			{
				Log.Warn($"Game launch failed (IOException): {ioex.Message}");

				OnGameLaunchFailed();
			}
		}

		/// <summary>
		/// Passes the internal event in the protocol handler to the outward-facing
		/// event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void OnModuleInstallProgressChanged(object sender, ModuleProgressChangedArgs e)
		{
			this.ProgressChanged?.Invoke(sender, e);
		}

		/// <summary>
		/// Passes the internal event in the protocol handler to the outward-facing
		/// event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void OnModuleInstallationFinished(object sender, EModule e)
		{
			this.DownloadFinished?.Invoke(sender, EventArgs.Empty);
		}

		/// <summary>
		/// Passes the internal event in the protocol handler to the outward-facing
		/// event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void OnModuleInstallationFailed(object sender, EModule e)
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
