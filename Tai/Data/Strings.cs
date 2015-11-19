using System;

namespace Tai.Data {

	internal static class Strings {

		internal static string RandomFarewell {
			
			get {
				var farewell = new[] {
					"thinking of you",
					"your majesty",
					"with smugness",
					"smugly yours",
					"sincerely",
					"with great jubilation",
					"fearfully yours",
					"lurking behind you",
					"good day",
					"with regrets",
					"My Best",
					"My best to you",
					"Best",
					"All Best",
					"All the best",
					"Best Wishes",
					"Bests",
					"Best Regards",
					"Regards",
					"Warm Regards",
					"Warmest Regards",
					"Warmest",
					"Warmly",
					"Take care",
					"Many thanks",
					"Thanks for your consideration",
					"Hope this helps",
					"Looking forward",
					"Rushing",
					"In haste",
					"Be well",
					"Peace",
					"Yours Truly",
					"Yours",
					"Very Truly Yours",
					"Sincerely",
					"Sincerely Yours",
					"Cheers!"
				};

				return farewell[new Random(RandomSeed.seed).Next(0, farewell.Length)];
			}
		}

		internal static string RandomAnalysisTerm {
			
			get {
				var term = new[] {
					"consider how",
					"investigate way",
					"figure out how",
					"study",
					"analyze"
				};

				return term[new Random(RandomSeed.seed).Next(0, term.Length)];
			}
		}

		internal static string RandomBuildTerm {
			
			get {
				var term = new[] {
					"build a way",
					"code out",
					"write code",
					"draft script",
					"create class"
				};

				return term[new Random(RandomSeed.seed).Next(0, term.Length)];
			}
		}

		internal static string RandomTestTerm {
			
			get {
				var term = new[] {
					"check on progress",
					"evaluate code",
					"examine scripts",
					"inspect feature",
					"experiment"
				};

				return term[new Random(RandomSeed.seed).Next(0, term.Length)];
			}
		}
	}
}
