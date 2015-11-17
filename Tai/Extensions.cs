namespace Tai.Extensions {

    public static class CustomExtensions {

        public static string ToJson(this string[] arr){

            string joined = "[";

            for(int i=0;i<arr.Length;++i) {
                joined += '"'+arr[i]+'"'; 
                joined += i < arr.Length-1 ? ", " : "";
            }

            joined += "]";

            return joined;
        }

        public static string[] ToArray(this Newtonsoft.Json.Linq.JArray arr) {
            
            string[] builtIn = new string[arr.Count];

            for(int i=0; i<arr.Count; ++i) {
                builtIn[i] = (string)arr[i];}

            return builtIn;
        }

        public static int[] Combine(this int[] original, int[] newValues){

            int[] resulting = original;
        
            if(original.Length == newValues.Length){
                for(int i=0;i<original.Length;++i){
                    resulting[i] += newValues[i];
                }
            }
            
            return resulting;
        }

        public static int Sum(this int[] arr) {

            int total = 0;

            for (int i = 0; i < arr.Length; ++i ) {
                total += arr[i];}

            return total;
        }

        public static bool Contains(this string hayStack, string[] needles) {

            bool isFound = false;

            foreach (string needle in needles) {

                isFound = hayStack.ToLower().Contains(needle.ToLower());

                if (isFound){
                    break;}
            }

            return isFound;
        }

        public static string ContainsMatch(this string hayStack, string[] needles) {

            string firstWord    = string.Empty;
            bool isFound        = false;

            if (hayStack != null) {

                foreach (string needle in needles) {

                    isFound = hayStack.ToLower().Contains(needle.ToLower());

                    if (isFound){
                        firstWord = needle;
                        break;
                    }
                }
            }

            return firstWord;
        }

        public static string GetPrettyDate(this string uglyDate) {/*don't hate me ross*/

            var prettyDate = "";

            System.DateTime realDate = System.Convert.ToDateTime(uglyDate);
            //todo: lower min acceptable date for kicks. try 01/01/0002
            prettyDate = realDate > System.DateTime.Parse("01/01/1989") ? realDate.ToString("MMMM dd yyyy") : prettyDate;

            return prettyDate;
        }

		public static string GetRallyDateString(this System.DateTime someDate){
			return someDate.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
		}
    }
}