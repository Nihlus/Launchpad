﻿//
//  FTPProtocolHandler.cs
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

namespace Launchpad.Launcher.Handlers.Protocols.Manifest;

/// <summary>
/// FTP handler. Handles downloading and reading files on a remote FTP server.
/// There are also functions for retrieving remote version information of the game and the launcher.
///
/// This protocol uses a manifest.
/// </summary>
internal sealed class FTPProtocolHandler : ManifestBasedProtocolHandler
{
    /// <summary>
    /// Logger instance for this class.
    /// </summary>
    private readonly ILogger<FTPProtocolHandler> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="FTPProtocolHandler"/> class.
    /// </summary>
    /// <param name="log">The logging instance.</param>
    /// <param name="localVersionService">The local version service.</param>
    /// <param name="fileManifestHandler">The manifest handler.</param>
    /// <param name="localizationCatalog">The localization catalog.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="tagfileService">The tagfile service.</param>
    /// <param name="directoryHelpers">The directory helpers.</param>
    public FTPProtocolHandler
    (
        ILogger<FTPProtocolHandler> log,
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
        _log.LogInformation("Pinging remote patching server to determine if we can connect to it");

        var canConnect = false;

        var url = this.Configuration.RemoteAddress.AbsoluteUri;
        var username = this.Configuration.RemoteUsername;
        var password = this.Configuration.RemotePassword;

        try
        {
            var getPlainRequest = CreateFtpWebRequest(url, username, password);
            if (!getPlainRequest.IsSuccess)
            {
                return Result<bool>.FromError(getPlainRequest);
            }

            var request = getPlainRequest.Entity;

            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            try
            {
                var timeout = Task.Delay(TimeSpan.FromSeconds(4));
                var getResponse = request.GetResponseAsync();

                var completedTask = await Task.WhenAny(timeout, getResponse);
                if (completedTask == timeout)
                {
                    return false;
                }

                using var response = (FtpWebResponse)await getResponse;
                switch (response.StatusCode)
                {
                    case FtpStatusCode.OpeningData:
                    case FtpStatusCode.DataAlreadyOpen:
                    {
                        canConnect = true;
                        break;
                    }
                }
            }
            catch (WebException wex)
            {
                _log.LogWarning(wex, "Unable to connect to remote patch server");
                canConnect = false;
            }
        }
        catch (WebException wex)
        {
            _log.LogWarning(wex, "Unable to connect due a malformed url in the configuration");
            canConnect = false;
        }

        return canConnect;
    }

    /// <inheritdoc />
    public override Task<Result<bool>> IsPlatformAvailableAsync(ESystemTarget platform)
    {
        var remote = $"{this.Configuration.RemoteAddress}/game/{platform}/.provides";

        return DoesRemoteFileExistAsync(remote);
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

        return DoesRemoteFileExistAsync(bannerURL);
    }

    /// <inheritdoc />
    public override async Task<Result<Image<Rgba32>>> GetBannerAsync()
    {
        var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";

        var localBannerPath = Path.Combine(Path.GetTempPath(), "banner.png");

        await DownloadRemoteFileAsync(bannerURL, localBannerPath);
        var bytes = await File.ReadAllBytesAsync(localBannerPath);
        return Image.Load<Rgba32>(bytes);
    }

    /// <inheritdoc />
    protected override async Task<Result<string>> ReadRemoteFileAsync(string url, bool useAnonymousLogin = false)
    {
        // Clean the input url first
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
            var getRequest = CreateFtpWebRequest(remoteURL, username, password);
            var getSizeRequest = CreateFtpWebRequest(remoteURL, username, password);

            if (!getRequest.IsSuccess)
            {
                return Result<string>.FromError(getRequest);
            }

            if (!getSizeRequest.IsSuccess)
            {
                return Result<string>.FromError(getSizeRequest);
            }

            var request = getRequest.Entity;
            var sizeRequest = getSizeRequest.Entity;

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;

            var data = string.Empty;
            await using var remoteStream = (await request.GetResponseAsync()).GetResponseStream();

            long fileSize;
            using (var sizeResponse = (FtpWebResponse)await sizeRequest.GetResponseAsync())
            {
                fileSize = sizeResponse.ContentLength;
            }

            var bufferSize = this.Configuration.RemoteFileDownloadBufferSize;
            if (fileSize < bufferSize)
            {
                var smallBuffer = new byte[fileSize];

                var read = 0;
                while (read < smallBuffer.Length)
                {
                    read = await remoteStream.ReadAsync(smallBuffer, read, smallBuffer.Length);
                }

                data = Encoding.UTF8.GetString(smallBuffer, 0, smallBuffer.Length);
            }
            else
            {
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
            }

            return data;
        }
        catch (WebException wex)
        {
            _log.LogError(wex, "Failed to read the contents of remote file \"{RemoteUrl}\"", remoteURL);
            return string.Empty;
        }
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
        // Make sure we're not passing in any backslashes in the url
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
            var getRequest = CreateFtpWebRequest(remoteURL, username, password);
            var getSizeRequest = CreateFtpWebRequest(remoteURL, username, password);

            if (!getRequest.IsSuccess)
            {
                return Result.FromError(getRequest);
            }

            if (!getSizeRequest.IsSuccess)
            {
                return Result.FromError(getSizeRequest);
            }

            var request = getRequest.Entity;
            var sizeRequest = getSizeRequest.Entity;

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.ContentOffset = contentOffset;

            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;

            await using var contentStream = (await request.GetResponseAsync()).GetResponseStream();

            var fileSize = contentOffset;
            using (var sizeReader = (FtpWebResponse)sizeRequest.GetResponse())
            {
                fileSize += sizeReader.ContentLength;
            }

            await using
            (
                var fileStream = contentOffset > 0
                    ? new FileStream(localPath, FileMode.Append)
                    : new FileStream(localPath, FileMode.Create)
            )
            {
                fileStream.Position = contentOffset;
                var totalBytesDownloaded = contentOffset;

                var bufferSize = this.Configuration.RemoteFileDownloadBufferSize;
                if (fileSize < bufferSize)
                {
                    var smallBuffer = new byte[fileSize];

                    var read = 0;
                    while (read < smallBuffer.Length)
                    {
                        read = await contentStream.ReadAsync(smallBuffer, read, smallBuffer.Length);
                    }

                    fileStream.Write(smallBuffer, 0, smallBuffer.Length);

                    totalBytesDownloaded += smallBuffer.Length;

                    // Report download progress
                    this.ModuleDownloadProgressArgs.ProgressBarMessage = GetDownloadProgressBarMessage
                    (
                        Path.GetFileName(remoteURL),
                        totalBytesDownloaded,
                        fileSize
                    );

                    this.ModuleDownloadProgressArgs.ProgressFraction = (double)totalBytesDownloaded / fileSize;
                    OnModuleDownloadProgressChanged();
                }
                else
                {
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
                            fileSize
                        );
                        this.ModuleDownloadProgressArgs.ProgressFraction = (double)totalBytesDownloaded / fileSize;
                        OnModuleDownloadProgressChanged();
                    }
                }
            }
        }
        catch (WebException wex)
        {
            _log.LogError(wex, "Failed to read the contents of remote file \"{RemoteUrl}\"", remoteURL);
            return wex;
        }
        catch (IOException ioex)
        {
            _log.LogError(ioex, "Failed to read the contents of remote file \"{RemoteUrl}\"", remoteURL);
            return ioex;
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Creates an ftp web request.
    /// </summary>
    /// <returns>The ftp web request.</returns>
    /// <param name="remotePath">Ftp directory path.</param>
    /// <param name="username">Remote FTP username.</param>
    /// <param name="password">Remote FTP password.</param>
    private Result<FtpWebRequest> CreateFtpWebRequest(string remotePath, string username, string password)
    {
        if (!remotePath.StartsWith(this.Configuration.RemoteAddress.AbsoluteUri))
        {
            remotePath = $"{this.Configuration.RemoteAddress}/{remotePath}";
        }

        try
        {
            var request = (FtpWebRequest)WebRequest.Create(new Uri(remotePath));

            // Set proxy to null. Under current configuration if this option is not set then the proxy
            // that is used will get an html response from the web content gateway (firewall monitoring system)
            request.Proxy = null;

            request.UsePassive = true;
            request.UseBinary = true;

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
    /// Checks if a given file exists on the remote FTP server.
    /// </summary>
    /// <returns><c>true</c>, if the file exists, <c>false</c> otherwise.</returns>
    /// <param name="remotePath">Remote path.</param>
    private async Task<Result<bool>> DoesRemoteFileExistAsync(string remotePath)
    {
        try
        {
            var createRequest = CreateFtpWebRequest
            (
                remotePath,
                this.Configuration.RemoteUsername,
                this.Configuration.RemotePassword
            );

            if (!createRequest.IsSuccess)
            {
                return Result<bool>.FromError(createRequest);
            }

            var request = createRequest.Entity;
            using var response = (FtpWebResponse)await request.GetResponseAsync();
        }
        catch (WebException ex)
        {
            using var response = (FtpWebResponse?)ex.Response;
            if (response?.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
            {
                return false;
            }

            return Result<bool>.FromError(ex);
        }

        return true;
    }
}
