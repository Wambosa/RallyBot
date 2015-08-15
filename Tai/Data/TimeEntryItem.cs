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
    }
}
