//
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

using Launchpad.Common.Enums;
using NLog;
using SixLabors.ImageSharp;

namespace Launchpad.Launcher.Handlers.Protocols.Manifest
{
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
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		/// <inheritdoc />
		public override bool CanPatch()
		{
			Log.Info("Pinging remote patching server to determine if we can connect to it.");

			var canConnect = false;

			var url = this.Configuration.RemoteAddress.AbsoluteUri;
			var username = this.Configuration.RemoteUsername;
			var password = this.Configuration.RemotePassword;

			try
			{
				var plainRequest = CreateFtpWebRequest(url, username, password);

				if (plainRequest == null)
				{
					return false;
				}

				plainRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
				plainRequest.Timeout = 4000;

				try
				{
					using (var response = (FtpWebResponse)plainRequest.GetResponse())
					{
						if (response.StatusCode == FtpStatusCode.OpeningData)
						{
							canConnect = true;
						}
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
		public override bool IsPlatformAvailable(ESystemTarget platform)
		{
			var remote = $"{this.Configuration.RemoteAddress}/game/{platform}/.provides";

			return DoesRemoteFileExist(remote);
		}

		/// <inheritdoc />
		public override string GetChangelogMarkup()
		{
			var changelogURL = $"{this.Configuration.RemoteAddress}/launcher/changelog.pango";
			return ReadRemoteFile(changelogURL);
		}

		/// <inheritdoc />
		public override bool CanProvideBanner()
		{
			var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";

			return DoesRemoteFileExist(bannerURL);
		}

		/// <inheritdoc />
		public override Image<Rgba32> GetBanner()
		{
			var bannerURL = $"{this.Configuration.RemoteAddress}/launcher/banner.png";

			var localBannerPath = $"{Path.GetTempPath()}/banner.png";

			DownloadRemoteFile(bannerURL, localBannerPath);
			var bytes = File.ReadAllBytes(localBannerPath);
			return Image.Load(bytes);
		}

		/// <inheritdoc />
		protected override string ReadRemoteFile(string url, bool useAnonymousLogin = false)
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
				var request = CreateFtpWebRequest(remoteURL, username, password);
				var sizerequest = CreateFtpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				var data = string.Empty;
				using (var remoteStream = request.GetResponse().GetResponseStream())
				{
					if (remoteStream == null)
					{
						return string.Empty;
					}

					long fileSize;
					using (var sizeResponse = (FtpWebResponse)sizerequest.GetResponse())
					{
						fileSize = sizeResponse.ContentLength;
					}

					var bufferSize = this.Configuration.RemoteFileDownloadBufferSize;
					if (fileSize < bufferSize)
					{
						var smallBuffer = new byte[fileSize];
						remoteStream.Read(smallBuffer, 0, smallBuffer.Length);

						data = Encoding.UTF8.GetString(smallBuffer, 0, smallBuffer.Length);
					}
					else
					{
						var buffer = new byte[bufferSize];

						while (true)
						{
							int bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

							if (bytesRead == 0)
							{
								break;
							}

							data += Encoding.UTF8.GetString(buffer, 0, bytesRead);
						}
					}
				}

				return data;
			}
			catch (WebException wex)
			{
				Log.Error($"Failed to read the contents of remote file \"{remoteURL}\" (WebException): {wex.Message}");
				return string.Empty;
			}
		}

		/// <inheritdoc />
		protected override void DownloadRemoteFile(string url, string localPath, long totalSize = 0, long contentOffset = 0, bool useAnonymousLogin = false)
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
				var request = CreateFtpWebRequest(remoteURL, username, password);
				var sizerequest = CreateFtpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				request.ContentOffset = contentOffset;

				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				using (var contentStream = request.GetResponse().GetResponseStream())
				{
					if (contentStream == null)
					{
						Log.Error
						(
							$"Failed to download the remote file at \"{remoteURL}\" (NullReferenceException from the content stream). " +
							"Check your internet connection."
						);

						return;
					}

					var fileSize = contentOffset;
					using (var sizereader = (FtpWebResponse)sizerequest.GetResponse())
					{
						fileSize += sizereader.ContentLength;
					}

					using
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
							contentStream.Read(smallBuffer, 0, smallBuffer.Length);

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
									fileSize
								);
								this.ModuleDownloadProgressArgs.ProgressFraction = (double)totalBytesDownloaded / fileSize;
								OnModuleDownloadProgressChanged();
							}
						}
					}
				}
			}
			catch (WebException wex)
			{
				Log.Error($"Failed to download the remote file at \"{remoteURL}\" (WebException): {wex.Message}");
			}
			catch (IOException ioex)
			{
				Log.Error($"Failed to download the remote file at \"{remoteURL}\" (IOException): {ioex.Message}");
			}
		}

		/// <summary>
		/// Creates an ftp web request.
		/// </summary>
		/// <returns>The ftp web request.</returns>
		/// <param name="remotePath">Ftp directory path.</param>
		/// <param name="username">Remote FTP username.</param>
		/// <param name="password">Remote FTP password</param>
		private FtpWebRequest CreateFtpWebRequest(string remotePath, string username, string password)
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
				Log.Warn("Unable to create a WebRequest for the specified file (WebException): " + wex.Message);
				return null;
			}
			catch (ArgumentException aex)
			{
				Log.Warn("Unable to create a WebRequest for the specified file (ArgumentException): " + aex.Message);
				return null;
			}
			catch (UriFormatException uex)
			{
				Log.Warn
				(
					"Unable to create a WebRequest for the specified file (UriFormatException): " + uex.Message + "\n" +
					"You may need to add \"ftp://\" before the url in the config."
				);
				return null;
			}
		}

		/// <summary>
		/// Checks if a given file exists on the remote FTP server.
		/// </summary>
		/// <returns><c>true</c>, if the file exists, <c>false</c> otherwise.</returns>
		/// <param name="remotePath">Remote path.</param>
		private bool DoesRemoteFileExist(string remotePath)
		{
			var request = CreateFtpWebRequest(remotePath, this.Configuration.RemoteUsername, this.Configuration.RemotePassword);
			FtpWebResponse response = null;

			try
			{
				response = (FtpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				response = (FtpWebResponse)ex.Response;
				if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
				{
					return false;
				}
			}
			finally
			{
				response?.Dispose();
			}

			return true;
		}
	}
}
