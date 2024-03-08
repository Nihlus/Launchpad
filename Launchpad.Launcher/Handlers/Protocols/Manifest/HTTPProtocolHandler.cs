//
//  HTTPProtocolHandler.cs
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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Launchpad.Common.Enums;
using Launchpad.Common.Handlers.Manifest;
using Launchpad.Launcher.Configuration;
using Launchpad.Launcher.Services;
using Launchpad.Launcher.Utility;
using Microsoft.Extensions.Logging;
using NGettext;
using Remora.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Launchpad.Launcher.Handlers.Protocols.Manifest
{
    /// <summary>
    /// HTTP protocol handler. Patches the launcher and game using the
    /// HTTP/HTTPS protocol.
    /// </summary>
    internal sealed class HTTPProtocolHandler : ManifestBasedProtocolHandler
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private readonly ILogger<HTTPProtocolHandler> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="HTTPProtocolHandler"/> class.
        /// </summary>
        /// <param name="log">The logging instance.</param>
        /// <param name="localVersionService">The local version service.</param>
        /// <param name="fileManifestHandler">The manifest handler.</param>
        /// <param name="localizationCatalog">The localization catalog.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="tagfileService">The tagfile service.</param>
        /// <param name="directoryHelpers">The directory helpers.</param>
        public HTTPProtocolHandler
        (
            ILogger<HTTPProtocolHandler> log,
            LocalVersionService localVersionService,
            ManifestHandler fileManifestHandler,
            ICatalog localizationCatalog,
            ILaunchpadConfiguration configuration,
            TagfileService tagfileService,
            DirectoryHelpers directoryHelpers
        )
            : base
            (
                log,
                localVersionService,
                fileManifestHandler,
                localizationCatalog,
                configuration,
                tagfileService,
                directoryHelpers
            )
        {
            _log = log;
        }

        /// <inheritdoc />
        public override async Task<Result<bool>> CanPatchAsync()
        {
            _log.LogInformation("Pinging remote patching server to determine if we can connect to it.");

            var canConnect = false;

            try
            {
                var getPlainRequest = CreateHttpWebRequest
                (
                    this.Configuration.RemoteAddress.AbsoluteUri,
                    this.Configuration.RemoteUsername,
                    this.Configuration.RemotePassword
                );

                if (!getPlainRequest.IsSuccess)
                {
                    return Result<bool>.FromError(getPlainRequest);
                }

                var plainRequest = getPlainRequest.Entity;

                plainRequest.Method = WebRequestMethods.Http.Head;
                plainRequest.Timeout = 4000;

                try
                {
                    using var response = (HttpWebResponse)await plainRequest.GetResponseAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        canConnect = true;
                    }
                }
                catch (WebException wex)
                {
                    _log.LogWarning("Unable to connect to remote patch server (WebException): " + wex.Message);
                    canConnect = false;
                }
            }
            catch (WebException wex)
            {
                _log.LogWarning("Unable to connect due a malformed url in the configuration (WebException): " + wex.Message);
                canConnect = false;
            }

            return canConnect;
        }

        /// <inheritdoc />
        public override Task<Result<bool>> IsPlatformAvailableAsync(ESystemTarget platform)
        {
            var remote = $"{this.Configuration.RemoteAddress}/game/{platform}/.provides";

            return DoesRemoteDirectoryOrFileExistAsync(remote);
        }

        /// <inheritdoc />
        public override Task<Result<string>> GetChangelogMarkupAsync()
        {
            var changelogURL = $"{this.Configuration.RemoteAddress}/launcher/changelog.pango";
            return ReadRemoteFileAsync(changelogURL);
        }

        /// <inheritdoc />
        public override Task<Result<bool>> CanProvideBannerAsync()
        {
            var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";

            return DoesRemoteDirectoryOrFileExistAsync(bannerURL);
        }

        /// <inheritdoc />
        public override async Task<Result<Image<Rgba32>>> GetBannerAsync()
        {
            var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";
            var localBannerPath = Path.Combine(Path.GetTempPath(), "banner.png");

            await DownloadRemoteFileAsync(bannerURL, localBannerPath);
            return Image.Load<Rgba32>(localBannerPath);
        }

        /// <inheritdoc />
        protected override async Task<Result> DownloadRemoteFileAsync
        (
            string url,
            string localPath,
            long totalSize = 0,
            long contentOffset = 0,
            bool useAnonymousLogin = false
        )
        {
            // Clean the url string
            var remoteURL = url.Replace(Path.DirectorySeparatorChar, '/');

            string username;
            string password;
            if (useAnonymousLogin)
            {
                username = "anonymous";
                password = "anonymous";
            }
            else
            {
                username = this.Configuration.RemoteUsername;
                password = this.Configuration.RemotePassword;
            }

            try
            {
                var getRequest = CreateHttpWebRequest(remoteURL, username, password);

                if (!getRequest.IsSuccess)
                {
                    return Result.FromError(getRequest);
                }

                var request = getRequest.Entity;

                request.Method = WebRequestMethods.Http.Get;
                request.AddRange(contentOffset);

                await using var contentStream = (await request.GetResponseAsync()).GetResponseStream();
                await using var fileStream = contentOffset > 0
                    ? new FileStream(localPath, FileMode.Append)
                    : new FileStream(localPath, FileMode.Create);

                fileStream.Position = contentOffset;
                var totalBytesDownloaded = contentOffset;

                long totalFileSize;
                if (contentStream.CanSeek)
                {
                    totalFileSize = contentOffset + contentStream.Length;
                }
                else
                {
                    totalFileSize = totalSize;
                }

                var bufferSize = this.Configuration.RemoteFileDownloadBufferSize;
                var buffer = new byte[bufferSize];

                while (true)
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    fileStream.Write(buffer, 0, bytesRead);

                    totalBytesDownloaded += bytesRead;

                    // Report download progress
                    this.ModuleDownloadProgressArgs.ProgressBarMessage = GetDownloadProgressBarMessage
                    (
                        Path.GetFileName(remoteURL),
                        totalBytesDownloaded,
                        totalFileSize
                    );
                    this.ModuleDownloadProgressArgs.ProgressFraction = totalBytesDownloaded / (double)totalFileSize;
                    OnModuleDownloadProgressChanged();
                }

                fileStream.Flush();
            }
            catch (WebException wex)
            {
                _log.LogError("Failed to download the remote file at \"{RemoteUrl}\"", remoteURL);
                return wex;
            }
            catch (IOException ioex)
            {
                _log.LogError("Failed to download the remote file at \"{RemoteUrl}\"", remoteURL);
                return ioex;
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        protected override async Task<Result<string>> ReadRemoteFileAsync
        (
            string url,
            bool useAnonymousLogin = false
        )
        {
            var remoteURL = url.Replace(Path.DirectorySeparatorChar, '/');

            string username;
            string password;
            if (useAnonymousLogin)
            {
                username = "anonymous";
                password = "anonymous";
            }
            else
            {
                username = this.Configuration.RemoteUsername;
                password = this.Configuration.RemotePassword;
            }

            try
            {
                var getRequest = CreateHttpWebRequest(remoteURL, username, password);

                if (!getRequest.IsSuccess)
                {
                    return Result<string>.FromError(getRequest);
                }

                var request = getRequest.Entity;

                request.Method = WebRequestMethods.Http.Get;

                var data = string.Empty;
                await using var remoteStream = (await request.GetResponseAsync()).GetResponseStream();

                var bufferSize = this.Configuration.RemoteFileDownloadBufferSize;
                var buffer = new byte[bufferSize];

                while (true)
                {
                    var bytesRead = await remoteStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    data += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }

                return data;
            }
            catch (WebException wex)
            {
                _log.LogError("Failed to read the contents of remote file \"{RemoteUrl}\"", remoteURL);
                return wex;
            }
        }

        /// <summary>
        /// Creates a HTTP web request.
        /// </summary>
        /// <returns>The HTTP web request.</returns>
        /// <param name="remotePath">url of the desired remote object.</param>
        /// <param name="username">The username used for authentication.</param>
        /// <param name="password">The password used for authentication.</param>
        private Result<HttpWebRequest> CreateHttpWebRequest
        (
            string remotePath,
            string username,
            string password
        )
        {
            if (!remotePath.StartsWith(this.Configuration.RemoteAddress.AbsoluteUri))
            {
                remotePath = $"{this.Configuration.RemoteAddress}/{remotePath}";
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(new Uri(remotePath));
                request.Credentials = new NetworkCredential(username, password);

                return request;
            }
            catch (WebException wex)
            {
                _log.LogError(wex, "Unable to create a WebRequest for the specified file \"{RemotePath}\"", remotePath);
                return wex;
            }
            catch (ArgumentException aex)
            {
                _log.LogError(aex, "Unable to create a WebRequest for the specified file \"{RemotePath}\"", remotePath);
                return aex;
            }
            catch (UriFormatException uex)
            {
                _log.LogError
                (
                    uex,
                    "Unable to create a WebRequest for the specified file. You may need to add \"ftp://\" before the url in the config"
                );

                return uex;
            }
        }

        /// <summary>
        /// Checks if the provided path points to a valid directory or file.
        /// </summary>
        /// <returns><c>true</c>, if the directory or file exists, <c>false</c> otherwise.</returns>
        /// <param name="url">The remote url of the directory or file.</param>
        private async Task<Result<bool>> DoesRemoteDirectoryOrFileExistAsync(string url)
        {
            var cleanURL = url.Replace(Path.DirectorySeparatorChar, '/');
            var getRequest = CreateHttpWebRequest
            (
                cleanURL,
                this.Configuration.RemoteUsername,
                this.Configuration.RemotePassword
            );

            if (!getRequest.IsSuccess)
            {
                return Result<bool>.FromError(getRequest);
            }

            var request = getRequest.Entity;

            request.Method = WebRequestMethods.Http.Head;
            try
            {
                using var response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
            }
            catch (WebException wex)
            {
                using var response = (HttpWebResponse?)wex.Response;
                if (response?.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                return Result<bool>.FromError(wex);
            }

            return true;
        }
    }
}
