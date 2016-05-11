using System;

namespace Launchpad.Launcher
{
	internal static class Utilities
	{
		/// <summary>
		/// Clean the specified input.
		/// </summary>
		/// <param name="input">Input.</param>
		public static string Clean(string input)
		{
			string output = "";

			output = input.Replace ("\n", String.Empty).Replace ("\0", String.Empty).Replace("\r", String.Empty);

			return output;
		}

		public static ESystemTarget ParseSystemTarget(string input)
		{
			ESystemTarget Target = ESystemTarget.Invalid;
			try
			{
				Target = (ESystemTarget)Enum.Parse (typeof(ESystemTarget), input);
			}
			catch (ArgumentNullException anex)
			{ 
				Console.WriteLine ("ArgumentNullException in ParseSystemTarget(): " + anex.Message);
			}
            catch (ArgumentException aex)
            {
                Console.WriteLine("ArgumentException in ParseSystemTarget(): " + aex.Message);
            }
            catch (OverflowException oex)
            {
                Console.WriteLine("OverflowException in ParseSystemTarget(): " + oex.Message);
            }

			return Target;
		}
	}
}

