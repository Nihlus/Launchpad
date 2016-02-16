using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using Launchpad.Launcher.Events.Arguments;
using Launchpad.Launcher.Events.Delegates;

namespace Launchpad.Launcher
{
	/// <summary>
	/// Patch handler. Handles downloading and reading files on a remote Patch server.
	/// There are also functions for retrieving remote version information of the game and the launcher.
	/// </summary>
    internal sealed class HTTPHandler
    {
		/// <summary>
		/// How many bytes of the target file that have been downloaded.
		/// </summary>
        public int PatchbytesDownloaded = 0;

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
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.PatchHandler"/> class.
		/// </summary>
		public HTTPHandler()
		{
			ProgressArgs = new FileDownloadProgressChangedEventArgs ();
			DownloadFinishedArgs = new FileDownloadFinishedEventArgs ();
		}
        
		/// <summary>
		/// Reads a text file from a remote Patch server.
		/// </summary>
		/// <returns>The Patch file contents.</returns>
		/// <param name="PatchSourceFilePath">Patch file path.</param>
        public string ReadPatchFile(string rawRemoteURL)
        {
			//clean the input URL first
			string remoteURL = rawRemoteURL.Replace (Path.DirectorySeparatorChar, '/');

			string username = Config.GetPatchUsername();
			string password = Config.GetPatchPassword();

            int bytesRead = 0;

			//the buffer size is 256kb. More or less than this reduces download speeds.
            byte[] buffer = new byte[262144];

            IAsyncResult asyncResult;

            HttpWebRequest request = null;


            try
            {

	            request = CreateHttpWebRequest(remoteURL, username, password, false);

	            request.Method = WebRequestMethods.Http.Get;

                string data = "";

                using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                {
                    data = new StreamReader(webResponse.GetResponseStream(), Encoding.Default).ReadToEnd();
                }

				//clean the output from \n and \0, then return
				return Utilities.Clean (data);
            }
            catch (WebException wex)
            {
                Console.Write("WebException in ReadPatchFileException: ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
                return wex.Message;
            }
			finally
			{

				//and finally, the requests themselves.
				if (request != null)
				{
					request.Abort();
				}

			}


        }


		/// <summary>
		/// Downloads an Patch file.
		/// </summary>
		/// <returns>The Patch file's location on disk, or the exception message.</returns>
		/// <param name="PatchSourceFilePath">Patch source file path.</param>
		/// <param name="localDestination">Local destination.</param>
		/// <param name="bUseAnonymous">If set to <c>true</c> b use anonymous.</param>
        public string DownloadPatchFile(string rawRemoteURL, string localPath, bool bUseAnonymous)
        {
			//clean the URL string
			string remoteURL = rawRemoteURL.Replace (Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (!bUseAnonymous)
			{
				username = Config.GetPatchUsername ();
				password = Config.GetPatchPassword ();
			}
			else
			{
				username = "anonymous";
				password = "anonymous";
			}


            int bytesRead = 0;

			//the buffer size is 256kb. More or less than this reduces download speeds.
            byte[] buffer = new byte[262144];

			HttpWebRequest request = null;
			HttpWebRequest sizerequest = null;

			Stream reader = null;
			HttpWebResponse sizereader = null;

			FileStream fileStream = null;

			//either a path to the file or an error message
			string returnValue = "";

			try
			{
                request = CreateHttpWebRequest(remoteURL, username, password, false);
                sizerequest = CreateHttpWebRequest(remoteURL, username, password, false);

                request.Method = WebRequestMethods.Http.Get;

	            long fileSize = 0;

            
				reader = request.GetResponse().GetResponseStream();
					
				fileStream = new FileStream(localPath, FileMode.Create);

				//reset byte counter
				PatchbytesDownloaded = 0;

                fileSize = request.GetResponse().ContentLength;

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

					PatchbytesDownloaded = PatchbytesDownloaded + bytesRead;
					fileStream.Write(buffer, 0, bytesRead);

					//set file progress info
					ProgressArgs.DownloadedBytes = PatchbytesDownloaded;

					OnProgressChanged();
				}

                OnProgressChanged();
				OnDownloadFinished();

				returnValue = localPath;
				return returnValue;             				                             
            }
            catch (WebException wex)
            {
                Console.Write("WebException in DownloadPatchFile: ");
                Console.WriteLine(wex.Message + " (" + remoteURL + ")");
                returnValue = wex.Message;

				return returnValue;
            }
            catch (IOException ioex)
            {
                Console.Write("IOException in DownloadPatchFile: ");
				Console.WriteLine(ioex.Message  + " (" + remoteURL + ")");
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
		/// Downloads an Patch file.
		/// </summary>
		/// <returns>The Patch file's location on disk, or the exception message.</returns>
		/// <param name="PatchSourceFilePath">Patch source file path.</param>
		/// <param name="localDestination">Local destination.</param>
		/// <param name="bUseAnonymous">If set to <c>true</c> b use anonymous.</param>
		/// <param name="contentOffset">The content offset where the download should resume.</param>
		public string DownloadPatchFile(string rawRemoteURL, string localPath, long contentOffset, bool bUseAnonymous)
		{
			//clean the URL string first
			string remoteURL = rawRemoteURL.Replace (Path.DirectorySeparatorChar, '/');

			string username;
			string password;
			if (!bUseAnonymous)
			{
				username = Config.GetPatchUsername ();
				password = Config.GetPatchPassword ();
			}
			else
			{
				username = "anonymous";
				password = "anonymous";
			}


			int bytesRead = 0;

			//the buffer size is 256kb. More or less than this reduces download speeds.
            byte[] buffer = new byte[262144];

			HttpWebRequest request = null;
			HttpWebRequest sizerequest = null;

			Stream reader = null;
			HttpWebResponse sizereader = null;

			FileStream fileStream = null;

			//either a path to the file or an error message
			string returnValue = "";

			try
			{
				request = CreateHttpWebRequest(remoteURL, username, password, true);
				sizerequest = CreateHttpWebRequest(remoteURL, username, password, true);

                request.Method = WebRequestMethods.Http.Get;
                request.AddRange(contentOffset);

                long fileSize = 0;

				reader = request.GetResponse().GetResponseStream();

				fileStream = new FileStream(localPath, FileMode.Append);

				//reset byte counter
				PatchbytesDownloaded = 0;

                fileSize = reader.Length + contentOffset;

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

					PatchbytesDownloaded = PatchbytesDownloaded + bytesRead;
					fileStream.Write(buffer, 0, bytesRead);

					//set file progress info
					ProgressArgs.DownloadedBytes = (int)contentOffset + PatchbytesDownloaded;

					OnProgressChanged();
				}

				OnProgressChanged();
				OnDownloadFinished();

				returnValue = localPath;
				return returnValue;             				                             
			}
			catch (WebException wex)
			{
				Console.Write("WebException in DownloadPatchFile (appending): ");
				Console.WriteLine(wex.Message  + " (" + remoteURL + ")");
				returnValue = wex.Message;

				return returnValue;
			}
			catch (IOException ioex)
			{
				Console.Write("IOException in DownloadPatchFile (appending): ");
				Console.WriteLine(ioex.Message  + " (" + remoteURL + ")");
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
		/// Creates an Patch web request.
		/// </summary>
		/// <returns>The Patch web request.</returns>
		/// <param name="PatchDirectoryPath">Patch directory path.</param>
		/// <param name="keepAlive">If set to <c>true</c> keep alive.</param>
        public static HttpWebRequest CreateHttpWebRequest(string PatchDirectoryPath, string username, string password, bool keepAlive)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(PatchDirectoryPath));

                //Set proxy to null. Under current configuration if this option is not set then the proxy 
				//that is used will get an html response from the web content gateway (firewall monitoring system)
                request.Proxy = null;

                request.KeepAlive = keepAlive;

                request.Credentials = new NetworkCredential(username, password);

                return request;
            }
            catch (WebException wex)
            {
                Console.WriteLine ("WebException in CreateHttpWebRequest(): " + wex.Message);

                return null;
            }
			catch (ArgumentException aex)
			{
				Console.WriteLine ("ArgumentException in CreateHttpWebRequest(): " + aex.Message);

				return null;
			}
            
        }

		/// <summary>
		/// Gets the remote launcher version.
		/// </summary>
		/// <returns>The remote launcher version.</returns>
		public Version GetRemoteLauncherVersion()
		{
			string remoteVersionPath = String.Format ("{0}/launcher/LauncherVersion.txt", Config.GetPatchUrl());
			string remoteVersion = ReadPatchFile (remoteVersionPath);

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
				remoteVersionPath = String.Format ("{0}/game/{1}/bin/GameVersion.txt", 
				                                   Config.GetPatchUrl(), 
				                                   Config.GetSystemTarget());

			}
			else
			{
				remoteVersionPath = String.Format ("{0}/game/bin/GameVersion.txt", 
				                                   Config.GetPatchUrl());

			}
			string remoteVersion = ReadPatchFile (remoteVersionPath);

			if (!string.IsNullOrEmpty (remoteVersion))
			{
				return Version.Parse (remoteVersion);
			}
			else
			{
				return null;
			}
		}

		public string GetRemoteManifestChecksum()
		{
			string checksum = String.Empty;

			checksum = ReadPatchFile (Config.GetManifestChecksumURL ());
			checksum = Utilities.Clean(checksum);

			return checksum;
		}

		public bool DoesFileExist(string remotePath)
		{
			HttpWebRequest request = CreateHttpWebRequest (remotePath, 
			                                            Config.GetPatchUsername (),
			                                            Config.GetPatchPassword (),
			                                            false);
			HttpWebResponse response = null;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				response = (HttpWebResponse)ex.Response;
				if (response.StatusCode == HttpStatusCode.NotFound)
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
