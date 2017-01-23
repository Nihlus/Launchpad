//
//  ManifestTests.cs
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

using Launchpad.Launcher.Handlers.Protocols.Manifest;
using NUnit.Framework;

namespace Launchpad.Tests.Launcher
{
	[TestFixture]
	public class ManifestTests
	{
		private readonly ManifestEntry ExpectedObject = new ManifestEntry
		{
			RelativePath = "/data/content.pak",
			Hash = "6A99C575AB87F8C7D1ED1E52E7E349CE",
			Size = 2000000000
		};

		private const string ValidInput = "/data/content.pak:6A99C575AB87F8C7D1ED1E52E7E349CE:2000000000";
		private const string InvalidInputNegativeSize = "/data/content.pak:6A99C575AB87F8C7D1ED1E52E7E349CE:-1";
		private const string InvalidInputHashTooShort = "/data/content.pak:6A99C575AB8:2000000000";
		private const string InvalidInputTooManyElements = "/data/content.pak:6A99C575AB87F8C7D1ED1E52E7E349CE:2000000000:ExtraData";
		private const string InvalidInputInvalidNumber = "/data/content.pak:6A99C575AB87F8C7D1ED1E52E7E349CE:deadbeef";
		private const string InvalidInputMissingHash = "/data/content.pak::2000000000";


		private readonly ManifestEntry ValidObject1 = new ManifestEntry
		{
			RelativePath = "/data/content1.pak",
			Hash = "6A99C575AB87F8C7D1ED1E52E7E349CE",
			Size = 3000000000
		};

		private readonly ManifestEntry ValidObject2 = new ManifestEntry
		{
			RelativePath = "/data/content2.pak",
			Hash = "6a99c575ab87f8c7d1ed1e52e7e349ce",
			Size = 4000000000
		};

		private readonly ManifestEntry ValidObject3EqualTo2 = new ManifestEntry
		{
			RelativePath = "/data/content2.pak",
			Hash = "6A99C575AB87F8C7D1ED1E52E7E349CE",
			Size = 4000000000
		};

		private const string ExpectedOutputString = ValidInput;

		[Test]
		public void TestCreateFromValidInput()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(ValidInput, out createdEntry);

			Assert.That(parsingSucceded);
			Assert.AreEqual(this.ExpectedObject, createdEntry);
		}

		[Test]
		public void TestInvalidNegativeSize()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(InvalidInputNegativeSize, out createdEntry);

			Assert.That(!parsingSucceded);
		}

		[Test]
		public void TestInvalidHashTooShort()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(InvalidInputHashTooShort, out createdEntry);

			Assert.That(!parsingSucceded);
		}

		[Test]
		public void TestInvalidTooManyElements()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(InvalidInputTooManyElements, out createdEntry);

			Assert.That(!parsingSucceded);
		}

		[Test]
		public void TestInvalidInvalidNumber()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(InvalidInputInvalidNumber, out createdEntry);

			Assert.That(!parsingSucceded);
		}

		[Test]
		public void TestInvalidMissingHash()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(InvalidInputMissingHash, out createdEntry);

			Assert.That(!parsingSucceded);
		}

		[Test]
		public void TestToString()
		{
			Assert.AreEqual(ExpectedOutputString, this.ExpectedObject.ToString());
		}

		[Test]
		public void TestObjectsEqual()
		{
			Assert.That(this.ValidObject1.Equals(this.ValidObject1));
		}

		[Test]
		public void TestObjectsEqualDifferentHashCase()
		{
			Assert.That(this.ValidObject2.Equals(this.ValidObject3EqualTo2));
		}

		[Test]
		public void TestObjectsNotEqual()
		{
			Assert.That(!this.ValidObject1.Equals(this.ValidObject2));
		}
	}
}