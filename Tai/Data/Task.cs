/*
A 'TASK' can have multiple 'TIME ITEMS' and each 'time item' can have multiple 'TIME VALUES'

TASK        = is just that, a task that we create in rally
TIME ITEM   = represents a 7 day period in rally (just like in the timesheet)
TIME VALUE  = is a single day and its hour value within the 'time item'
*/

using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {
    public class Task {

        public JToken json;

        public string taskObjectId {get; set;}

        public string storyName {get; set;}

        public string taskName  {get; set;}

        /* the key will be the week beginning of a particular time item */
        public Dictionary<DateTime, TimeEntryItem> weeklyTime {get; set;} //create an extension that gives me the sum time or just a method in task actually
    
        public int[] GetWeekHoursTotal(DateTime weekStartDate){
            
            int[] total = new int[]{0,0,0,0,0,0,0};

            if(weeklyTime.ContainsKey(weekStartDate.Date)){
                total = weeklyTime[weekStartDate.Date].GetWeekHoursTotal();}

            return total;
        }
    }
}
