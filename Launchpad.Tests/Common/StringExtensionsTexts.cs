//
//  StringExtensionsTexts.cs
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

using Launchpad.Common;
using Xunit;

namespace Launchpad.Tests.Common
{
	public class StringExtensionsTexts
	{
		public class RemoveLineSeparatorsAndNulls
		{
			private const string Expected = "data";
			private const string StringThatContainsNulls = "data\0\0";
			private const string StringThatContainsCarriageReturns = "data\r\r";
			private const string StringThatContainsLinefeeds = "data\n\n";
			private const string StringThatContainsEverything = "data\0\r\n";

			[Fact]
			public void DoesNotChangeStringThatDoesNotContainNullsCarriageReturnsOrLineFeeds()
			{
				Assert.Equal(Expected, Expected.RemoveLineSeparatorsAndNulls());
			}

			[Fact]
			public void RemovesNullCharacters()
			{
				Assert.Equal(Expected, StringThatContainsNulls.RemoveLineSeparatorsAndNulls());
			}

			[Fact]
			public void RemovesCarriageReturns()
			{
				Assert.Equal(Expected, StringThatContainsCarriageReturns.RemoveLineSeparatorsAndNulls());
			}

			[Fact]
			public void RemovesLineFeeds()
			{
				Assert.Equal(Expected, StringThatContainsLinefeeds.RemoveLineSeparatorsAndNulls());
			}

			[Fact]
			public void RemovesNullsCarriageReturnsAndLineFeeds()
			{
				Assert.Equal(Expected, StringThatContainsEverything.RemoveLineSeparatorsAndNulls());
			}
		}
	}
}