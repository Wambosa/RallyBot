using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {

    public class TaiConfig : Dictionary<string, string> {

        public new string this[string column] {

            get {
                if (ContainsKey(column)) {
                    return base[column] == "" ? null : base[column];
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

            try{

                string raw = System.IO.File.ReadAllText(@fileLocation);

                if (raw.Length > 50) {//shallow validation

                    JObject json_config = JObject.Parse(raw);

                    foreach(var property in json_config) {
                        this[property.Key] = property.Value.ToString();}

                    hasPhysicalConfigFile = true;
                }
            
            }catch{/* i dont care if config is missing or messed up */}
        }

        public override string ToString() {
        
            var jObject = new JObject();

            foreach(KeyValuePair<string, string> pair in this){
                jObject[pair.Key] = pair.Value;}

            return jObject.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        private string ValidateConfigurationFileAtLocation() {
            
            //meah...
            return "error if file length too short or if a property is found that is not supported";
        }
    }
}
