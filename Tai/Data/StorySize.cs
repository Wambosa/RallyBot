using System;
using System.Collections.Generic;

namespace Tai.Data {
	internal static class StorySize {

		internal static Dictionary<int, string> digitToLetter = new Dictionary<int, string>(){
			{1, "XS"},
			{2, "S"},
			{3, "M"},
			{4, "M"},
			{5, "L"},
			{6, "L"},
			{7, "L"},
			{8, "XL"},
			{9, "XL"},
			{10, "XL"},
		};

		internal static Dictionary<string, int> preferredEstimates = new Dictionary<string,int>(){
			{"XS", 6},
			{"S", 12},
			{"M", 30},
			{"L", 54},
			{"XL", 54},
		};

		internal static Dictionary<string, int> randomEstimates = new Dictionary<string,int>(){
			{"XS", new Random(RandomSeed.seed).Next(1, 11)},
			{"S", new Random(RandomSeed.seed).Next(11, 21)},
			{"M", new Random(RandomSeed.seed).Next(21, 46)},
			{"L", new Random(RandomSeed.seed).Next(46, 76)},
			{"XL", new Random(RandomSeed.seed).Next(76, 106)},
		};
	}
}
