using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;

namespace Launchpad
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
		public event LaunchpadEventDelegates.FileProgressChangedEventHandler FileProgressChanged;
		/// <summary>
		/// Occurs when file download finished.
		/// </summary>
		public event LaunchpadEventDelegates.FileDownloadFinishedEventHandler FileDownloadFinished;

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
			ProgressArgs = new FileDownloadProgressChangedEventArgs ();
			DownloadFinishedArgs = new FileDownloadFinishedEventArgs ();
		}
        
		/// <summary>
		/// Reads a text file from a remote FTP server.
		/// </summary>
		/// <returns>The FTP file contents.</returns>
		/// <param name="ftpSourceFilePath">FTP file path.</param>
        public string ReadFTPFile(string ftpSourceFilePath)
        {
			string username = Config.GetFTPUsername();
			string password = Config.GetFTPPassword();

            int bytesRead = 0;
            byte[] buffer = new byte[1024];

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;
			FtpWebResponse sizereader = null;

			try
			{

	            request = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);
	            sizerequest = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);

	            request.Method = WebRequestMethods.Ftp.DownloadFile;
	            sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

	            string data = "";
	            long fileSize = 0;

            
                reader = request.GetResponse().GetResponseStream();
                sizereader = (FtpWebResponse)sizerequest.GetResponse();

                while (true)
                {
                    bytesRead = reader.Read(buffer, 0, buffer.Length);

                    fileSize = sizereader.ContentLength;

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    FTPbytesDownloaded = FTPbytesDownloaded + bytesRead;
                    data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                }

				//clean the output from \n and \0, then return
				return Utilities.Clean (data);
            }
            catch (Exception ex)
            {
                Console.Write("ReadFTPFileException: ");
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
			finally
			{
				//clean up all open requests
				//then, the responses that are reading from the requests.
				if (reader != null)
				{
					reader.Close ();
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
        public string DownloadFTPFile(string ftpSourceFilePath, string localDestination, bool bUseAnonymous)
        {
			Console.WriteLine (ftpSourceFilePath);
			string username;
			string password;
			if (!bUseAnonymous)
			{
				username = Config.GetFTPUsername ();
				password = Config.GetFTPPassword ();
			}
			else
			{
				username = "anonymous";
				password = "anonymous";
			}


            int bytesRead = 0;
            byte[] buffer = new byte[2048];

			FtpWebRequest request = null;
			FtpWebRequest sizerequest = null;

			Stream reader = null;
			FtpWebResponse sizereader = null;

			FileStream fileStream = null;

			//either a path to the file or an error message
			string returnValue = "";

			try
			{
	            request = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);
	            sizerequest = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);

	            request.Method = WebRequestMethods.Ftp.DownloadFile;
	            sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

	            long fileSize = 0;

            
				reader = request.GetResponse().GetResponseStream();
                sizereader = (FtpWebResponse)sizerequest.GetResponse();
					
				fileStream = new FileStream(localDestination, FileMode.Create);

				//reset byte counter
				FTPbytesDownloaded = 0;

				fileSize = sizereader.ContentLength;

				//set file info for progress reporting
				ProgressArgs.Filename = Path.GetFileNameWithoutExtension(ftpSourceFilePath);
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

				returnValue = localDestination;
				return returnValue;             				                             
            }
            catch (Exception ex)
            {
                Console.Write("DownloadFTPFileException: ");
                Console.WriteLine(ex.Message);
				returnValue = ex.Message;

				if (ex.Message == "Server returned an error: 421 There are too many connections from your internet address.")
				{
					Console.WriteLine ("Breakpoint!");
				}

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
					reader.Close ();
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
        public FtpWebRequest CreateFtpWebRequest(string ftpDirectoryPath, string username, string password, bool keepAlive = false)
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
            catch (Exception ex)
            {
                Console.Write("CreateFTPWebRequestException: ");
                Console.WriteLine(ex.Message);

                return null;
            }
            
        }

		/// <summary>
		/// Gets the remote launcher version.
		/// </summary>
		/// <returns>The remote launcher version.</returns>
		public Version GetRemoteLauncherVersion()
		{
			string remoteVersionPath = String.Format ("{0}/launcher/LauncherVersion.txt", Config.GetFTPUrl());
			string remoteVersion = ReadFTPFile (remoteVersionPath);

			return Version.Parse (remoteVersion);
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
				remoteVersionPath = String.Format ("{0}/game/{1}/GameVersion.txt", 
				                                   Config.GetFTPUrl(), 
				                                   Config.GetSystemTarget());

			}
			else
			{
				remoteVersionPath = String.Format ("{0}/game/GameVersion.txt", 
				                                   Config.GetFTPUrl());

			}
			string remoteVersion = ReadFTPFile (remoteVersionPath);

			return Version.Parse(remoteVersion);
		}

		public string GetRemoteManifestChecksum()
		{
			string checksum = "";

			try
			{
				checksum = ReadFTPFile (Config.GetManifestChecksumURL ());
				checksum = Utilities.Clean(checksum);
			}
			catch (Exception ex)
			{
				Console.WriteLine (ex.Message);
			}

			return checksum;
		}

		public bool DoesItemExist(string item)
		{
			FtpWebRequest request = CreateFtpWebRequest (item, 
			                                            Config.GetFTPUsername (),
			                                            Config.GetFTPPassword (),
			                                            false);
			FtpWebResponse response = null;
			try
			{
				response = (FtpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				response = (FtpWebResponse)ex.Response;
				if (response.StatusCode ==
				    FtpStatusCode.ActionNotTakenFileUnavailable)
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
				FileProgressChanged (this, ProgressArgs);
			}
		}

		private void OnDownloadFinished()
		{
			if (FileDownloadFinished != null)
			{
				FileDownloadFinished (this, DownloadFinishedArgs);
			}
		}


    }
}
