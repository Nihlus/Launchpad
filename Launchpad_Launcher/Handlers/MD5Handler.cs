using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Security.Cryptography;

namespace Launchpad_Launcher
{
    public class MD5Handler
    {
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

                    //release the file
                    fileStream.Close();
                    return resultString;
                }
            }
            catch (IOException)
            {
                //release the file (if we had one)
                fileStream.Close();
                return "ERROR - IOException";
            }
        }
    }
}
