using System;
using System.Text;

namespace Tai.UtilityBelt
{
	internal static class Grapple
	{
        // if you need to go get something use grable to do the IO
		// Graple does exactly that, it extends and hangs on to stuff in the tai namespace and runtime
		// Life configurations, resource files, command line input

		////								_ 1      1 __        _.xxxxxx.
		////				 [xxxxxxxxxxxxxx|##|xxxxxxxx|##|xxxxxxXXXXXXXXX|
		//// ____            [XXXXXXXXXXXXXXXXXXXXX/.\||||||XXXXXXXXXXXXXXX|
		////|::: `-------.-.__[=========---___/::::|::::::|::::||X O^XXXXXX|
		////|::::::::::::|2|%%%%%%%%%%%%\::::::::::|::::::|::::||X /
		////|::::,-------|_|~~~~~~~~~~~~~`---=====-------------':||  5
		//// ~~~~                       |===|:::::|::::::::|::====:\O
		////							|===|:::::|:.----.:|:||::||:|
		////							|=3=|::4::`'::::::`':||__||:|
		////							|===|:::::::/  ))\:::`----':/
		////BlasTech Industries'        `===|::::::|  // //~`######b
		////DL-44 Heavy Blaster Pistol      \`--------=====/  ######B
		////												  `######b
		////1 .......... Sight Adjustments                    #######b
		////2 ............... Stun Setting                    #######B
		////3 ........... Air Cooling Vent                    `#######b
		////4 ................. Power Pack                     #######P
		////5 ... Power Pack Release Lever             LS      `#####B
		
		// TODO: Create formatted empty reports, pull them in with graple - 
		// TODO: pass them off to something else to build the report and return it

		/// <summary>
		/// well....we're going to get a folder, this one to be sure
		/// </summary>
		/// <returns></returns>
		internal static string GetSolutionFolder()
		{
			// The config path is currently three directories back
			string microsoftPsychopath = "";
			foreach (var bit in System.Reflection.Assembly.GetEntryAssembly().Location.Split('\\'))
			{
				microsoftPsychopath += bit + '\\';
				if (bit.Equals("tai"))
				{
					break;
				}
			}
			return microsoftPsychopath;
		}

        internal static string GetThisFolder()
        {
            var path_bits = System.Reflection.Assembly.GetEntryAssembly().Location.Split('\\');

            string simple_path_i_have_to_build_because_you_suck_microsoft = "";

            for(int i=0; i<(path_bits.Length-1); ++i) 
            {
                simple_path_i_have_to_build_because_you_suck_microsoft += path_bits[i]+'\\';
            }

            return simple_path_i_have_to_build_because_you_suck_microsoft;
        }

		internal static string GetAnyFileContentsAnyWhere()
		{
			// Do meaningful ninja work
			var contents = "";
			return contents;
		}

		internal static string GetUsernameFromTerminal()
		{
			//todo: validate email format regex ? /w.*\@/w.*\.com
			Console.Write("enter username\nex: jbond@gmail.com\n\nyour username: ");
			return Console.ReadLine();
		}

		internal static string GetPasswordFromTerminal()
		{
			//TODO: actually hide password
			Console.Write("enter password\n\nyour hidden password: ");
			return Console.ReadLine();
		}

		internal static string[] GetTeamMemberFirstNamesFromTerminal()
		{

			Console.Write("enter first name(s) of your dev team members seperated by a single space \nex: ross derrick antonio \n\nfirst names: ");
			return Console.ReadLine().Split(' ');
		}
	}
}
