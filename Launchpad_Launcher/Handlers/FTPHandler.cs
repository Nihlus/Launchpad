using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;

namespace Launchpad_Launcher
{
    public class FTPHandler
    {
        public int FTPbytesDownloaded = 0;
		ConfigHandler Config = new ConfigHandler();
        
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

            FtpWebRequest request = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);
            FtpWebRequest sizerequest = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

            string data = "";
            long fileSize = 0;

            try
            {
                Stream reader = request.GetResponse().GetResponseStream();
                FtpWebResponse sizereader = (FtpWebResponse)sizerequest.GetResponse();

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
                return data;
            }
            catch (WebException ex)
            {
                Console.Write("ReadFTPFileWebException: ");
                Console.WriteLine(ex.Status.ToString());
                return ex.Status.ToString();
            }
            catch (Exception ex)
            {
                Console.Write("ReadFTPFileException: ");
                Console.WriteLine(ex.StackTrace);
                return ex.StackTrace;
            }
        }

		/// <summary>
		/// Downloads an FTP file.
		/// </summary>>
		/// <param name="ftpSourceFilePath">Ftp source file path.</param>
		/// <param name="localDestination">Local destination.</param>
        public void DownloadFTPFile(string ftpSourceFilePath, string localDestination, bool bUseAnonymous)
        {
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

            FtpWebRequest request = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);
            FtpWebRequest sizerequest = CreateFtpWebRequest(ftpSourceFilePath, username, password, true);

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            sizerequest.Method = WebRequestMethods.Ftp.GetFileSize;

            long fileSize = 0;

            try
            {
                Stream reader = request.GetResponse().GetResponseStream();
                FtpWebResponse sizereader = (FtpWebResponse)sizerequest.GetResponse();

                FileStream fileStream = new FileStream(localDestination, FileMode.Create);

                //reset byte counter
                FTPbytesDownloaded = 0;

                while (true)
                {
                    bytesRead = reader.Read(buffer, 0, buffer.Length);

                    fileSize = sizereader.ContentLength;

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    FTPbytesDownloaded = FTPbytesDownloaded + bytesRead;
                    fileStream.Write(buffer, 0, bytesRead);
                }

                fileStream.Close();
            }
            catch (WebException ex)
            {
                Console.Write("DownloadFTPFileWebException: ");
                Console.WriteLine(ex.Status.ToString());
            }
            catch (Exception ex)
            {
                Console.Write("DownloadFTPFileException: ");
                Console.WriteLine(ex.StackTrace);
            }
        }

		/// <summary>
		/// Creates an ftp web request.
		/// </summary>
		/// <returns>The ftp web request.</returns>
		/// <param name="ftpDirectoryPath">Ftp directory path.</param>
		/// <param name="username">Username.</param>
		/// <param name="password">Password.</param>
		/// <param name="keepAlive">If set to <c>true</c> keep alive.</param>
        public FtpWebRequest CreateFtpWebRequest(string ftpDirectoryPath, string username, string password, bool keepAlive = false)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(ftpDirectoryPath));

                //Set proxy to null. Under current configuration if this option is not set then the proxy that is used will get an html response from the web content gateway (firewall monitoring system)
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
                Console.WriteLine(ex.StackTrace);

                return null;
            }
            
        }
    }
}
