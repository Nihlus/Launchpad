//
//  GameHandler.cs
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using log4net;
using Launchpad.Launcher.Handlers.Protocols;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	///  This class has a lot of async stuff going on. It handles installing the game
	///  and updating it when it needs to.
	///
	///  The download protocol is selected based on the configuration each time this is
	///  instantiated, and control is then handed over to whatever the protocol needs
	///  to do.
	///
	///	 Since this class starts new threads in which it does the larger computations,
	///	 there must be no useage of UI code in this class. Keep it clean!
	/// </summary>
	internal sealed class GameHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(GameHandler));

		/// <summary>
		/// Event raised whenever the progress of installing or updating the game changes.
		/// </summary>
		public event ModuleInstallationProgressChangedEventHandler ProgressChanged;

		/// <summary>
		/// Event raised whenever the game finishes downloading, regardless of whether or not it's updating
		/// or installing.
		/// </summary>
		public event GameInstallationFinishedEventHandler DownloadFinished;

		/// <summary>
		/// Event raised whenever the game fails to download, regardless of whether or not it's updating
		/// or installing.
		/// </summary>
		public event GameInstallationFailedEventHander DownloadFailed;

		/// <summary>
		/// Event raised whenever the game fails to launch.
		/// </summary>
		public event GameLaunchFailedEventHandler LaunchFailed;

		/// <summary>
		/// Event raised whenever the game exits.
		/// </summary>
		public event GameExitEventHandler GameExited;

		// ...
		private readonly GameExitEventArgs GameExitArgs = new GameExitEventArgs();

		/// <summary>
		/// The config handler reference.
		/// </summary>
		private static readonly ConfigHandler Config = ConfigHandler.Instance;

		private readonly PatchProtocolHandler Patch;

		/// <summary>
		/// Creates a new instance of the <see cref="GameHandler"/> class.
		/// </summary>
		public GameHandler()
		{
			Patch = Config.GetPatchProtocol();
			if (Patch != null)
			{
				Patch.ModuleDownloadProgressChanged += OnModuleInstallProgressChanged;
				Patch.ModuleVerifyProgressChanged += OnModuleInstallProgressChanged;
				Patch.ModuleUpdateProgressChanged += OnModuleInstallProgressChanged;

				Patch.ModuleInstallationFinished += OnModuleInstallationFinished;
				Patch.ModuleInstallationFailed += OnModuleInstallationFailed;
			}
		}

		/// <summary>
		/// Starts an asynchronous game installation task.
		/// </summary>
		public void InstallGame()
		{
			Log.Info($"Starting installation of game files using protocol \"{this.Patch.GetType().Name}\"");
			Thread t = new Thread(this.Patch.InstallGame);
			t.Start();
		}

		/// <summary>
		/// Starts an asynchronous game update task.
		/// </summary>
		public void UpdateGame()
		{
			Log.Info($"Starting update of game files using protocol \"{this.Patch.GetType().Name}\"");
			Thread t = new Thread(() => this.Patch.UpdateModule(EModule.Game));
			t.Start();
		}

		/// <summary>
		/// Starts an asynchronous game verification task.
		/// </summary>
		public void VerifyGame()
		{
			Log.Info("Beginning verification of game files.");
			Thread t = new Thread(() => this.Patch.VerifyModule(EModule.Game));
			t.Start();
		}

		public void ReinstallGame()
		{
			Log.Info("Beginning full reinstall of game files.");
			if (Directory.Exists(Config.GetGamePath()))
			{
				Log.Info("Deleting existing game files.");
				Directory.Delete(Config.GetGamePath(), true);
			}

			if (File.Exists(ConfigHandler.GetInstallCookiePath()))
			{
				Log.Info("Deleting install progress cookie.");
				File.Delete(ConfigHandler.GetInstallCookiePath());
			}

			Thread t = new Thread(() => this.Patch.InstallGame());
			t.Start();
		}

		/// <summary>
		/// Launches the game.
		/// </summary>
		public void LaunchGame()
		{
			//start new process of the game executable
			try
			{
				ProcessStartInfo gameStartInfo = new ProcessStartInfo
				{
					UseShellExecute = false,
					FileName = Config.GetGameExecutable()
				};
				GameExitArgs.GameName = Config.GetGameName();

				Log.Info($"Launching game. \n\tExecutable path: {gameStartInfo.FileName}");

				Process gameProcess = new Process
				{
					StartInfo = gameStartInfo,
					EnableRaisingEvents = true
				};

				gameProcess.Exited += delegate
				{
					if (gameProcess.ExitCode != 0)
					{
						Log.Info($"The game exited with an exit code of {gameProcess.ExitCode}. " +
						         "There may have been issues during runtime, or the game may not have started at all.");
					}
					GameExitArgs.ExitCode = gameProcess.ExitCode;
					OnGameExited();

					// Manual disposing
					gameProcess.Dispose();
				};

				gameProcess.Start();
			}
			catch (IOException ioex)
			{
				Log.Warn($"Game launch failed (IOException): {ioex.Message}");
				GameExitArgs.ExitCode = 1;

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
			ProgressChanged?.Invoke(sender, e);
		}

		/// <summary>
		/// Passes the internal event in the protocol handler to the outward-facing
		/// event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void OnModuleInstallationFinished(object sender, ModuleInstallationFinishedArgs e)
		{
			DownloadFinished?.Invoke(sender, e);
		}

		/// <summary>
		/// Passes the internal event in the protocol handler to the outward-facing
		/// event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void OnModuleInstallationFailed(object sender, ModuleInstallationFailedArgs e)
		{
			DownloadFailed?.Invoke(sender, e);
		}

		/// <summary>
		/// Raises the Game Launch Failed event.
		/// </summary>
		private void OnGameLaunchFailed()
		{
			LaunchFailed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raises the Game Exited event.
		/// </summary>
		private void OnGameExited()
		{
			GameExited?.Invoke(this, GameExitArgs);
		}
	}

	/*
		Game-specific delegates
	*/
	public delegate void GameInstallationFinishedEventHandler(object sender,EventArgs e);
	public delegate void GameInstallationFailedEventHander(object sender,EventArgs e);
	public delegate void GameLaunchFailedEventHandler(object sender,EventArgs e);
	public delegate void GameExitEventHandler(object sender,GameExitEventArgs e);

	/*
		Game-specific event arguments
	*/
	public class GameExitEventArgs : EventArgs
	{
		public string GameName
		{
			get;
			set;
		}

		public int ExitCode
		{
			get;
			set;
		}
	}
}

