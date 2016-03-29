//
//  HTTPProtocolHandler.cs
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
using Launchpad.Launcher.Utility.Enums;
using System;
using System.Net;
using System.IO;
using System.Text;
using Launchpad.Launcher.Utility;
using System.Collections.Generic;

namespace Launchpad.Launcher.Handlers.Protocols
{
	/// <summary>
	/// HTTP protocol handler. Patches the launcher and game using the 
	/// HTTP/HTTPS protocol.
	/// 
	/// This protocol uses a manifest.
	/// </summary>
	internal sealed class HTTPProtocolHandler : PatchProtocolHandler
	{
		private readonly ManifestHandler manifestHandler = new ManifestHandler();

		public HTTPProtocolHandler()
			: base()
		{
		}

		public override bool CanPatch()
		{
			bool bCanConnectToServer;

			try
			{
				HttpWebRequest plainRequest = CreateHttpWebRequest(Config.GetBaseHTTPUrl(), 
					                              Config.GetRemoteUsername(), Config.GetRemotePassword());

				plainRequest.Method = WebRequestMethods.Http.Head;
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
					Console.WriteLine("WebException in HTTPProcolHandler.CanPatch(): " + wex.Message);
					bCanConnectToServer = false;
				}
			}
			catch (WebException wex)
			{
				Console.WriteLine("WebException in HTTPProcolHandler.CanPatch() (Invalid URL): " + wex.Message);
				bCanConnectToServer = false;
			}

			if (!bCanConnectToServer)
			{
				Console.WriteLine("Failed to connect to HTTP server at: {0}", Config.GetBaseHTTPUrl());
			}

			return bCanConnectToServer;
		}

		public override bool IsPlatformAvailable(ESystemTarget Platform)
		{
			string remote = String.Format("{0}/game/{1}/.provides", 
				                Config.GetBaseHTTPUrl(), 
				                Platform);

			return DoesRemoteDirectoryOrFileExist(remote);
		}

		public override bool CanProvideChangelog()
		{
			return false;
		}

		public override string GetChangelog()
		{
			return String.Empty;
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
				return false;
			}
		}

		public override void InstallGame()
		{
			ModuleInstallFinishedArgs.Module = EModule.Game;
			ModuleInstallFailedArgs.Module = EModule.Game;

			try
			{
				//create the .install file to mark that an installation has begun
				//if it exists, do nothing.
				ConfigHandler.CreateInstallCookie();

				// Make sure the manifest is up to date
				RefreshGameManifest();

				// Download Game
				DownloadGame();

				// Verify Game
				VerifyGame();
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in InstallGame(): " + ioex.Message);
				OnModuleInstallationFailed();
			}
		}

		public override void DownloadLauncher()
		{
			RefreshLaunchpadManifest();

			ModuleInstallFinishedArgs.Module = EModule.Launcher;
			ModuleInstallFailedArgs.Module = EModule.Launcher;

			List<ManifestEntry> Manifest = manifestHandler.LaunchpadManifest;

			//in order to be able to resume downloading, we check if there is an entry
			//stored in the install cookie.
			//attempt to parse whatever is inside the install cookie
			ManifestEntry lastDownloadedFile;
			if (ManifestEntry.TryParse(File.ReadAllText(ConfigHandler.GetInstallCookiePath()), out lastDownloadedFile))
			{
				//loop through all the entries in the manifest until we encounter
				//an entry which matches the one in the install cookie

				foreach (ManifestEntry Entry in Manifest)
				{
					if (lastDownloadedFile == Entry)
					{
						//remove all entries before the one we were last at.
						Manifest.RemoveRange(0, Manifest.IndexOf(Entry));
					}
				}
			}

			int downloadedFiles = 0;
			foreach (ManifestEntry Entry in Manifest)
			{
				++downloadedFiles;

				// Prepare the progress event contents
				ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(Entry.RelativePath), Manifest.Count);
				OnModuleDownloadProgressChanged();

				DownloadEntry(Entry, EModule.Launcher);
			}

			VerifyLauncher();
		}

		protected override void DownloadGame()
		{
			List<ManifestEntry> Manifest = manifestHandler.GameManifest;

			//in order to be able to resume downloading, we check if there is an entry
			//stored in the install cookie.
			//attempt to parse whatever is inside the install cookie
			ManifestEntry lastDownloadedFile;
			if (ManifestEntry.TryParse(File.ReadAllText(ConfigHandler.GetInstallCookiePath()), out lastDownloadedFile))
			{
				//loop through all the entries in the manifest until we encounter
				//an entry which matches the one in the install cookie

				foreach (ManifestEntry Entry in Manifest)
				{
					if (lastDownloadedFile == Entry)
					{
						//remove all entries before the one we were last at.
						Manifest.RemoveRange(0, Manifest.IndexOf(Entry));
					}
				}
			}

			int downloadedFiles = 0;
			foreach (ManifestEntry Entry in Manifest)
			{
				++downloadedFiles;

				// Prepare the progress event contents
				ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(Entry.RelativePath), Manifest.Count);
				OnModuleDownloadProgressChanged();

				DownloadEntry(Entry, EModule.Game);
			}
		}

		public override void VerifyLauncher()
		{
			try
			{
				List<ManifestEntry> Manifest = manifestHandler.LaunchpadManifest;			
				List<ManifestEntry> BrokenFiles = new List<ManifestEntry>();
							
				int verifiedFiles = 0;
				foreach (ManifestEntry Entry in Manifest)
				{					
					++verifiedFiles;

					// Prepare the progress event contents
					ModuleVerifyProgressArgs.IndicatorLabelMessage = GetVerifyIndicatorLabelMessage(verifiedFiles, Path.GetFileName(Entry.RelativePath), Manifest.Count);
					OnModuleVerifyProgressChanged();

					if (!Entry.IsFileIntegrityIntact())
					{
						BrokenFiles.Add(Entry);
					}
				}

				int downloadedFiles = 0;
				foreach (ManifestEntry Entry in BrokenFiles)
				{					
					++downloadedFiles;

					// Prepare the progress event contents
					ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(Entry.RelativePath), BrokenFiles.Count);
					OnModuleDownloadProgressChanged();

					for (int i = 0; i < Config.GetFileRetries(); ++i)
					{					
						if (!Entry.IsFileIntegrityIntact())
						{
							DownloadEntry(Entry, EModule.Launcher);
						}
						else
						{
							break;
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in VerifyLauncher(): " + ioex.Message);
				OnModuleInstallationFailed();			
			}

			OnModuleInstallationFinished();
		}

		public override void VerifyGame()
		{
			try
			{
				List<ManifestEntry> Manifest = manifestHandler.GameManifest;			
				List<ManifestEntry> BrokenFiles = new List<ManifestEntry>();
							
				int verifiedFiles = 0;
				foreach (ManifestEntry Entry in Manifest)
				{					
					++verifiedFiles;

					// Prepare the progress event contents
					ModuleVerifyProgressArgs.IndicatorLabelMessage = GetVerifyIndicatorLabelMessage(verifiedFiles, Path.GetFileName(Entry.RelativePath), Manifest.Count);
					OnModuleVerifyProgressChanged();

					if (!Entry.IsFileIntegrityIntact())
					{
						BrokenFiles.Add(Entry);
					}
				}

				int downloadedFiles = 0;
				foreach (ManifestEntry Entry in BrokenFiles)
				{					
					++downloadedFiles;

					// Prepare the progress event contents
					ModuleDownloadProgressArgs.IndicatorLabelMessage = GetDownloadIndicatorLabelMessage(downloadedFiles, Path.GetFileName(Entry.RelativePath), BrokenFiles.Count);
					OnModuleDownloadProgressChanged();

					for (int i = 0; i < Config.GetFileRetries(); ++i)
					{					
						if (!Entry.IsFileIntegrityIntact())
						{
							DownloadEntry(Entry, EModule.Game);
						}
						else
						{
							break;
						}
					}
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in VerifyGame(): " + ioex.Message);
				OnModuleInstallationFailed();			
			}

			OnModuleInstallationFinished();
		}

		public override void UpdateGame()
		{
			// Make sure the local copy of the manifest is up to date
			RefreshGameManifest();

			//check all local files against the manifest for file size changes.
			//if the file is missing or the wrong size, download it.
			//better system - compare old & new manifests for changes and download those?
			List<ManifestEntry> Manifest = manifestHandler.GameManifest;
			List<ManifestEntry> OldManifest = manifestHandler.OldGameManifest;

			List<ManifestEntry> FilesRequiringUpdate = new List<ManifestEntry>();
			foreach (ManifestEntry Entry in Manifest)
			{
				if (!OldManifest.Contains(Entry))
				{
					FilesRequiringUpdate.Add(Entry);
				}
			}

			try
			{
				int updatedFiles = 0;
				foreach (ManifestEntry Entry in FilesRequiringUpdate)
				{
					++updatedFiles;

					ModuleUpdateProgressArgs.IndicatorLabelMessage = GetUpdateIndicatorLabelMessage(Path.GetFileName(Entry.RelativePath), 
						updatedFiles, 
						FilesRequiringUpdate.Count);
					OnModuleUpdateProgressChanged();

					DownloadEntry(Entry, EModule.Game);
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in UpdateGameAsync(): " + ioex.Message);
				OnModuleInstallationFailed();
			}

			OnModuleInstallationFinished();
		}

		/// <summary>
		/// Downloads the provided manifest entry.
		/// This function resumes incomplete files, verifies downloaded files and 
		/// downloads missing files.
		/// </summary>
		/// <param name="Entry">The entry to download.</param>
		private void DownloadEntry(ManifestEntry Entry, EModule Module)
		{
			ModuleDownloadProgressArgs.Module = Module;

			string baseRemotePath;
			string baseLocalPath;
			if (Module == EModule.Game)
			{
				baseRemotePath = Config.GetGameURL();
				baseLocalPath = Config.GetGamePath();
			}
			else
			{
				baseRemotePath = Config.GetLauncherBinariesURL();
				baseLocalPath = ConfigHandler.GetTempLauncherDownloadPath();
			}

			string RemotePath = String.Format("{0}{1}", 
				                    baseRemotePath, 
				                    Entry.RelativePath);

			string LocalPath = String.Format("{0}{1}{2}", 
				                   baseLocalPath,
				                   Path.DirectorySeparatorChar, 
				                   Entry.RelativePath);
					                   				
			// Make sure we have a directory to put the file in
			Directory.CreateDirectory(Path.GetDirectoryName(LocalPath));
				
			// Reset the cookie
			File.WriteAllText(ConfigHandler.GetInstallCookiePath(), String.Empty);

			// Write the current file progress to the install cookie
			using (TextWriter textWriterProgress = new StreamWriter(ConfigHandler.GetInstallCookiePath()))
			{					
				textWriterProgress.WriteLine(Entry);
				textWriterProgress.Flush();
			}

			if (File.Exists(LocalPath))
			{
				FileInfo fileInfo = new FileInfo(LocalPath);
				if (fileInfo.Length != Entry.Size)
				{
					// If the file is partial, resume the download.
					if (fileInfo.Length < Entry.Size)
					{
						DownloadRemoteFile(RemotePath, LocalPath, fileInfo.Length);
					}
					else
					{
						// If it's larger than expected, toss it in the bin and try again.
						File.Delete(LocalPath);
						DownloadRemoteFile(RemotePath, LocalPath);
					}
				}									
			}
			else
			{
				//no file, download it
				DownloadRemoteFile(RemotePath, LocalPath);
			}		

			if (ChecksHandler.IsRunningOnUnix())
			{
				//if we're dealing with a file that should be executable, 
				string gameName = Config.GetGameName();
				bool bFileIsGameExecutable = (Path.GetFileName(LocalPath).EndsWith(".exe")) || (Path.GetFileName(LocalPath) == gameName);
				if (bFileIsGameExecutable)
				{
					//set the execute bits
					UnixHandler.MakeExecutable(LocalPath);
				}					
			}

			// We've finished the download, so empty the cookie
			File.WriteAllText(ConfigHandler.GetInstallCookiePath(), String.Empty);
		}

		/// <summary>
		/// Checks if the provided path points to a valid directory or file.
		/// </summary>
		/// <returns><c>true</c>, if the directory or file exists, <c>false</c> otherwise.</returns>
		/// <param name="URL">The remote URL of the directory or file.</param>
		private bool DoesRemoteDirectoryOrFileExist(string URL)
		{
			string cleanURL = URL.Replace(Path.DirectorySeparatorChar, '/');
			HttpWebRequest request = CreateHttpWebRequest(cleanURL, 
				                         Config.GetRemoteUsername(), Config.GetRemotePassword());

			request.Method = WebRequestMethods.Http.Head;
			HttpWebResponse response = null;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return false;
				}
			}
			catch (WebException wex)
			{
				response = (HttpWebResponse)wex.Response;
				if (response.StatusCode == HttpStatusCode.NotFound)
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

		/// <summary>
		/// Downloads a remote file to a local file path.
		/// </summary>
		/// <param name="URL">The remote URL of the file..</param>
		/// <param name="localPath">Local path where the file is to be stored.</param>
		/// <param name="contentOffset">Content offset. If nonzero, appends data to an existing file.</param>
		/// <param name="useAnonymousLogin">If set to <c>true</c> use anonymous login.</param>
		private void DownloadRemoteFile(string URL, string localPath, long contentOffset = 0, bool useAnonymousLogin = false)
		{
			//clean the URL string
			string remoteURL = URL.Replace(Path.DirectorySeparatorChar, '/');

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
				HttpWebRequest request = CreateHttpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Http.Get;
				request.AddRange(contentOffset);

				using (Stream contentStream = request.GetResponse().GetResponseStream())
				{
					using (FileStream fileStream = contentOffset > 0 ? new FileStream(localPath, FileMode.Append) :
																		new FileStream(localPath, FileMode.Create))
					{
						fileStream.Position = contentOffset;
						long totalBytesDownloaded = contentOffset;
						long totalFileSize = contentOffset + contentStream.Length;

						if (contentStream.Length < 4096)
						{
							byte[] smallBuffer = new byte[contentStream.Length];
							contentStream.Read(smallBuffer, 0, smallBuffer.Length);

							fileStream.Write(smallBuffer, 0, smallBuffer.Length);

							totalBytesDownloaded += smallBuffer.Length;

							// Report download progress
							ModuleDownloadProgressArgs.ProgressBarMessage = GetDownloadProgressBarMessage(Path.GetFileName(remoteURL), 
								totalBytesDownloaded, totalFileSize);
							ModuleDownloadProgressArgs.ProgressFraction = (double)totalBytesDownloaded / (double)totalFileSize;
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
									totalBytesDownloaded, totalFileSize);
								ModuleDownloadProgressArgs.ProgressFraction = (double)totalBytesDownloaded / (double)totalFileSize;
								OnModuleDownloadProgressChanged();
							}
						}

						fileStream.Flush();
					}
				}
			}
			catch (WebException wex)
			{
				Console.Write("WebException in DownloadRemoteFile(): ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
			}
			catch (IOException ioex)
			{
				Console.Write("IOException in DownloadRemoteFile(): ");
				Console.WriteLine(ioex.Message + " (" + remoteURL + ")");
			}
		}

		/// <summary>
		/// Reads the string content of a remote file. The output is scrubbed
		/// of all \r, \n and \0 characters before it is returned.
		/// </summary>
		/// <returns>The contents of the remote file.</returns>
		/// <param name="URL">The remote URL of the file.</param>
		/// <param name="useAnonymousLogin">If set to <c>true</c> use anonymous login.</param>
		private string ReadRemoteFile(string URL, bool useAnonymousLogin = false)
		{
			string remoteURL = URL.Replace(Path.DirectorySeparatorChar, '/');

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
				HttpWebRequest request = CreateHttpWebRequest(remoteURL, username, password);

				request.Method = WebRequestMethods.Http.Get;

				string data = "";
				using (Stream remoteStream = request.GetResponse().GetResponseStream())
				{
					if (remoteStream.Length < 262144)
					{
						byte[] smallBuffer = new byte[remoteStream.Length];
						remoteStream.Read(smallBuffer, 0, smallBuffer.Length);

						data = Encoding.UTF8.GetString(smallBuffer, 0, smallBuffer.Length);
					}
					else
					{
						// The large buffer size is 256kb. More or less than this reduces download speeds.
						byte[] buffer = new byte[262144];

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

				return Utilities.Clean(data);
			}
			catch (WebException wex)
			{
				Console.Write("WebException in ReadRemoteFile(): ");
				Console.WriteLine(wex.Message + " (" + remoteURL + ")");
				return wex.Message;
			}
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while installing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="nFilesDownloaded">N files downloaded.</param>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="totalFilesToDownload">Total files to download.</param>
		private string GetDownloadIndicatorLabelMessage(int nFilesDownloaded, string currentFilename, int totalFilesToDownload)
		{
			return String.Format("Downloading file {0} ({1} of {2})", currentFilename, nFilesDownloaded, totalFilesToDownload);
		}

		/// <summary>
		/// Gets the progress bar message.
		/// </summary>
		/// <returns>The progress bar message.</returns>
		/// <param name="filename">Filename.</param>
		/// <param name="downloadedBytes">Downloaded bytes.</param>
		/// <param name="totalBytes">Total bytes.</param>
		private string GetDownloadProgressBarMessage(string filename, long downloadedBytes, long totalBytes)
		{
			return String.Format("Downloading {0}: {1} out of {2} bytes", filename, downloadedBytes, totalBytes);
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while repairing.
		/// </summary>
		/// <returns>The indicator label message.</returns>
		/// <param name="nFilesToVerify">N files downloaded.</param>
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="totalFilesVerified">Total files to download.</param>
		private string GetVerifyIndicatorLabelMessage(int nFilesToVerify, string currentFilename, int totalFilesVerified)
		{
			return String.Format("Verifying file {0} ({1} of {2})", currentFilename, nFilesToVerify, totalFilesVerified);
		}

		/// <summary>
		/// Gets the indicator label message to display to the user while repairing.
		/// </summary>
		/// <returns>The indicator label message.</returns>	
		/// <param name="currentFilename">Current filename.</param>
		/// <param name="updatedFiles">Number of files that have been updated</param>
		/// <param name="totalFiles">Total files that are to be updated</param>
		private string GetUpdateIndicatorLabelMessage(string currentFilename, int updatedFiles, int totalFiles)
		{
			return String.Format("Verifying file {0} ({1} of {2})", currentFilename, updatedFiles, totalFiles);
		}

		/// <summary>
		/// Creates a HTTP web request.
		/// </summary>
		/// <returns>The HTTP web request.</returns>
		/// <param name="URL">URL of the desired remote object.</param>
		/// <param name="Username">The username used for authentication.</param>
		/// <param name="Password">The password used for authentication.</param>
		private HttpWebRequest CreateHttpWebRequest(string URL, string Username, string Password)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
				request.Proxy = null;
				request.Credentials = new NetworkCredential(Username, Password);

				return request;
			}
			catch (WebException wex)
			{
				Console.WriteLine("WebException in CreateHttpWebRequest(): " + wex.Message);

				return null;
			}
			catch (ArgumentException aex)
			{
				Console.WriteLine("ArgumentException in CreateHttpWebRequest(): " + aex.Message);

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
			string remoteVersion = ReadRemoteFile(remoteVersionPath, Config.GetDoOfficialUpdates());

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
				                           Config.GetBaseHTTPUrl(), 
				                           Config.GetSystemTarget());

			string remoteVersion = ReadRemoteFile(remoteVersionPath);

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
		/// Refreshs the local copy of the manifest.
		/// </summary>
		private void RefreshGameManifest()
		{
			if (File.Exists(ManifestHandler.GetGameManifestPath()))
			{
				if (IsGameManifestOutdated())
				{
					DownloadGameManifest();
				}
			}
			else
			{
				DownloadGameManifest();
			}
		}

		/// <summary>
		/// Refreshs the local copy of the launchpad manifest.
		/// </summary>
		private void RefreshLaunchpadManifest()
		{
			if (File.Exists(ManifestHandler.GetLaunchpadManifestPath()))
			{
				if (IsLaunchpadManifestOutdated())
				{
					DownloadLaunchpadManifest();
				}
			}
			else
			{
				DownloadLaunchpadManifest();
			}
		}

		//TODO: Maybe move to ManifestHandler?
		/// <summary>
		/// Determines whether the  manifest is outdated.
		/// </summary>
		/// <returns><c>true</c> if the manifest is outdated; otherwise, <c>false</c>.</returns>
		private bool IsGameManifestOutdated()
		{
			if (File.Exists(ManifestHandler.GetGameManifestPath()))
			{
				string remoteHash = GetRemoteGameManifestChecksum();

				using (Stream file = File.OpenRead(ManifestHandler.GetGameManifestPath()))
				{
					string localHash = MD5Handler.GetStreamHash(file);

					return remoteHash != localHash;
				}
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Determines whether the launchpad manifest is outdated.
		/// </summary>
		/// <returns><c>true</c> if the manifest is outdated; otherwise, <c>false</c>.</returns>
		private bool IsLaunchpadManifestOutdated()
		{
			if (File.Exists(ManifestHandler.GetLaunchpadManifestPath()))
			{
				string remoteHash = GetRemoteLaunchpadManifestChecksum();

				using (Stream file = File.OpenRead(ManifestHandler.GetLaunchpadManifestPath()))
				{
					string localHash = MD5Handler.GetStreamHash(file);

					return remoteHash != localHash;
				}
			}
			else
			{
				return true;
			}
		}

		//TODO: Maybe move to ManifestHandler?
		/// <summary>
		/// Gets the remote manifest checksum.
		/// </summary>
		/// <returns>The remote manifest checksum.</returns>
		public string GetRemoteGameManifestChecksum()
		{
			string checksum = ReadRemoteFile(manifestHandler.GetGameManifestChecksumURL());
			checksum = Utilities.Clean(checksum);

			return checksum;
		}

		/// <summary>
		/// Gets the remote launchpad manifest checksum.
		/// </summary>
		/// <returns>The remote launchpad manifest checksum.</returns>
		public string GetRemoteLaunchpadManifestChecksum()
		{
			string checksum = ReadRemoteFile(manifestHandler.GetLaunchpadManifestChecksumURL());
			checksum = Utilities.Clean(checksum);

			return checksum;
		}

		/// <summary>
		/// Downloads the game manifest.
		/// </summary>
		private void DownloadGameManifest()
		{
			try
			{
				string RemoteURL = manifestHandler.GetGameManifestURL();
				string LocalPath = ManifestHandler.GetGameManifestPath();
				string OldLocalPath = ManifestHandler.GetOldGameManifestPath();

				if (File.Exists(ManifestHandler.GetGameManifestPath()))
				{
					// Create a backup of the old manifest so that we can compare them when updating the game
					if (File.Exists(OldLocalPath))
					{
						File.Delete(OldLocalPath);
					}

					File.Move(LocalPath, OldLocalPath);			
				}						

				DownloadRemoteFile(RemoteURL, LocalPath);
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in DownloadGameManifest(): " + ioex.Message);
			}
		}

		/// <summary>
		/// Downloads the launchpad manifest.
		/// </summary>
		private void DownloadLaunchpadManifest()
		{
			try
			{
				string RemoteURL = manifestHandler.GetLaunchpadManifestURL();
				string LocalPath = ManifestHandler.GetLaunchpadManifestPath();
				string OldLocalPath = ManifestHandler.GetOldLaunchpadManifestPath();

				if (File.Exists(LocalPath))
				{
					// Create a backup of the old manifest so that we can compare them when updating the game
					if (File.Exists(OldLocalPath))
					{
						File.Delete(OldLocalPath);
					}

					File.Move(LocalPath, OldLocalPath);			
				}						

				DownloadRemoteFile(RemoteURL, LocalPath);
			}
			catch (IOException ioex)
			{
				Console.WriteLine("IOException in DownloadLaunchpadManifest(): " + ioex.Message);
			}
		}
	}
}

