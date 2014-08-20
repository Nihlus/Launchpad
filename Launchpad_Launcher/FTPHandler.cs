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

        public string ReadFTPFile(string username, string password, string ftpSourceFilePath)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[64];

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

        public void DownloadFTPFile(string username, string password, string ftpSourceFilePath, string localDestination)
        {
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
