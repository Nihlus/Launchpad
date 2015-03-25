using System;

namespace Launchpad_Launcher
{
	public static class Utilities
	{
		/// <summary>
		/// Clean the specified input.
		/// </summary>
		/// <param name="input">Input.</param>
		public static string Clean(string input)
		{
			string output = "";

			output = input.Replace ("\n", String.Empty).Replace ("\0", String.Empty);

			return output;
		}

		public static ESystemTarget ParseSystemTarget(string input)
		{
			ESystemTarget Target = ESystemTarget.Invalid;
			try
			{
				Target = (ESystemTarget)Enum.Parse (typeof(ESystemTarget), input);
			}
			catch (Exception ex)
			{ 
				Console.WriteLine (ex.Message);
			}

			return Target;
		}
	}
}

