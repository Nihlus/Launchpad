using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Security.Cryptography;

namespace Launchpad
{
	/// <summary>
	/// MD5 hashing handler. Used to ensure file integrity.
	/// </summary>
    public sealed class MD5Handler
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="Launchpad_Launcher.MD5Handler"/> class.
		/// </summary>
		public MD5Handler()
		{

		}
        /// <summary>
        /// Gets the file hash from a file stream.
        /// </summary>
        /// <returns>The file hash.</returns>
        /// <param name="fileStream">File stream.</param>
        public string GetFileHash(Stream fileStream)
        {

            try
            {
                using (var md5 = MD5.Create())
                {
                    //we got a valid file, calculate the MD5 and return it
                    var resultString = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "");

                    return resultString;
                }
            }
            catch (Exception ex)
            {
				Console.Write ("MD5 Hash Computing: ");
				Console.WriteLine (ex.Message);

                return "";
            }
			finally
			{
				//release the file (if we had one)
				fileStream.Close();
			}
        }
    }
}
