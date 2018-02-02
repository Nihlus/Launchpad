//
//  MD5HandlerTests.cs
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

using System.IO;
using Launchpad.Common.Handlers;
using Xunit;

namespace Launchpad.Tests.Common
{
	public class MD5HandlerTests
	{
		/// <summary>
		/// Holds the string "placeholder".
		/// </summary>
		private readonly MemoryStream DataStream = new MemoryStream(new byte[]
		{
			112, 108, 97, 99, 101, 104, 111, 108, 100, 101, 114
		});

		private const string ExpectedHash = "6A99C575AB87F8C7D1ED1E52E7E349CE";

		[Fact]
		public void HashesCorrectly()
		{
			Assert.Equal(ExpectedHash, MD5Handler.GetStreamHash(this.DataStream));
		}
	}
}