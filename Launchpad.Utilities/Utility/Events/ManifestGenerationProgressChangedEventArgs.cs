//
//  ManifestGenerationProgressChangedEventArgs.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
//

using System;

namespace Launchpad.Utilities.Utility.Events
{
    /// <summary>
    /// Represents progress in a manifest generation.
    /// </summary>
    public class ManifestGenerationProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the current file path.
        /// </summary>
        public string Filepath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the total number of files.
        /// </summary>
        public int TotalFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of completed files.
        /// </summary>
        public int CompletedFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current hash.
        /// </summary>
        public string Hash
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the current file.
        /// </summary>
        public long Size
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestGenerationProgressChangedEventArgs"/> class.
        /// </summary>
        /// <param name="filepath">The current file path.</param>
        /// <param name="totalFiles">The total number of files.</param>
        /// <param name="completedFiles">The number of completed files.</param>
        /// <param name="hash">The current hash.</param>
        /// <param name="size">The size of the current file.</param>
        public ManifestGenerationProgressChangedEventArgs
    (
            string filepath,
            int totalFiles,
            int completedFiles,
            string hash,
            long size
        )
        {
            this.Filepath = filepath;
            this.TotalFiles = totalFiles;
            this.CompletedFiles = completedFiles;
            this.Hash = hash;
            this.Size = size;
        }
    }
}
