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
using System.Collections.Generic;
using Launchpad.Launcher.Utility;

namespace Launchpad.Launcher.Handlers.Protocols
{
	/// <summary>
	/// FTP handler. Handles downloading and reading files on a remote FTP server.
	/// There are also functions for retrieving remote version information of the game and the launcher.
	/// 
	/// This protocol uses a manifest.
	/// </summary>
	internal sealed class FTPProtocolHandler : PatchProtocolHandler
	{
		/// <summary>
		/// How many bytes of the target file that have been downloaded.
		/// </summary>
		public long FTPbytesDownloaded;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad.Launcher.Handlers.Protocols.FTPProtocolHandler"/> class. 
		/// This also calls the base PatchProtocolHandler constructor, setting up the common functionality.
		/// </summary>
		public FTPProtocolHandler()
			: base()
		{

		}

		public override bool CanPatch()
		{
			bool bCanConnectToFTP;

			string FTPURL = Config.GetBaseFTPUrl();
			string FTPUserName = Config.GetFTPUsername();
			string FTPPassword = Config.GetFTPPassword();

			try
			{
				FtpWebRequest plainRequest = FTPProtocolHandler.CreateFtpWebRequest(FTPURL, FTPUserName, FTPPassword);

				plainRequest.Credentials = new NetworkCredential(FTPUserName, FTPPassword);
				plainRequest.Method = WebRequestMethods.Ftp.ListDirectory;
				plainRequest.Timeout = 4000;

				try
				{
					using (WebResponse response = plainRequest.GetResponse())
					{
						bCanConnectToFTP = true;
					}					
				}
				catch (WebException wex)
				{
					Console.WriteLine("WebException in CanConnectToFTP(): " + wex.Message);
					bCanConnectToFTP = false;
				}
			}
			catch (WebException wex)
			{
				// Case where FTP URL in config is not valid
				Console.WriteLine("WebException CanConnectToFTP() (Invalid URL): " + wex.Message);

				bCanConnectToFTP = false;
			}

			if (!bCanConnectToFTP)
			{
				Console.WriteLine("Failed to connect to FTP server at: {0}", Config.GetBaseFTPUrl());
			}

			return bCanConnectToFTP;
		}

		public override bool IsLauncherOutdated()
		{
			try
			{
				Version local = Config.GetLocalLauncherVersion();
				Version remote = GetRemoteLauncherVersion();	

				return local < remote;
			}
			catch (WebException wex)
			{
				Console.WriteLine("WebException in IsLauncherOutdated(): " + wex.Message);
				return false;	
			}
		}

		public override bool IsGameOutdated()
		{
			try
			{
				Version local = Config.GetLocalGameVersion();
				Version remote = GetRemoteGameVersion();

				return local < remote;
			}
			catch (WebException wex)
			{
				Console.WriteLine("WebException in IsGameOutdated(): " + wex.Message);
				return true;
			}
		}

		public override void InstallLauncher()
		{

		}

		public override void InstallGame()
		{

		}

		protected override void DownloadLauncher()
		{

		}

		protected override void DownloadGame()
		{

		}

		public override void VerifyLauncher()
		{

		}

		public override void VerifyGame()
		{

		}

		/// <summary>
		/// Reads a text file from a remote FTP server.
		/// </summary>
		/// <returns>The FTP file contents.</returns>
		/// <param name="rawRemoteURL">FTP file path.</param>
		/// <param name="useAnonymousLogin">Force anonymous credentials for the connection.</param>
		public string ReadFTPFile(string rawRemoteURL, bool useAnonymousLogin = false)
		{
			// Clean the input URL first
			string remoteURL = rawRemoteURL.Replace(Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (useAnonymousLogin)
			{
				username = "anonymous";
				password = "anonymous";

			}
			else
			{
				username = Config.GetFTPUsername();
				password = Config.GetFTPPassword();
			}

			// The buffer size is 256kb. More or less than this reduces download speeds.
			byte[] buffer = new byte[262144];

			try
			{
				FtpWebRequest request = CreateFtpWebRequest(remoteURL, username, password);
				FtpWebRequest sizerequest = CreateFtpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				//TODO: Maybe use the manifest filesize instead? We should be able to trust it.
				// Or maybe just bail out if the two differ
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				string data = "";
            
				using (Stream reader = request.GetResponse().GetResponseStream())
				{
					int bytesRead;
					while (true)
					{
						bytesRead = reader.Read(buffer, 0, buffer.Length);

						if (bytesRead == 0)
						{
							break;
						}

						FTPbytesDownloaded = FTPbytesDownloaded + bytesRead;
						data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
					}
				}

				//clean the output from \n and \0, then return
				return Utilities.Clean(data);
			}
			catch (WebException wex)
			{
				Console.Write("WebException in ReadFTPFileException: ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
				return wex.Message;
			}
		}

		/// <summary>
		/// Gets the relative paths for all files in the specified FTP directory.
		/// </summary>
		/// <param name="rawRemoteURL">The URL to search.</param>
		/// <param name="bRecursively">Should the search should include subdirectories?</param>
		/// <returns>A list of relative paths for the files in the specified directory.</returns>
		public List<string> GetFilePaths(string rawRemoteURL, bool bRecursively)
		{			
			string remoteURL = Utilities.Clean(rawRemoteURL) + "/";
			List<string> relativePaths = new List<string>();

			if (DoesRemoteDirectoryExist(remoteURL))
			{
				try
				{
					FtpWebRequest request = CreateFtpWebRequest(
						                        remoteURL, 
						                        Config.GetFTPUsername(), 
						                        Config.GetFTPPassword());

					request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

					using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
					{
						using (StreamReader sr = new StreamReader(response.GetResponseStream()))
						{
							string rawListing = sr.ReadToEnd();
							string[] listing = rawListing.Replace("\r", String.Empty).Split('\n');

							List<string> directories = new List<string>();
							foreach (string fileOrDir in listing)
							{
								// We only need to save the directories if we're searching recursively
								if (bRecursively && fileOrDir.StartsWith("d"))
								{
									// It's a directory, add it to directories
									string[] parts = fileOrDir.Split(' ');                        
									string relativeDirectoryPath = parts[parts.Length - 1];

									directories.Add(relativeDirectoryPath);
								}
								else
								{
									// There's a file, add it to our relative paths
									string[] filePath = fileOrDir.Split(' ');
									if (!String.IsNullOrEmpty(filePath[filePath.Length - 1]))
									{
										string relativePath = "/" + filePath[filePath.Length - 1];
										relativePaths.Add(relativePath);
									}                        
								}
							}

							// If we should search recursively, keep looking in subdirectories.
							if (bRecursively)
							{
								if (directories.Count != 0)
								{
									foreach (string directory in directories)
									{
										string parentDirectory = remoteURL.Replace(Config.GetLauncherBinariesURL(), String.Empty);

										string recursiveURL = Config.GetLauncherBinariesURL() + parentDirectory + "/" + directory;
										List<string> files = GetFilePaths(recursiveURL, true);
										foreach (string rawPath in files)
										{
											string relativePath = "/" + directory + rawPath;
											relativePaths.Add(relativePath);
										}
									}
								}
							}
						}
					}
				}
				catch (WebException wex)
				{
					Console.WriteLine("WebException in GetFileURLs(): " + wex.Message);
					return null;
				}
			}	

			return relativePaths;
		}

		/// <summary>
		/// Downloads an FTP file.
		/// </summary>
		/// <returns>The FTP file's location on disk, or the exception message.</returns>
		/// <param name="rawRemoteURL">Ftp source file path.</param>
		/// <param name="localPath">Local destination.</param>
		/// <param name="contentOffset">Offset into the remote file where downloading should start</param>
		/// <param name="useAnonymousLogin">If set to <c>true</c> b use anonymous.</param>
		public void DownloadFTPFile(string rawRemoteURL, string localPath, long contentOffset = 0, bool useAnonymousLogin = false)
		{
			//clean the URL string
			string remoteURL = rawRemoteURL.Replace(Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (useAnonymousLogin)
			{
				username = "anonymous";
				password = "anonymous";
			}
			else
			{
				username = Config.GetFTPUsername();
				password = Config.GetFTPPassword();
			}
					
			//the buffer size is 256kb. More or less than this reduces download speeds.
			byte[] buffer = new byte[262144];

			try
			{
				FtpWebRequest request = CreateFtpWebRequest(remoteURL, username, password);
				FtpWebRequest sizerequest = CreateFtpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				//TODO: Maybe use the manifest filesize instead? We should be able to trust it.
				// Or maybe just bail out if the two differ
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;
				request.ContentOffset = contentOffset;
								            
				using (Stream reader = request.GetResponse().GetResponseStream())
				{				
					long fileSize;
					using (FtpWebResponse sizereader = (FtpWebResponse)sizerequest.GetResponse())
					{
						fileSize = sizereader.ContentLength;
					}

					// Set initial progress argument data
					FileDownloadProgressArgs.FileName = Path.GetFileNameWithoutExtension(remoteURL);
					FileDownloadProgressArgs.TotalBytes = fileSize;

					// Select an appending or creating filestream object based on whether or not we're attempting
					// to complete an existing file
					using (FileStream fileStream = contentOffset > 0 ? new FileStream(localPath, FileMode.Append) :
																		new FileStream(localPath, FileMode.Create))
					{
						// Sets the content offset for the file stream, allowing it to begin writing where it last stopped.
						fileStream.Position = contentOffset;
						FTPbytesDownloaded = contentOffset;

						//TODO: Fold this into a for loop?
						int bytesRead = 0;
						while (true)
						{
							bytesRead = reader.Read(buffer, 0, buffer.Length);

							if (bytesRead == 0)
							{
								break;
							}

							FTPbytesDownloaded = FTPbytesDownloaded + bytesRead;
							fileStream.Write(buffer, 0, bytesRead);

							// Set file progress info
							FileDownloadProgressArgs.DownloadedBytes = FTPbytesDownloaded;

							OnFileDownloadProgressChanged();
						}
					}				
				}
			}
			catch (WebException wex)
			{
				Console.Write("WebException in DownloadFTPFile: ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
			}
			catch (IOException ioex)
			{
				Console.Write("IOException in DownloadFTPFile: ");
				Console.WriteLine(ioex.Message + " (" + remoteURL + ")");
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
				FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(ftpDirectoryPath));

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
				Console.WriteLine("WebException in CreateFTPWebRequest(): " + wex.Message);

				return null;
			}
			catch (ArgumentException aex)
			{
				Console.WriteLine("ArgumentException in CreateFTPWebRequest(): " + aex.Message);

				return null;
			}
            
		}

		/// <summary>
		/// Gets the remote launcher version.
		/// </summary>
		/// <returns>The remote launcher version. 
		/// If the version could not be retrieved from the server, a version of 0.0.0 is returned.</returns>
		public Version GetRemoteLauncherVersion()
		{			
			string remoteVersionPath = Config.GetLauncherVersionURL();

			// Config.GetDoOfficialUpdates is used here since the official update server always allows anonymous logins.
			string remoteVersion = ReadFTPFile(remoteVersionPath, Config.GetDoOfficialUpdates());

			Version version;
			if (Version.TryParse(remoteVersion, out version))
			{
				return version;
			}
			else
			{
				return new Version("0.0.0");
			}
		}

		/// <summary>
		/// Gets the remote game version.
		/// </summary>
		/// <returns>The remote game version.</returns>
		public Version GetRemoteGameVersion()
		{
			string remoteVersionPath = String.Format("{0}/game/{1}/bin/GameVersion.txt", 
				                           Config.GetBaseFTPUrl(), 
				                           Config.GetSystemTarget());

			string remoteVersion = ReadFTPFile(remoteVersionPath);

			Version version;
			if (Version.TryParse(remoteVersion, out version))
			{
				return version;
			}
			else
			{
				return new Version("0.0.0");
			}
		}

		/// <summary>
		/// Gets the remote manifest checksum.
		/// </summary>
		/// <returns>The remote manifest checksum.</returns>
		public string GetRemoteManifestChecksum()
		{
			string checksum = ReadFTPFile(Config.GetManifestChecksumURL());
			checksum = Utilities.Clean(checksum);

			return checksum;
		}

		/// <summary>
		/// Checks if a given directory exists on the remote FTP server.
		/// </summary>
		/// <returns><c>true</c>, if the directory exists, <c>false</c> otherwise.</returns>
		/// <param name="remotePath">Remote path.</param>
		public bool DoesRemoteDirectoryExist(string remotePath)
		{
			FtpWebRequest request = CreateFtpWebRequest(remotePath, 
				                        Config.GetFTPUsername(),
				                        Config.GetFTPPassword());
			FtpWebResponse response = null;

			try
			{
				request.Method = WebRequestMethods.Ftp.ListDirectory;
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
				if (response != null)
				{
					response.Close();
				}
			}

			return true;
			
		}

		/// <summary>
		/// Checks if a given file exists on the remote FTP server.
		/// </summary>
		/// <returns><c>true</c>, if the file exists, <c>false</c> otherwise.</returns>
		/// <param name="remotePath">Remote path.</param>
		public bool DoesRemoteFileExist(string remotePath)
		{
			FtpWebRequest request = CreateFtpWebRequest(remotePath, 
				                        Config.GetFTPUsername(),
				                        Config.GetFTPPassword());
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
				if (response != null)
				{
					response.Dispose();
				}
			}

			return true;
		}
	}
}
