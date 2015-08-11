using Newtonsoft.Json.Linq;
using System.Collections.Generic;


namespace Tai {

    public class TaiConfig {

        public bool isValidConfiguration;

        public string projectId;

        public string username;

        public string password;

        #region report specific

        public string emailGreeting;

        public string emailSignature;

        public string apiUrl;

        public string[] includeNames; //change to includeInReportNames
        #endregion

        #region burndown specific

        //todo: implement these

        public bool isWeekendWorkaholic = false;

        public int hoursPerDay = 0;

        public string[] myTaskPrioritieNames = new string[]{};
        #endregion

        public TaiConfig(string fileLocation) {

            string raw = System.IO.File.ReadAllText(@fileLocation);

            if (raw.Length > 50) {//shallow validation

                JToken json_config = JToken.Parse(raw);

                List<string> devs = new List<string>();

                foreach (JToken devname in json_config["includeNames"]) {
                    devs.Add((string)devname);}

                apiUrl = (string)json_config["apiUrl"];
                projectId = (string)json_config["projectId"];
                username = (string)json_config["username"];
                password = (string)json_config["password"];

                emailGreeting = (string)json_config["emailGreeting"];
                emailSignature = (string)json_config["emailSignature"];
                includeNames = devs.ToArray();
                isValidConfiguration = true;

            }else{
                //todo: this will trigger a conf file recreation and questioning to repopulate
                isValidConfiguration = false;
            }
        }

        public override string ToString() {
        
            //convert this entire thing into the conf file format i know and love
            return "todo";
        }

        private string ValidateConfigurationFileAtLocation() {
            
            
            return "error if file length too short or if a property is found that is not supported";
        }
    }
}
