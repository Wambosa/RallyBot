using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {

    public class TaiConfig : Dictionary<string, string> {

        public new string this[string column] {

            get {
                if (ContainsKey(column)) {//todo: need to check for empty strings and treat as null
                    return base[column];
                } else {
                    return null;}
            }

            set {
                if (ContainsKey(column)) {
                    base[column] = value;
                } else {
                    Add(column, value);}
            }
        }

        public bool hasPhysicalConfigFile {get; protected set;}

        #region prototype todo reminder
        public bool isWeekendWorkaholic = false;

        public int hoursPerDay = 0;

        public string[] myTaskPrioritieNames = new string[]{};
        #endregion

        public TaiConfig(string fileLocation) {

            string raw = System.IO.File.ReadAllText(@fileLocation);

            if (raw.Length > 50) {//shallow validation

                JObject json_config = JObject.Parse(raw);

                foreach(var property in json_config) {
                    this[property.Key] = property.Value.ToString();}

                hasPhysicalConfigFile = true;
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
