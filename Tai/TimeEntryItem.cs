using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {

    public class TimeEntryItem {

        public JToken self;//todo: un-lame this

        public string storyName;

        public string taskName;

        public string timeEntryObjectId;

        public List<JToken> timeEntryValues;


        //i really hate this... unify these two into a getTimeValueForThisDate
        public string getTimeValueObjectIdForThisDate(DateTime date_to_match) {

            string found_id = string.Empty;

            foreach(JToken time_val in timeEntryValues) {
                
                if(date_to_match.Date == Convert.ToDateTime((string)time_val["DateVal"]).Date) {
                    found_id = (string)time_val["ObjectID"];
                    break;
                }
            }

            return found_id;
        }
        public int getTimeValueHoursForThisDate(DateTime date_to_match) {

            int hours = 0;

            foreach(JToken time_val in timeEntryValues) {
                
                if(date_to_match.Date == Convert.ToDateTime((string)time_val["DateVal"]).Date) {
                    hours = (int)time_val["Hours"];
                    break;
                }
            }

            return hours;
        }

    }
}
