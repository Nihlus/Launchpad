//
//  ExtensionMethods.cs
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
using System.Collections.Generic;

namespace Launchpad.Common
{
	/// <summary>
	/// Various extension methods.
	/// </summary>
	public static class ExtensionMethods
	{
		/// <summary>
		/// Sanitizes the input string, removing any \n, \r, or \0 characters.
		/// </summary>
		/// <param name="input">Input string.</param>
		/// <returns>The string, without the illegal characters.</returns>
		public static string RemoveLineSeparatorsAndNulls(this string input)
		{
			return input?.Replace("\n", string.Empty).Replace("\0", string.Empty).Replace("\r", string.Empty);
		}

		/// <summary>
		/// Adds a new value to an existing IDictionary, or if the dictionary already contains a value for the given key,
		/// updates the existing key with the new value.
		/// </summary>
		/// <param name="dictionary">The dictionary to update.</param>
		/// <param name="key">The key of the provided value.</param>
		/// <param name="value">The value to add or update.</param>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException(nameof(dictionary));
			}

			if (dictionary.ContainsKey(key))
			{
				dictionary[key] = value;
			}
			else
			{
				dictionary.Add(key, value);
			}
		}
	}
}
