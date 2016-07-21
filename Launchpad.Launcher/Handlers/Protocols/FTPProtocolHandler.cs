//
//  FTPHandler.cs
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
using System.IO;
using System.Net;
using System.Text;
using Launchpad.Launcher.Utility;
using Launchpad.Launcher.Utility.Enums;
using System.Drawing;
using log4net;

namespace Launchpad.Launcher.Handlers.Protocols
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
		private static readonly ILog Log = LogManager.GetLogger(typeof(FTPProtocolHandler));

		public override bool CanPatch()
		{
			Log.Info("Pinging remote patching server to determine if we can connect to it.");

			bool bCanConnectToServer;

			string url = Config.GetBaseFTPUrl();
			string username = Config.GetRemoteUsername();
			string password = Config.GetRemotePassword();

			try
			{
				FtpWebRequest plainRequest = CreateFtpWebRequest(url, username, password);

				if (plainRequest == null)
				{
					return false;
				}

				plainRequest.Method = WebRequestMethods.Ftp.ListDirectory;
				plainRequest.Timeout = 4000;

				try
				{
					using (WebResponse response = plainRequest.GetResponse())
					{
						bCanConnectToServer = true;
					}
				}
				catch (WebException wex)
				{
					Log.Warn("Unable to connect to remote patch server (WebException): " + wex.Message);
					bCanConnectToServer = false;
				}
			}
			catch (WebException wex)
			{
				Log.Warn("Unable to connect due a malformed url in the configuration (WebException): " + wex.Message);
				bCanConnectToServer = false;
			}

			return bCanConnectToServer;
		}

		public override bool IsPlatformAvailable(ESystemTarget platform)
		{
			string remote = $"{Config.GetBaseFTPUrl()}/game/{platform}/.provides";

			return DoesRemoteFileExist(remote);
		}

		public override bool CanProvideChangelog()
		{
			return true;
		}

		public override string GetChangelogSource()
		{
			string changelogURL = $"{Config.GetBaseFTPUrl()}/launcher/changelog.html";

			// Return simple raw HTML
			return ReadRemoteFile(changelogURL);
		}

		public override bool CanProvideBanner()
		{
			string bannerURL = $"{Config.GetBaseFTPUrl()}/launcher/banner.png";

			return DoesRemoteFileExist(bannerURL);
		}

		public override Bitmap GetBanner()
		{
			string bannerURL = $"{Config.GetBaseFTPUrl()}/launcher/banner.png";

			string localBannerPath = $"{Path.GetTempPath()}/banner.png";

			DownloadRemoteFile(bannerURL, localBannerPath);
			return new Bitmap(localBannerPath);
		}

		/// <summary>
		/// Reads a text file from a remote FTP server.
		/// </summary>
		/// <returns>The FTP file contents.</returns>
		/// <param name="url">FTP file path.</param>
		/// <param name="useAnonymousLogin">Force anonymous credentials for the connection.</param>
		protected override string ReadRemoteFile(string url, bool useAnonymousLogin = false)
		{
			// Clean the input url first
			string remoteURL = url.Replace(Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (useAnonymousLogin)
			{
				username = "anonymous";
				password = "anonymous";

			}
			else
			{
				username = Config.GetRemoteUsername();
				password = Config.GetRemotePassword();
			}

			try
			{
				FtpWebRequest request = CreateFtpWebRequest(remoteURL, username, password);
				FtpWebRequest sizerequest = CreateFtpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				string data = "";
				using (Stream remoteStream = request.GetResponse().GetResponseStream())
				{
					if (remoteStream == null)
					{
						return string.Empty;
					}

					long fileSize;
					using (FtpWebResponse sizeResponse = (FtpWebResponse)sizerequest.GetResponse())
					{
						fileSize = sizeResponse.ContentLength;
					}

					if (fileSize < 4096)
					{
						byte[] smallBuffer = new byte[fileSize];
						remoteStream.Read(smallBuffer, 0, smallBuffer.Length);

						data = Encoding.UTF8.GetString(smallBuffer, 0, smallBuffer.Length);
					}
					else
					{
						byte[] buffer = new byte[4096];

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

				// Clean the output from \n and \0, then return
				return Utilities.SanitizeString(data);
			}
			catch (WebException wex)
			{
				Log.Error($"Failed to read the contents of remote file \"{remoteURL}\" (WebException): {wex.Message}");
				return string.Empty;
			}
		}

		/// <summary>
		/// Downloads an FTP file.
		/// </summary>
		/// <returns>The FTP file's location on disk, or the exception message.</returns>
		/// <param name="url">Ftp source file path.</param>
		/// <param name="localPath">Local destination.</param>
		/// <param name="totalSize">The total expected size of the file.</param>
		/// <param name="contentOffset">Offset into the remote file where downloading should start</param>
		/// <param name="useAnonymousLogin">If set to <c>true</c> b use anonymous.</param>
		protected override void DownloadRemoteFile(string url, string localPath, long totalSize = 0, long contentOffset = 0, bool useAnonymousLogin = false)
		{
			// Make sure we're not passing in any backslashes in the url
			string remoteURL = url.Replace(Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (useAnonymousLogin)
			{
				username = "anonymous";
				password = "anonymous";
			}
			else
			{
				username = Config.GetRemoteUsername();
				password = Config.GetRemotePassword();
			}

			try
			{
				FtpWebRequest request = CreateFtpWebRequest(remoteURL, username, password);
				FtpWebRequest sizerequest = CreateFtpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				request.ContentOffset = contentOffset;

				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				using (Stream contentStream = request.GetResponse().GetResponseStream())
				{
					if (contentStream == null)
					{
						Log.Error($"Failed to download the remote file at \"{remoteURL}\" (NullReferenceException from the content stream). " +
						          "Check your internet connection.");

						return;
					}

					long fileSize = contentOffset;
					using (FtpWebResponse sizereader = (FtpWebResponse) sizerequest.GetResponse())
					{
						fileSize += sizereader.ContentLength;
					}

					using (FileStream fileStream = contentOffset > 0
						                               ? new FileStream(localPath, FileMode.Append)
						                               : new FileStream(localPath, FileMode.Create))
					{
						fileStream.Position = contentOffset;
						long totalBytesDownloaded = contentOffset;

						if (fileSize < 4096)
						{
							byte[] smallBuffer = new byte[fileSize];
							contentStream.Read(smallBuffer, 0, smallBuffer.Length);

							fileStream.Write(smallBuffer, 0, smallBuffer.Length);

							totalBytesDownloaded += smallBuffer.Length;

							// Report download progress
							ModuleDownloadProgressArgs.ProgressBarMessage = GetDownloadProgressBarMessage(Path.GetFileName(remoteURL),
								totalBytesDownloaded, fileSize);
							ModuleDownloadProgressArgs.ProgressFraction = (double) totalBytesDownloaded / (double) fileSize;
							OnModuleDownloadProgressChanged();
						}
						else
						{
							byte[] buffer = new byte[4096];

							while (true)
							{
								int bytesRead = contentStream.Read(buffer, 0, buffer.Length);

								if (bytesRead == 0)
								{
									break;
								}

								fileStream.Write(buffer, 0, bytesRead);

								totalBytesDownloaded += bytesRead;

								// Report download progress
								ModuleDownloadProgressArgs.ProgressBarMessage = GetDownloadProgressBarMessage(Path.GetFileName(remoteURL),
									totalBytesDownloaded, fileSize);
								ModuleDownloadProgressArgs.ProgressFraction = (double) totalBytesDownloaded / (double) fileSize;
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
		/// <param name="ftpDirectoryPath">Ftp directory path.</param>
		/// <param name="username">Remote FTP username.</param>
		/// <param name="password">Remote FTP password</param>
		public static FtpWebRequest CreateFtpWebRequest(string ftpDirectoryPath, string username, string password)
		{
			try
			{
				FtpWebRequest request = (FtpWebRequest) WebRequest.Create(new Uri(ftpDirectoryPath));

				//Set proxy to null. Under current configuration if this option is not set then the proxy
				//that is used will get an html response from the web content gateway (firewall monitoring system)
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
				Log.Warn("Unable to create a WebRequest for the specified file (UriFormatException): " + uex.Message + "\n" +
                         "You may need to add \"ftp://\" before the url in the config.");
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
			FtpWebRequest request = CreateFtpWebRequest(remotePath,
				                        Config.GetRemoteUsername(),
				                        Config.GetRemotePassword());
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
