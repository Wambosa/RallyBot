using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {

    public class TimeEntryItem {

        public JToken json  {get; set;}

        public DateTime weekStartDate {get; set;}

        public string storyName {get; set;}

        public string taskName  {get; set;}

        public string timeEntryObjectId {get; set;}

        public List<JToken> timeEntryValues {get; set;}//erase

        public Dictionary<DateTime, JToken> dailyTime {get; set;} //todo: create a specific object for time values

        public int[] GetWeekHoursTotal() {
            
            int[] total = new int[]{0,0,0,0,0,0,0};
        
            foreach(KeyValuePair<DateTime, JToken> entry in dailyTime) {
                JToken day = entry.Value;
                DateTime t_val_date = day.Value<DateTime>("DateVal").Date;
                total[(int)t_val_date.DayOfWeek] += day.Value<int>("Hours");
            }
            return total;
        }

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
