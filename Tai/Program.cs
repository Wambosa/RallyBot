using System;
using System.Text;
using Mono.Options;
using Tai.UtilityBelt;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tai {

    class Program {

        private static OptionSet OPTIONS;
        private static string THIS_FOLDER;
        private static int VERBOSITY = 1; //todo: honor. use the cyclops log level for verbosity

        public static void Main(string[] args) {

            Console.WriteLine(GoodSamaritan.WelcomeText());

            foreach(string arg in args) {
                Console.WriteLine(arg);}

	        THIS_FOLDER			= Grapple.GetThisFolder();
            var config          = Grapple.LoadConfig(THIS_FOLDER + "tai.conf");

            var reportFlags     = string.Format("{0}|{1}", "iteration-report=", "report=");
            var burnFlags       = string.Format("{0}|{1}|{2}", "burndown-hours:", "burndown:", "burndown-on-date:");
            var docFlags        = string.Format("{0}|{1}", "designdoc=", "design-doc="); //todo: maybe this will just create a data struct that can be consumed by pmDeath
            var helpFlags       = string.Format("{0}|{1}|{2}", "?", "h", "help");
            var setBuildIdFlags = string.Format("{0}", "buildid=");
            var todayString     = DateTime.Today.ToString("yyyy-MM-dd");

	        OPTIONS = new OptionSet() {
				{ reportFlags, "Generate status report email", iter_num => WriteStatusReportForAnIteration(config, iter_num ?? "")},
				{ burnFlags, "Automatically 'Burn Down' hours", alternate_date => AutomaticallySetTime_Alpha(config, Convert.ToDateTime(alternate_date ?? todayString))},
				{ helpFlags, "Display detailed help.", _ => GoodSamaritan.HelpText()},
				{ setBuildIdFlags, "Set a new Build Id", buildId => SetStoryBuildId(config, buildId ?? "")},
				{"verbosity=", "giving Tai a martinee sure does make her chatty", level => {VERBOSITY = Convert.ToInt32(level); Console.WriteLine("log level set to "+ VERBOSITY);}}
			};

            var badInput = OPTIONS.Parse(args);

            if(badInput.Count > 0) {
                Console.WriteLine(Grapple.GetErrorReport(badInput.ToArray()));
                Console.WriteLine(GoodSamaritan.OffensiveGesture());
                GoodSamaritan.HelpText();
            }

            Console.WriteLine("done");
        }

        private static void WriteStatusReportForAnIteration(TaiConfig config, string iterationArg = "", Delegate emailBuilder = null) {

            var db          = new ApiWrapper(config);
            var iterNum    = iterationArg.Length > 0 ? iterationArg : Grapple.GetIterationNumberFromTerminal("What Iteration number do you want a report on ?");

            var team        = db.GetTeamMembers(config.projectId);
            var iterations  = db.GetIteration(config.projectId, iterNum);
            var storys      = db.GetUserStories(config.projectId, iterations);
            var tasks       = db.GetTasks(storys);
            storys          = AssignTaskMastersToStorys(storys, tasks, config.includeNames);
            var emailBody   = Reporter.GenerateMicrosoftHtmlTabularReport(storys, config);

            System.IO.File.WriteAllText(@THIS_FOLDER + "email_body.txt", emailBody.ToString());

            Console.WriteLine(Reporter.GenerateCliReport(team, iterations, storys, tasks));
        }



        private static List<JToken> AssignTaskMastersToStorys(List<JToken> storys, List<JToken> tasks, string[] includeNames) {

            var timeTracker = new Dictionary<string, Dictionary<string, int>>(); /* <story guid, <username, time>> */

            foreach(JToken task in tasks) {

                if(task["Owner"].Type == JTokenType.Null){
                    continue;}

                string user_name = task["Owner"].Value<string>("_refObjectName");

                if (!user_name.Contains(includeNames)) {
                    continue;}

                string story_guid = task["WorkProduct"].Value<string>("_refObjectUUID");

                if (!timeTracker.ContainsKey(story_guid)) {
                    timeTracker.Add(story_guid, new Dictionary<string, int>());}
    
                int estimate = task["Estimate"].Type != JTokenType.Null ? task.Value<int>("Estimate") : 0;

                if (timeTracker[story_guid].ContainsKey(user_name)){

                    timeTracker[story_guid][user_name] += estimate;
                }else {
                    timeTracker[story_guid].Add(user_name, estimate);
                }
            }

            var mutated_storys = new List<JToken>();
            foreach(JToken story in storys) {

                string story_guid = story.Value<string>("ObjectUUID");

                story["HeroOfTime"] = "DreamTeam";//todo:un-hardcode use the discovered team name
                story["HeroValue"] = 0;

                if(timeTracker.ContainsKey(story_guid)) {

                    foreach (KeyValuePair<string, int> dev_time in timeTracker[story_guid]) {

                        if (dev_time.Value > story.Value<int>("HeroValue")) {
                            story["HeroOfTime"] = dev_time.Key;
                            story["HeroValue"] = dev_time.Value;
                        }
                    }
                }

                mutated_storys.Add(story);
            }

            return mutated_storys;
        }

        private static void SetStoryBuildId(TaiConfig config, string buildId) {

            var buildid = buildId.Length > 0 ? buildId.Split(' ')[0] : Grapple.GetIterationNumberFromTerminal("What BuildId number do want to use ?");

            var userStoryId = buildId.Split(' ')[1];

            Console.WriteLine("User story ID: " + userStoryId);
            Console.WriteLine("Build ID: " + buildid);

            var db = new ApiWrapper(config);
            string UserStoryURL = db.GetUserStoryRef(userStoryId);

            JObject postJson = new JObject();
            postJson["c_BuildID"] = buildid;

            db.PostNewBuildId(postJson, UserStoryURL);
            Console.WriteLine("..........");
        }


		////							  _________.__                             
		////							 /   _____/|  |__    ____    ____    ______
		////							 \_____  \ |  |  \  /  _ \  /    \  /  ___/
		////							 /        \|   Y  \(  <_> )|   |  \ \___ \ 
		////							/_______  /|___|  / \____/ |___|  //____  >
		////									\/      \/              \/      \/ 
		//// ________________________________________________________________________________________________
		//// \____    / ____    ____    ____     ____ _/ ____\  /   _____/|  |__  _____     _____    ____  
		////   /     / /  _ \  /    \ _/ __ \   /  _ \\   __\   \_____  \ |  |  \ \__  \   /     \ _/ __ \ 
		////  /     /_(  <_> )|   |  \\  ___/  (  <_> )|  |     /        \|   Y  \ / __ \_|  Y Y  \\  ___/ 
		//// /_______ \\____/ |___|  / \___  >  \____/ |__|    /_______  /|___|  /(____  /|__|_|  / \___  >
		////	  	 \/            \/      \/                          \/      \/      \/       \/      \/
		////________________________________________________________________________________________________
        #region Savagery
        //todo: refactor this savagery. viewers dont h8
        private static void AutomaticallySetTime_Alpha(TaiConfig config, DateTime targetDate) {

            //required: must only autofill one week up to the target date (as to not appear suspicious under investigating)
            //required: support target date in order to backfill easily

            var week_begin_date = targetDate.AddDays(-(int)targetDate.DayOfWeek);
            var goal_concept    = string.Format("burn down hours from {0} to {1} as role: {2}", week_begin_date.ToString("MMMM dd yyyy"), targetDate.ToString("MMMM dd yyyy"), "dev");
            
            Console.WriteLine(goal_concept);

            //1. get tasks relating to this.user current task list
            var db          = new ApiWrapper(config);
            var this_team   = db.GetTeamMembers(config.projectId);
            var my_id       = db.GetSpecificTeamMemberObjectId(this_team, config.username);
            var tasks       = db.GetTasks(my_id, week_begin_date);
            var time_items  = db.GetTimeEntryItemsForOneTeamMember(tasks, week_begin_date); //todo: maybe class AWeekAtWork{list<timeitems>, computed int[] dailyHours, computed totalHours, method set priorities (orders list by given name list)}
            
            int daily_min   = config.hoursPerDay != 0 ? config.hoursPerDay : 8;
            int[] day_hours = new int[] { 0, 0, 0, 0, 0, 0, 0 };

            var priority_chart      = new Dictionary<string, TimeEntryItem>();
            var priority_strings    = new string[]{"Administration", "Regression", "Iteration Planning", "Deployment Planning", "Environment Issue", "User Stories"};


            Console.WriteLine(my_id             + " is my object_id");
            Console.WriteLine(time_items.Count  + " time entrys");
            Console.WriteLine("# # # # # # # # # # #");

            time_items = new List<TimeEntryItem>(); //force back down to zero for testing

            // if there are no time entry values, then we need to create some******

            if(time_items.Count == 0) {
                
                foreach(JToken task in tasks) { //need a way to figure out if a task has a time item or not! that is the key here.

                    // need to actually create a new time item so that i can get an id to pass to new time values
                    //Project + User + WeekStartDate + Task
                    // return string id of new timeentryitem db.CreateNewTimeEntryItem(string projectId, string userId, string taskId, DateTime weekStartDate)

                    var newId = db.CreateNewTimeEntryItem(config.projectId, my_id, task.Value<string>("ObjectId"), week_begin_date);

                    newId = newId == string.Empty ?  (string)task["futuretimeitemid"] : newId;

                    var ti = new TimeEntryItem() {
                        self = null,
                        storyName = task["WorkProduct"].Value<string>("_refObjectName"),
                        taskName = task.Value<string>("Name"),
                        timeEntryObjectId = newId, //will be the returned id value
                        timeEntryValues = new List<JToken>()
                    };

                    time_items.Add(ti);
                }
            }

            // this loop does 3 very different things. it fills the day_hours array, it establishes priorities, and generates a cli report
            foreach(TimeEntryItem item in time_items) {

                Console.WriteLine("Story Name: "+ item.storyName);
                Console.WriteLine("Task Name: " + item.taskName);
                Console.WriteLine("Time Values:\n");

                foreach(JToken time_value in item.timeEntryValues) {
                    //todo: create WeekTime object that takes in an array for adding to all the days at once
                    var t_val_date = Convert.ToDateTime(time_value["DateVal"].ToString());
                    day_hours[(int)t_val_date.DayOfWeek] += (int)time_value["Hours"];

                    Console.WriteLine("DateVal: "       + ((string)time_value["DateVal"]).GetPrettyDate());
                    Console.WriteLine("Hours: "         + time_value["Hours"]);
                    Console.WriteLine("Last Updated: "  + time_value["LastUpdated"]);
                    Console.WriteLine("----------");
                }


                //needs to be after time calcs
                string priority_name = item.taskName.Contains(priority_strings, true);
                if(priority_name != string.Empty) {
                    if(!priority_chart.ContainsKey(priority_name)) {
                        priority_chart.Add(priority_name, item);
                    }
                }

                Console.WriteLine("==================================================\n\n\n");
            }

            int required_hours = (int)targetDate.DayOfWeek * daily_min; //this will not support sunday work. maybe support weekend autofilling as a bool switch next version. also allow config of the 8 hours to more or less

            Console.WriteLine("min hours needed to pass inspection: " + required_hours);
            Console.WriteLine("You have " + day_hours.Sum() + " hours"); // if you do not match or exceed the min hours, then we will perform some autofilling

            //then add up the time for all days that week. if the total is less than sun-today * 8, then add new time entry values foreach day where total time < 8 (not in this method fyi)

            //consider while here while() TODO: instead of doing the submints in this loop. just generate a workload to perform later
            if (day_hours.Sum() < required_hours) {
                
                for(int i = 0; i<day_hours.Length; ++i) {

                    int this_days_time  = day_hours[i];
                    string day_name     = Enum.GetName(typeof(DayOfWeek), i);
                    int time_needed     = daily_min - this_days_time; //this allows for overages since we are adding in increments of 8. should only allow for a maximum of (inspection_amount + increment-1)
                    int week_total_hours= day_hours.Sum();//important to refresh each loop

                    if (week_total_hours < required_hours && time_needed > 0 /**/ && i!=0 && i !=6) { //hard code ignore of sunday and saturaday for now

                        Console.WriteLine(day_name + " FAILED inspection. Tai says, 'add some time motherfucker!'");
                        
                        while (time_needed > 0) {

                            //muaha! now create the new time items using the api
                            //remember we are looking into the payload manipulation via c#

                            foreach(string priority in priority_strings) { //since days only get iterated over once, this will save a max of 6 hours in single day. either iterate over the days while week_time_needed > 0 OR add more priorty strings. the prior is superior because if tere is at least 1 priority, then it will get filled out to the max instead of each priority only getting filled out then closing

                                var post_date = week_begin_date.AddDays(i);

                                string existing_object_id = priority_chart[priority].getTimeValueObjectIdForThisDate(post_date);
                                int existing_hours = priority_chart[priority].getTimeValueHoursForThisDate(post_date);

                                JObject postJson = new JObject();
                                postJson["Verb"] =  existing_object_id != string.Empty ? "update" : "insert";
                                postJson["ObjectID"] = existing_object_id;
                                postJson["DateVal"] = post_date.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                postJson["Hours"] = (existing_hours+1);
                                postJson["TimeEntryItem"] = priority_chart[priority].timeEntryObjectId;
                                
                                Console.WriteLine(postJson);
                                Console.WriteLine("..........");

                                db.PostNewTimeEntryValue(postJson); // store in workload array instead of executing in the loop
                                //if post was a success then add time to mem
                                day_hours[i] += (existing_hours+1);

                                time_needed -= (int)postJson["Hours"];
                                if(time_needed <= 0) {
                                    break;}                                

                            }
                        }

                    }else{
                        Console.WriteLine(day_name + " PASSED inspection");
                    }
                }

            }else {
                Console.WriteLine("Autofilling is not needed since you have already filled out the minimum necessary time");
            }
        }
        #endregion Savagery
    }
}