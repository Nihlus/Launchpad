using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Launchpad.Launcher.Events.Arguments;
using Launchpad.Launcher.Events.Delegates;


namespace Launchpad.Launcher
{
    internal sealed class ProtocolHandler
    {
        bool bUseHTTP = true;

        private HTTPHandler HTTP = new HTTPHandler();

        private FTPHandler FTP = new FTPHandler();

        public ProtocolHandler(bool bHTTP)
        {
            bUseHTTP = bHTTP;
        }


        /// <summary>
        /// Gets the relative paths for all files in the specified Patch directory.
        /// </summary>
        /// <param name="rawRemoteURL">The URL to search.</param>
        /// <param name="bRecursively">Should the search should include subdirectories?</param>
        /// <returns>A list of relative paths for the files in the specified directory.</returns>
        public List<string> GetFilePaths(string rawRemoteURL, bool bRecursively)
        {
            if (bUseHTTP)
            {
                return null;
            }
            else
            {
                return FTP.GetFilePaths( rawRemoteURL, bRecursively);
            }
        }


        /// <summary>
        /// Reads a text file from a remote Patch server.
        /// </summary>
        /// <returns>The Patch file contents.</returns>
        /// <param name="PatchSourceFilePath">Patch file path.</param>
        public string ReadPatchFile(string rawRemoteURL)
        {

            if ( bUseHTTP )
            {
                return HTTP.ReadPatchFile(rawRemoteURL);
            }
            else
            {
                return FTP.ReadPatchFile(rawRemoteURL);
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
            if ( bUseHTTP )
            {
                return HTTP.DownloadPatchFile( rawRemoteURL, localPath, bUseAnonymous);
            }
            else
            {
                return FTP.DownloadPatchFile( rawRemoteURL, localPath, bUseAnonymous);
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
            if ( bUseHTTP )
            {
                return HTTP.DownloadPatchFile( rawRemoteURL, localPath, contentOffset, bUseAnonymous);
            }
            else
            {
                return FTP.DownloadPatchFile( rawRemoteURL, localPath, contentOffset, bUseAnonymous);
            }
        }

        /// <summary>
        /// Creates an Patch web request.
        /// </summary>
        /// <returns>The Patch web request.</returns>
        /// <param name="PatchDirectoryPath">Patch directory path.</param>
        /// <param name="keepAlive">If set to <c>true</c> keep alive.</param>
        public WebRequest CreateHttpWebRequest(string PatchDirectoryPath, string username, string password, bool keepAlive)
        {
            if( bUseHTTP )
            {
                return HTTPHandler.CreateHttpWebRequest( PatchDirectoryPath, username, password, keepAlive );
            }
            else
            {
                return FTPHandler.CreateFtpWebRequest(PatchDirectoryPath, username, password, keepAlive);
            }
        }

        /// <summary>
        /// Gets the remote launcher version.
        /// </summary>
        /// <returns>The remote launcher version.</returns>
        public Version GetRemoteLauncherVersion()
        {
            if ( bUseHTTP )
            {
                return HTTP.GetRemoteLauncherVersion();
            }
            else
            {
                return FTP.GetRemoteLauncherVersion();
            }
        }

        /// <summary>
        /// Gets the remote game version.
        /// </summary>
        /// <returns>The remote game version.</returns>
        public Version GetRemoteGameVersion(bool bUseSystemTarget)
        {
            if ( bUseHTTP )
            {
                return HTTP.GetRemoteGameVersion(bUseSystemTarget);
            }
            else
            {
                return FTP.GetRemoteGameVersion(bUseSystemTarget);
            }
        }

        public string GetRemoteManifestChecksum()
        {
            if ( bUseHTTP )
            {
                return HTTP.GetRemoteManifestChecksum();
            }
            else
            {
                return FTP.GetRemoteManifestChecksum();
            }
        }

        public bool DoesFileExist(string remotePath)
        {
            if ( bUseHTTP )
            {
                return HTTP.DoesFileExist(remotePath);
            }
            else
            {
                return FTP.DoesFileExist(remotePath);
            }
        }

        /// Wrappers for the two events we need for the bar graph to work.
        /// <summary>
        /// Occurs when file progress changed.
        /// </summary>
        public void FileProgressChanged(FileProgressChangedEventHandler EventArgs, bool bAdd)
        {
            if (bUseHTTP)
            {
                if (bAdd)
                {
                    HTTP.FileProgressChanged += EventArgs;
                }
                else
                {
                    HTTP.FileProgressChanged -= EventArgs;
                }
            }
            else
            {
                if (bAdd)
                {
                    FTP.FileProgressChanged += EventArgs;
                }
                else
                {
                    FTP.FileProgressChanged += EventArgs;
                }
            }
        }

        /// <summary>
        /// Occurs when file download finished.
        /// </summary>
        public void FileDownloadFinished(FileDownloadFinishedEventHandler EventArgs, bool bAdd)
        {
            if (bUseHTTP)
            {
                if (bAdd)
                {
                    HTTP.FileDownloadFinished += EventArgs;
                }
                else
                {
                    HTTP.FileDownloadFinished -= EventArgs;
                }
            }
            else
            {
                if (bAdd)
                {
                    FTP.FileDownloadFinished += EventArgs;
                }
                else
                {
                    FTP.FileDownloadFinished += EventArgs;
                }
            }
        }

    }
}
