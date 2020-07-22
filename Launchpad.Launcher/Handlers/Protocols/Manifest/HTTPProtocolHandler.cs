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
using NLog;
using Remora.Results;
using SixLabors.ImageSharp;
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
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        /// <inheritdoc />
        public override async Task<RetrieveEntityResult<bool>> CanPatchAsync()
        {
            Log.Info("Pinging remote patching server to determine if we can connect to it.");

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
                    return RetrieveEntityResult<bool>.FromError(getPlainRequest);
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
                    Log.Warn("Unable to connect to remote patch server (WebException): " + wex.Message);
                    canConnect = false;
                }
            }
            catch (WebException wex)
            {
                Log.Warn("Unable to connect due a malformed url in the configuration (WebException): " + wex.Message);
                canConnect = false;
            }

            return canConnect;
        }

        /// <inheritdoc />
        public override Task<RetrieveEntityResult<bool>> IsPlatformAvailableAsync(ESystemTarget platform)
        {
            var remote = $"{this.Configuration.RemoteAddress}/game/{platform}/.provides";

            return DoesRemoteDirectoryOrFileExistAsync(remote);
        }

        /// <inheritdoc />
        public override Task<RetrieveEntityResult<string>> GetChangelogMarkupAsync()
        {
            var changelogURL = $"{this.Configuration.RemoteAddress}/launcher/changelog.pango";
            return ReadRemoteFileAsync(changelogURL);
        }

        /// <inheritdoc />
        public override Task<RetrieveEntityResult<bool>> CanProvideBannerAsync()
        {
            var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";

            return DoesRemoteDirectoryOrFileExistAsync(bannerURL);
        }

        /// <inheritdoc />
        public override async Task<RetrieveEntityResult<Image<Rgba32>>> GetBannerAsync()
        {
            var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";
            var localBannerPath = Path.Combine(Path.GetTempPath(), "banner.png");

            await DownloadRemoteFileAsync(bannerURL, localBannerPath);
            return Image.Load(localBannerPath);
        }

        /// <inheritdoc />
        protected override async Task<DetermineConditionResult> DownloadRemoteFileAsync
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
                    return DetermineConditionResult.FromError(getRequest);
                }

                var request = getRequest.Entity;

                request.Method = WebRequestMethods.Http.Get;
                request.AddRange(contentOffset);

                using var contentStream = (await request.GetResponseAsync()).GetResponseStream();
                if (contentStream == null)
                {
                    return DetermineConditionResult.FromError
                    (
                        $"Failed to download the remote file at \"{remoteURL}\" (the content stream was null). Check " +
                        $"your internet connection."
                    );
                }

                using var fileStream = contentOffset > 0
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
                    var bytesRead = contentStream.Read(buffer, 0, buffer.Length);

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
                return DetermineConditionResult.FromError
                (
                    $"Failed to download the remote file at \"{remoteURL}\".",
                    wex
                );
            }
            catch (IOException ioex)
            {
                return DetermineConditionResult.FromError
                (
                    $"Failed to download the remote file at \"{remoteURL}\".",
                    ioex
                );
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <inheritdoc />
        protected override async Task<RetrieveEntityResult<string>> ReadRemoteFileAsync
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
                    return RetrieveEntityResult<string>.FromError(getRequest);
                }

                var request = getRequest.Entity;

                request.Method = WebRequestMethods.Http.Get;

                var data = string.Empty;
                using var remoteStream = (await request.GetResponseAsync()).GetResponseStream();

                // Drop out early if the stream wasn't present
                if (remoteStream == null)
                {
                    Log.Error
                    (
                        $"Failed to read the contents of remote file \"{remoteURL}\": " +
                        "Remote stream was null. This could be due to a network interruption " +
                        "or issues with the remote file."
                    );

                    return string.Empty;
                }

                var bufferSize = this.Configuration.RemoteFileDownloadBufferSize;
                var buffer = new byte[bufferSize];

                while (true)
                {
                    var bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

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
                return RetrieveEntityResult<string>.FromError
                (
                    $"Failed to read the contents of remote file \"{remoteURL}\".",
                    wex
                );
            }
            catch (NullReferenceException nex)
            {
                Log.Error(" (NullReferenceException): " + nex.Message);

                return RetrieveEntityResult<string>.FromError
                (
                    "Failed to establish a network connection, or the connection was interrupted during the download.",
                    nex
                );
            }
        }

        /// <summary>
        /// Creates a HTTP web request.
        /// </summary>
        /// <returns>The HTTP web request.</returns>
        /// <param name="remotePath">url of the desired remote object.</param>
        /// <param name="username">The username used for authentication.</param>
        /// <param name="password">The password used for authentication.</param>
        private CreateEntityResult<HttpWebRequest> CreateHttpWebRequest
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
                Log.Warn("Unable to create a WebRequest for the specified file (WebException): " + wex.Message);
                throw new InvalidOperationException();
            }
            catch (ArgumentException aex)
            {
                Log.Warn("Unable to create a WebRequest for the specified file (ArgumentException): " + aex.Message);
                throw new InvalidOperationException();
            }
            catch (UriFormatException uex)
            {
                Log.Warn("Unable to create a WebRequest for the specified file (UriFormatException): " + uex.Message + "\n" +
                    "You may need to add \"http://\" before the url in the config.");
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Checks if the provided path points to a valid directory or file.
        /// </summary>
        /// <returns><c>true</c>, if the directory or file exists, <c>false</c> otherwise.</returns>
        /// <param name="url">The remote url of the directory or file.</param>
        private async Task<RetrieveEntityResult<bool>> DoesRemoteDirectoryOrFileExistAsync(string url)
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
                return RetrieveEntityResult<bool>.FromError(getRequest);
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
                using var response = (HttpWebResponse)wex.Response;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                return RetrieveEntityResult<bool>.FromError(wex);
            }

            return true;
        }
    }
}
