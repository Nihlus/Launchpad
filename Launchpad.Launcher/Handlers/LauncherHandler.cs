//
//  LauncherHandler.cs
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
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Launchpad.Common;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Handlers.Protocols;
using Launchpad.Launcher.Utility;
using Microsoft.Extensions.Logging;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace Launchpad.Launcher.Handlers
{
    /// <summary>
    /// This class has a lot of async stuff going on. It handles updating the launcher
    /// and loading the changelog from the server.
    /// Since this class starts new threads in which it does the larger computations,
    /// there must be no usage of UI code in this class. Keep it clean.
    /// </summary>
    public sealed class LauncherHandler
    {
        // Replace the variables in the script with actual data
        private const string TempDirectoryVariable = "%temp%";
        private const string LocalInstallDirectoryVariable = "%localDir%";
        private const string LocalExecutableName = "%launchpadExecutable%";

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private readonly ILogger<LauncherHandler> _log;

        /// <summary>
        /// Raised whenever the launcher finishes downloading.
        /// </summary>
        public event EventHandler? LauncherDownloadFinished;

        /// <summary>
        /// Raised whenever the launcher download progress changes.
        /// </summary>
        public event EventHandler<ModuleProgressChangedArgs>? LauncherDownloadProgressChanged;

        private readonly PatchProtocolHandler _patch;

        /// <summary>
        /// The config handler reference.
        /// </summary>
        private readonly ILaunchpadConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Launchpad.Launcher.Handlers.LauncherHandler"/> class.
        /// </summary>
        /// <param name="log">The logging instance.</param>
        /// <param name="patch">The patch protocol.</param>
        /// <param name="configuration">The configuration.</param>
        public LauncherHandler
        (
            ILogger<LauncherHandler> log,
            PatchProtocolHandler patch,
            ILaunchpadConfiguration configuration
        )
        {
            _log = log;
            _patch = patch;
            _configuration = configuration;

            _patch.ModuleDownloadProgressChanged += OnLauncherDownloadProgressChanged;
            _patch.ModuleInstallationFinished += OnLauncherDownloadFinished;
        }

        /// <summary>
        /// Updates the launcher asynchronously.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateLauncherAsync()
        {
            try
            {
                _log.LogInformation($"Starting update of lancher files using protocol \"{_patch.GetType().Name}\"");

                await _patch.UpdateModuleAsync(EModule.Launcher);
            }
            catch (IOException ioex)
            {
                _log.LogWarning("The launcher update failed (IOException): " + ioex.Message);
            }
        }

        /// <summary>
        /// Checks if the launcher can access the standard HTTP changelog.
        /// </summary>
        /// <returns><c>true</c> if the changelog can be accessed; otherwise, <c>false</c>.</returns>
        public async Task<bool> CanAccessStandardChangelog()
        {
            if (string.IsNullOrEmpty(_configuration.ChangelogAddress.AbsoluteUri))
            {
                return false;
            }

            var address = _configuration.ChangelogAddress;

            // Only allow HTTP URIs
            if (!(address.Scheme == "http" || address.Scheme == "https"))
            {
                return false;
            }

            var headRequest = (HttpWebRequest)WebRequest.Create(address);
            headRequest.Method = "HEAD";

            try
            {
                using var headResponse = (HttpWebResponse)await headRequest.GetResponseAsync();
                return headResponse.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException wex)
            {
                _log.LogWarning("Could not access standard changelog (WebException): " + wex.Message);
                return false;
            }
        }

        /// <summary>
        /// Creates the update script on disk.
        /// </summary>
        /// <returns>ProcessStartInfo for the update script.</returns>
        public ProcessStartInfo CreateUpdateScript()
        {
            try
            {
                var updateScriptPath = GetUpdateScriptPath();
                var updateScriptSource = GetUpdateScriptSource();

                File.WriteAllText(updateScriptPath, updateScriptSource);

                if (PlatformHelpers.IsRunningOnUnix())
                {
                    var chmod = Process.Start("chmod", $"+x {updateScriptPath}");
                    chmod?.WaitForExit();
                }

                var updateShellProcess = new ProcessStartInfo
                {
                    FileName = updateScriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                return updateShellProcess;
            }
            catch (IOException ioex)
            {
                _log.LogWarning("Failed to create update script (IOException): " + ioex.Message);

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Extracts the bundled update script and populates the variables in it
        /// with the data needed for the update procedure.
        /// </summary>
        private string GetUpdateScriptSource()
        {
            // Load the script from the embedded resources
            var localAssembly = Assembly.GetExecutingAssembly();

            var scriptSource = string.Empty;
            var resourceName = GetUpdateScriptResourceName();
            using (var resourceStream = localAssembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream != null)
                {
                    using var reader = new StreamReader(resourceStream);
                    scriptSource = reader.ReadToEnd();
                }
            }

            var transientScriptSource = scriptSource;

            transientScriptSource = transientScriptSource.Replace(TempDirectoryVariable, Path.GetTempPath());
            transientScriptSource = transientScriptSource.Replace(LocalInstallDirectoryVariable, DirectoryHelpers.GetLocalLauncherDirectory());
            transientScriptSource = transientScriptSource.Replace(LocalExecutableName, Path.GetFileName(localAssembly.Location));

            return transientScriptSource;
        }

        /// <summary>
        /// Gets the name of the embedded update script.
        /// </summary>
        private static string GetUpdateScriptResourceName()
        {
            if (PlatformHelpers.IsRunningOnUnix())
            {
                return "Launchpad.Launcher.Resources.launchpad_update.sh";
            }
            else
            {
                 return "Launchpad.Launcher.Resources.launchpad_update.bat";
            }
        }

        /// <summary>
        /// Gets the name of the embedded update script.
        /// </summary>
        private static string GetUpdateScriptPath()
        {
            if (PlatformHelpers.IsRunningOnUnix())
            {
                return Path.Combine(Path.GetTempPath(), "launchpad_update.sh");
            }

            return Path.Combine(Path.GetTempPath(), "launchpad_update.bat");
        }

        private void OnLauncherDownloadProgressChanged(object? sender, ModuleProgressChangedArgs e)
        {
            this.LauncherDownloadProgressChanged?.Invoke(sender, e);
        }

        private void OnLauncherDownloadFinished(object? sender, EModule e)
        {
            this.LauncherDownloadFinished?.Invoke(sender, EventArgs.Empty);
        }
    }
}
