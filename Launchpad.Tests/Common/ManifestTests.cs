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

using System.Collections.Generic;
using System.IO;
using System.Text;
using Launchpad.Common.Handlers.Manifest;
using NUnit.Framework;

namespace Launchpad.Tests.Common
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

		private const string SampleManifestWindowsStyle =
			"\\GameVersion.txt:7D23FF901039AEF6293954D33D23C066:5\r\n" +
			"\\MyGame.exe:D41D8CD98F00B204E9800998ECF8427E:0\r\n" +
			"\\MyGame.txt:170606695BC36CDC3455E29ADBEE0D40:28\r\n";

		private const string SampleManifestUnixStyle =
			"/GameVersion.txt:7D23FF901039AEF6293954D33D23C066:5\n" +
			"/MyGame.exe:D41D8CD98F00B204E9800998ECF8427E:0\n" +
			"/MyGame.txt:170606695BC36CDC3455E29ADBEE0D40:28\n";

		private static readonly List<ManifestEntry> SampleManifestEntries = new List<ManifestEntry>
		{
			new ManifestEntry
			{
				Hash = "7D23FF901039AEF6293954D33D23C066",
				RelativePath = "/GameVersion.txt",
				Size = 5
			},
			new ManifestEntry
			{
				Hash = "D41D8CD98F00B204E9800998ECF8427E",
				RelativePath = "/MyGame.exe",
				Size = 0
			},
			new ManifestEntry
			{
				Hash = "170606695BC36CDC3455E29ADBEE0D40",
				RelativePath = "/MyGame.txt",
				Size = 28
			}
		};


		[Test]
		public void TestCreateFromValidInput()
		{
			ManifestEntry createdEntry;
			bool parsingSucceded = ManifestEntry.TryParse(ValidInput, out createdEntry);

			Assert.That(parsingSucceded);
			Assert.AreEqual(this.ExpectedObject, createdEntry);
		}

		[Test]
		public void TestInvalidEmptyString()
		{
			bool parsingSucceeded = ManifestEntry.TryParse(string.Empty, out ManifestEntry _);
			Assert.That(!parsingSucceeded);
		}

		[Test]
		public void TestInvalidNullInput()
		{
			bool parsingSucceeded = ManifestEntry.TryParse(null, out ManifestEntry _);
			Assert.That(!parsingSucceeded);
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
		public void TestObjectsNotEqualNull()
		{
			Assert.That(this.ValidObject1.Equals(null));
		}

		[Test]
		public void TestObjectsNotEqualOtherType()
		{
			Assert.That(this.ValidObject1.Equals(new object()));
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

		[Test]
		public void TestLoadManifestWindowsStyle()
		{
			List<ManifestEntry> loadedEntries;
			using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(SampleManifestWindowsStyle)))
			{
				loadedEntries = ManifestHandler.LoadManifest(ms);
			}

			Assert.That(loadedEntries, Is.EquivalentTo(SampleManifestEntries));
		}

		[Test]
		public void TestLoadManifestUnixStyle()
		{
			List<ManifestEntry> loadedEntries;
			using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(SampleManifestUnixStyle)))
			{
				loadedEntries = ManifestHandler.LoadManifest(ms);
			}

			Assert.That(loadedEntries, Is.EquivalentTo(SampleManifestEntries));
		}
	}
}