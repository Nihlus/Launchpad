using System;
using System.IO;
using System.Security.Cryptography;

namespace Launchpad.Launcher
{
	/// <summary>
	/// MD5 hashing handler. Used to ensure file integrity.
	/// </summary>
    internal static class MD5Handler
    {
        /// <summary>
        /// Gets the file hash from a file stream.
        /// </summary>
        /// <returns>The file hash.</returns>
        /// <param name="fileStream">File stream.</param>
        public static string GetFileHash(Stream fileStream)
        {
			if (fileStream != null)
			{
				try
				{
					using (MD5 md5 = MD5.Create())
					{
						//calculate the hash of the stream.
						string resultString = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "");

						return resultString;
					}
				}
				catch (IOException ioex)
				{
					Console.WriteLine ("IOException in GetFileHash(): " + ioex.Message);

					return String.Empty;
				}
				finally
				{
					//release the file (if we had one)
					fileStream.Close();
				}
			}     
			else
			{ 
				return String.Empty;
			}
        }
    }
}
