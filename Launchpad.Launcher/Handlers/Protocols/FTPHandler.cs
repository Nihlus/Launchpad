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
	/// </summary>
	internal sealed class FTPHandler
	{
		/// <summary>
		/// How many bytes of the target file that have been downloaded.
		/// </summary>
		public int FTPbytesDownloaded = 0;

		/// <summary>
		/// The config handler reference.
		/// </summary>
		ConfigHandler Config = ConfigHandler._instance;

		/// <summary>
		/// Occurs when file progress changed.
		/// </summary>
		public event FileProgressChangedEventHandler FileProgressChanged;
		/// <summary>
		/// Occurs when file download finished.
		/// </summary>
		public event FileDownloadFinishedEventHandler FileDownloadFinished;

		/// <summary>
		/// The progress arguments object. Is updated during file download operations.
		/// </summary>
		private FileDownloadProgressChangedEventArgs ProgressArgs;

		/// <summary>
		/// The download finished arguments object. Is updated once a file download finishes.
		/// </summary>
		private FileDownloadFinishedEventArgs DownloadFinishedArgs;

		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.FTPHandler"/> class.
		/// </summary>
		public FTPHandler()
		{
			ProgressArgs = new FileDownloadProgressChangedEventArgs();
			DownloadFinishedArgs = new FileDownloadFinishedEventArgs();
		}

		/// <summary>
		/// Reads a text file from a remote FTP server.
		/// </summary>
		/// <returns>The FTP file contents.</returns>
		/// <param name="ftpSourceFilePath">FTP file path.</param>
		public string ReadFTPFile(string rawRemoteURL)
		{
			//clean the input URL first
			string remoteURL = rawRemoteURL.Replace(Path.DirectorySeparatorChar, '/');

			string username = Config.GetFTPUsername();
			string password = Config.GetFTPPassword();

			int bytesRead = 0;

			//the buffer size is 256kb. More or less than this reduces download speeds.
			byte[] buffer = new byte[262144];

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;

			try
			{

				request = CreateFtpWebRequest(remoteURL, username, password, false);
				sizerequest = CreateFtpWebRequest(remoteURL, username, password, false);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
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

			if (DoesDirectoryExist(remoteURL))
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
		public string DownloadFTPFile(string rawRemoteURL, string localPath, bool bUseAnonymous)
		{
			//clean the URL string
			string remoteURL = rawRemoteURL.Replace(Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (!bUseAnonymous)
			{
				username = Config.GetFTPUsername();
				password = Config.GetFTPPassword();
			}
			else
			{
				username = "anonymous";
				password = "anonymous";
			}


			int bytesRead = 0;

			//the buffer size is 256kb. More or less than this reduces download speeds.
			byte[] buffer = new byte[262144];

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;
			FtpWebResponse sizereader = null;

			FileStream fileStream = null;

			//either a path to the file or an error message
			string returnValue = "";

			try
			{
				request = CreateFtpWebRequest(remoteURL, username, password, false);
				sizerequest = CreateFtpWebRequest(remoteURL, username, password, false);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				long fileSize = 0;

            
				reader = request.GetResponse().GetResponseStream();
				sizereader = (FtpWebResponse)sizerequest.GetResponse();
					
				fileStream = new FileStream(localPath, FileMode.Create);

				//reset byte counter
				FTPbytesDownloaded = 0;

				fileSize = sizereader.ContentLength;

				//set file info for progress reporting
				ProgressArgs.FileName = Path.GetFileNameWithoutExtension(remoteURL);
				ProgressArgs.TotalBytes = (int)fileSize;

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
					ProgressArgs.DownloadedBytes = FTPbytesDownloaded;

					OnProgressChanged();
				}

				OnProgressChanged();
				OnDownloadFinished();

				returnValue = localPath;
				return returnValue;             				                             
			}
			catch (WebException wex)
			{
				Console.Write("WebException in DownloadFTPFile: ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
				returnValue = wex.Message;

				return returnValue;
			}
			catch (IOException ioex)
			{
				Console.Write("IOException in DownloadFTPFile: ");
				Console.WriteLine(ioex.Message + " (" + remoteURL + ")");
				returnValue = ioex.Message;

				return returnValue;
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
		/// Downloads an FTP file.
		/// </summary>
		/// <returns>The FTP file's location on disk, or the exception message.</returns>
		/// <param name="ftpSourceFilePath">Ftp source file path.</param>
		/// <param name="localDestination">Local destination.</param>
		/// <param name="bUseAnonymous">If set to <c>true</c> b use anonymous.</param>
		/// <param name="contentOffset">The content offset where the download should resume.</param>
		public string DownloadFTPFile(string rawRemoteURL, string localPath, long contentOffset, bool bUseAnonymous)
		{
			//clean the URL string first
			string remoteURL = rawRemoteURL.Replace(Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (!bUseAnonymous)
			{
				username = Config.GetFTPUsername();
				password = Config.GetFTPPassword();
			}
			else
			{
				username = "anonymous";
				password = "anonymous";
			}


			int bytesRead = 0;

			//the buffer size is 256kb. More or less than this reduces download speeds.
			byte[] buffer = new byte[262144];

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;
			FtpWebResponse sizereader = null;

			FileStream fileStream = null;

			//either a path to the file or an error message
			string returnValue = "";

			try
			{
				request = CreateFtpWebRequest(remoteURL, username, password, true);
				sizerequest = CreateFtpWebRequest(remoteURL, username, password, true);

				request.Method = WebRequestMethods.Ftp.DownloadFile;
				sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

				request.ContentOffset = contentOffset;

				long fileSize = 0;


				reader = request.GetResponse().GetResponseStream();
				sizereader = (FtpWebResponse)sizerequest.GetResponse();

				fileStream = new FileStream(localPath, FileMode.Append);

				//reset byte counter
				FTPbytesDownloaded = 0;

				fileSize = sizereader.ContentLength;

				//set file info for progress reporting
				ProgressArgs.FileName = Path.GetFileNameWithoutExtension(remoteURL);
				ProgressArgs.TotalBytes = (int)fileSize;

				//sets the content offset for the file stream, allowing it to begin writing where it last stopped
				fileStream.Position = contentOffset;
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
					ProgressArgs.DownloadedBytes = (int)contentOffset + FTPbytesDownloaded;

					OnProgressChanged();
				}

				OnProgressChanged();
				OnDownloadFinished();

				returnValue = localPath;
				return returnValue;             				                             
			}
			catch (WebException wex)
			{
				Console.Write("WebException in DownloadFTPFile (appending): ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
				returnValue = wex.Message;

				return returnValue;
			}
			catch (IOException ioex)
			{
				Console.Write("IOException in DownloadFTPFile (appending): ");
				Console.WriteLine(ioex.Message + " (" + remoteURL + ")");
				returnValue = ioex.Message;

				return returnValue;
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
		/// <returns>The remote launcher version.</returns>
		public Version GetRemoteLauncherVersion()
		{
			string remoteVersionPath = String.Format("{0}/launcher/LauncherVersion.txt", Config.GetFTPUrl());
			string remoteVersion = ReadFTPFile(remoteVersionPath);

			return Version.Parse(remoteVersion);
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
					Config.GetFTPUrl(), 
					Config.GetSystemTarget());

			}
			else
			{
				remoteVersionPath = String.Format("{0}/game/bin/GameVersion.txt", 
					Config.GetFTPUrl());

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

		public string GetRemoteManifestChecksum()
		{
			string checksum = String.Empty;

			checksum = ReadFTPFile(Config.GetManifestChecksumURL());
			checksum = Utilities.Clean(checksum);

			return checksum;
		}

		public bool DoesDirectoryExist(string remotePath)
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

		public bool DoesFileExist(string remotePath)
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

		private void OnProgressChanged()
		{
			if (FileProgressChanged != null)
			{
				FileProgressChanged(this, ProgressArgs);
			}
		}

		private void OnDownloadFinished()
		{
			if (FileDownloadFinished != null)
			{
				FileDownloadFinished(this, DownloadFinishedArgs);
			}
		}


	}
}
