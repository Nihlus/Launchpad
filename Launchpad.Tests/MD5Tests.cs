//
//  Program.cs
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

using System.IO;
using Launchpad.Launcher.Handlers;
using NUnit.Framework;

namespace Launchpad.Launcher
{
	[TestFixture]
    public class MD5Tests
	{
		private const string SampleFilePath = "677d63e6c4119e2f23b782195c016404";

	    [Test]
	    public void HashFile()
	    {
		    using (Stream fileStream = File.OpenRead("Resources/SampleFile"))
		    {
			    string hash = MD5Handler.GetStreamHash(fileStream);

			    Assert.AreEqual(SampleFilePath, hash);
		    }
	    }
    }
}
