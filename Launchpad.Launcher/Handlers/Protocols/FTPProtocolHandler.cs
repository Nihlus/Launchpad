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
using Launchpad.Launcher.Utility.Events;
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
		public long FTPbytesDownloaded = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.FTPHandler"/> class. 
		/// This also calls the base PatchProtocolHandler constructor, setting up the common functionality.
		/// </summary>
		public FTPProtocolHandler()
			: base()
		{

		}

		public override bool CanPatch()
		{
			return false;
		}

		public override bool IsLauncherOutdated()
		{
			return false;
		}

		public override bool IsGameOutdated()
		{
			return false;
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
		/// <param name="ftpSourceFilePath">FTP file path.</param>
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


			int bytesRead = 0;

			// The buffer size is 256kb. More or less than this reduces download speeds.
			byte[] buffer = new byte[262144];

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;

			try
			{
				request = CreateFtpWebRequest(remoteURL, username, password, false);
				sizerequest = CreateFtpWebRequest(remoteURL, username, password, false);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				//TODO: Maybe use the manifest filesize instead? We should be able to trust it.
				// Or maybe just bail out if the two differ
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				string data = "";
            
				reader = request.GetResponse().GetResponseStream();

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

				//clean the output from \n and \0, then return
				return Utilities.Clean(data);
			}
			catch (WebException wex)
			{
				Console.Write("WebException in ReadFTPFileException: ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
				return wex.Message;
			}
			finally
			{
				//clean up all open requests
				//then, the responses that are reading from the requests.
				if (reader != null)
				{
					reader.Close();
				}

				//and finally, the requests themselves.
				if (request != null)
				{
					request.Abort();
				}

				if (sizerequest != null)
				{
					sizerequest.Abort();
				}
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
			FtpWebRequest request = null;
			FtpWebResponse response = null;
			string remoteURL = Utilities.Clean(rawRemoteURL) + "/";
			List<string> relativePaths = new List<string>();

			if (DoesRemoteDirectoryExist(remoteURL))
			{
				try
				{
					request = CreateFtpWebRequest(
						remoteURL, 
						Config.GetFTPUsername(), 
						Config.GetFTPPassword(), 
						false);

					request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

					response = (FtpWebResponse)request.GetResponse();
					Stream responseStream = response.GetResponseStream();
					StreamReader sr = new StreamReader(responseStream);

					string rawListing = sr.ReadToEnd();
					string[] listing = rawListing.Replace("\r", String.Empty).Split('\n');
					List<string> directories = new List<string>();

					foreach (string fileOrDir in listing)
					{
						//we only need to save the directories if we're searching recursively
						if (bRecursively && fileOrDir.StartsWith("d"))
						{
							//it's a directory, add it to directories
							string[] parts = fileOrDir.Split(' ');                        
							string relativeDirectoryPath = parts[parts.Length - 1];

							directories.Add(relativeDirectoryPath);
						}
						else
						{
							//there's a file, add it to our relative paths
							string[] filePath = fileOrDir.Split(' ');
							if (!String.IsNullOrEmpty(filePath[filePath.Length - 1]))
							{
								string relativePath = "/" + filePath[filePath.Length - 1];
								relativePaths.Add(relativePath);
							}                        
						}
					}

					//if we should search recursively, keep looking in subdirectories.
					if (bRecursively)
					{
						if (directories.Count != 0)
						{
							for (int i = 0; i < directories.Count; ++i)
							{
								string directory = directories[i];
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
				catch (WebException wex)
				{
					Console.WriteLine("WebException in GetFileURLs(): " + wex.Message);
					return null;
				}
				finally
				{
					if (request != null)
					{
						request.Abort();
					}    

					if (response != null)
					{
						response.Close();
					}
				}
			}	

			return relativePaths;
		}

		/// <summary>
		/// Downloads an FTP file.
		/// </summary>
		/// <returns>The FTP file's location on disk, or the exception message.</returns>
		/// <param name="ftpSourceFilePath">Ftp source file path.</param>
		/// <param name="localDestination">Local destination.</param>
		/// <param name="bUseAnonymous">If set to <c>true</c> b use anonymous.</param>
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

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;
			FtpWebResponse sizereader = null;

			FileStream fileStream = null;		

			try
			{
				request = CreateFtpWebRequest(remoteURL, username, password, false);
				sizerequest = CreateFtpWebRequest(remoteURL, username, password, false);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				//TODO: Maybe use the manifest filesize instead? We should be able to trust it.
				// Or maybe just bail out if the two differ
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;
				request.ContentOffset = contentOffset;

				long fileSize = 0;
				            
				reader = request.GetResponse().GetResponseStream();
				sizereader = (FtpWebResponse)sizerequest.GetResponse();
		
				//reset byte counter
				FTPbytesDownloaded = 0;

				fileSize = sizereader.ContentLength;

				//set file info for progress reporting
				FileDownloadProgressArgs.FileName = Path.GetFileNameWithoutExtension(remoteURL);
				FileDownloadProgressArgs.TotalBytes = (int)fileSize;

				if (contentOffset > 0)
				{
					fileStream = new FileStream(localPath, FileMode.Append);
				}
				else
				{
					fileStream = new FileStream(localPath, FileMode.Create);
				}

				// Sets the content offset for the file stream, allowing it to begin writing where it last stopped.
				fileStream.Position = contentOffset;
				FTPbytesDownloaded = contentOffset;

				fileSize = sizereader.ContentLength;

				//set file info for progress reporting
				FileDownloadProgressArgs.FileName = Path.GetFileNameWithoutExtension(remoteURL);
				FileDownloadProgressArgs.TotalBytes = (int)fileSize;

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

					//set file progress info
					FileDownloadProgressArgs.DownloadedBytes = FTPbytesDownloaded;

					OnFileDownloadProgressChanged();
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
			finally
			{
				//clean up all open requests
				//first, close and dispose of the file stream
				if (fileStream != null)
				{
					fileStream.Close();
				}

				//then, the responses that are reading from the requests.
				if (reader != null)
				{
					reader.Close();
				}
				if (sizereader != null)
				{
					sizereader.Close();
				}

				//and finally, the requests themselves.
				if (request != null)
				{
					request.Abort();
				}

				if (sizerequest != null)
				{
					sizerequest.Abort();
				}
			}
		}

		/// <summary>
		/// Creates an ftp web request.
		/// </summary>
		/// <returns>The ftp web request.</returns>
		/// <param name="ftpDirectoryPath">Ftp directory path.</param>
		/// <param name="keepAlive">If set to <c>true</c> keep alive.</param>
		public static FtpWebRequest CreateFtpWebRequest(string ftpDirectoryPath, string username, string password, bool keepAlive)
		{
			try
			{
				FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(ftpDirectoryPath));

				//Set proxy to null. Under current configuration if this option is not set then the proxy 
				//that is used will get an html response from the web content gateway (firewall monitoring system)
				request.Proxy = null;

				request.UsePassive = true;
				request.UseBinary = true;
				request.KeepAlive = keepAlive;

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
		public Version GetRemoteGameVersion(bool bUseSystemTarget)
		{
			string remoteVersionPath = "";
			if (bUseSystemTarget)
			{
				remoteVersionPath = String.Format("{0}/game/{1}/bin/GameVersion.txt", 
					Config.GetBaseFTPUrl(), 
					Config.GetSystemTarget());

			}
			else
			{
				remoteVersionPath = String.Format("{0}/game/bin/GameVersion.txt", 
					Config.GetBaseFTPUrl());

			}
			string remoteVersion = ReadFTPFile(remoteVersionPath);

			if (!string.IsNullOrEmpty(remoteVersion))
			{
				return Version.Parse(remoteVersion);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the remote manifest checksum.
		/// </summary>
		/// <returns>The remote manifest checksum.</returns>
		public string GetRemoteManifestChecksum()
		{
			string checksum = String.Empty;

			checksum = ReadFTPFile(Config.GetManifestChecksumURL());
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
				                        Config.GetFTPPassword(),
				                        false);
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
				                        Config.GetFTPPassword(),
				                        false);
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
					response.Close();
				}
			}

			return true;
		}
	}
}
