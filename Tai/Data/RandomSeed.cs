using System;

namespace Tai.Data{
	internal static class RandomSeed {

		// i want to be able to manipulate this random seed at will
		internal static int seed;

		static RandomSeed() {
			seed = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
		}
	}
}
