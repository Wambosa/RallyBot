using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

		internal static TaiConfig LoadConfig(string fileLocation = "tai.conf")
		{

			//todo: check if the file exists, if it does not, then create one
			//todo: encode the password or some other method. ask ross (if there is a plaintext password then erase it and write the back the encoded version as a different serialized property name)

			TaiConfig conf = new TaiConfig(@fileLocation);

			if (!conf.isValidConfiguration)
			{

				conf.username = GetUsernameFromTerminal();
				conf.password = GetPasswordFromTerminal();
				conf.emailGreeting = "Hey Boss";
				conf.emailSignature = "dev team";
				conf.includeNames = GetTeamMemberFirstNamesFromTerminal();
				conf.projectId = GetProjectIdFromTerminal();
				SaveNewConfig(@fileLocation, conf);
			}

			return conf;
		}

		/// <summary>
		/// TODO: Probably wont live here
		/// </summary>
		/// <param name="newFileLocation"></param>
		/// <param name="config"></param>
		/// <returns></returns>
		private static bool SaveNewConfig(string newFileLocation, TaiConfig config)
		{

			//delete any potential existing file at location
			//create the new file and save the serialized version into it config.ToString() override 
			// if there is a failure, just tell the user. dont try to solve any issues yet

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		internal static string GetAnyFileContentsAnyWhere()
		{
			// Do meaningful ninja work
			var contents = "";
			return contents;
		}

		private static string GetUsernameFromTerminal()
		{
			//todo: validate email format regex ? /w.*\@/w.*\.com
			Console.Write("enter username\nex: jbond@gmail.com\n\nyour username: ");
			return Console.ReadLine();
		}

		private static string GetPasswordFromTerminal()
		{
			//TODO: actually hide password
			Console.Write("enter password\n\nyour hidden password: ");
			return Console.ReadLine();
		}

		private static string[] GetTeamMemberFirstNamesFromTerminal()
		{

			Console.Write("enter first name(s) of your dev team members seperated by a single space \nex: ross derrick antonio \n\nfirst names: ");
			return Console.ReadLine().Split(' ');
		}

		internal static string GetIterationNumberFromTerminal(string someQuestion)
		{
			//todo: input validation
			Console.WriteLine("\n" + someQuestion + "\n");
			return Console.ReadLine();
		}


		/// <summary>
		/// TODO: DESTROY THIS. instead just get the project id from the users most recent task? 
		/// </summary>
		/// <returns></returns>
		private static string GetProjectIdFromTerminal()
		{
			//todo: simply ask the user for team name and try to get the id for them automatically
			Console.Write(
					@"Your ProjectId is associated with your specific team;
					In order to get your ProjectId:

						1. goto https://rally1.rallydev.com/
						2. login using your credentials
						3. In the top left corner, be sure that your team name is selected
						4. Once you have selected your team name, the ProjectId will be located in the url after the hashtag(#) symbol
						5. be sure to exclude the last letter 'd'

					What is your ProjectId? 
					");

			return Console.ReadLine();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="badInput"></param>
		/// <returns></returns>
		internal static StringBuilder GetErrorReport(string[] badInput)
		{
			var errReport = new StringBuilder();
			foreach (var misunderstoodWord in badInput)
				errReport.AppendLine("No switch available for: " + misunderstoodWord);
			
			return errReport;
		}
	}
}
