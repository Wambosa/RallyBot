using System;
using System.Text;
using Tai.Extensions;
using Tai.UtilityBelt;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {
    internal static class CLIMethod {

        internal static void WriteStatusReportForAnIteration(TaiConfig config) {

            config["targetUser"] = config["targetUser"] ?? config["username"];
            config["projectId"] = config["projectId"] ?? ApiWrapper.GetProjectId(config["targetUser"]);
            config["iterationNumber"] = config["iterationNumber"] ?? ApiWrapper.GetIterationNumber(config["projectId"]);

            var team        = ApiWrapper.GetTeamMembers(config["projectId"]);
            var iterations  = ApiWrapper.GetIteration(config["projectId"], config["iterationNumber"]);
            var storys      = ApiWrapper.GetUserStories(config["projectId"], iterations);
            var tasks       = ApiWrapper.GetTasks(storys);
            storys          = AssignTaskMastersToStorys(storys, tasks, JArray.Parse(config["statusReportNames"]).ToArray());
            var emailBody   = GenerateMicrosoftHtmlTabularReport(storys, config);

            System.IO.File.WriteAllText(Grapple.GetThisFolder() + "email_body.txt", emailBody.ToString());

            Echo.IterationReport(team, iterations, storys, tasks);
        }

        internal static void SetStoryBuildId(TaiConfig config) {

            if(config.ContainsKey("storyId") && config.ContainsKey("buildId")) {
                
                Echo.Out("User story ID: " + config["storyId"], 5);
                Echo.Out("Build ID: " + config["buildId"], 5);

                string UserStoryURL = ApiWrapper.GetUserStoryRef(config["storyId"]);            

                JObject postJson = new JObject();
                postJson["c_BuildID"] = config["buildId"];

                ApiWrapper.UpdateStoryBuildId(postJson, UserStoryURL);
                Echo.Out("..........", 5);
            }else{
                Echo.Out("you must have both a storyId and buildId in order to perform action: SetStoryBuildId", 1);
            }
        }

        #region Savagery
        //todo: refactor this savagery. viewers dont h8
        internal static void AutomaticallySetTime_Alpha(TaiConfig config) {

            //required: must only autofill one week up to the target date (as to not appear suspicious under investigating)
            //required: support target date in order to backfill easily

            //1. get tasks relating to this.user current task list
            config["targetUser"]    = config["targetUser"] ?? config["username"];
            config["projectId"]     = config["projectId"] ?? ApiWrapper.GetProjectId(config["targetUser"]);
            config["burndownDate"]  = config["burndownDate"] ?? DateTime.Today.ToString("yyyy-MM-dd");

            var target_date = DateTime.Parse(config["burndownDate"]);
            var week_begin_date = target_date.AddDays(-(int)target_date.DayOfWeek);
            var goal_concept = string.Format("burn down hours from {0} to {1} as role: {2}", week_begin_date.ToString("MMMM dd yyyy"), target_date.ToString("MMMM dd yyyy"), "dev");
            
            Echo.Out(goal_concept, 2);


            var this_team   = ApiWrapper.GetTeamMembers(config["projectId"]);//erase this line
            var my_id       = ApiWrapper.GetSpecificTeamMemberObjectId(this_team, config["username"]); //todo: just get the id directly somehow
            var tasks       = ApiWrapper.GetTasks(my_id, week_begin_date);
            var time_items  = ApiWrapper.GetTimeEntryItemsForOneTeamMember(tasks, week_begin_date); //todo: maybe class AWeekAtWork{list<timeitems>, computed int[] dailyHours, computed totalHours, method set priorities (orders list by given name list)}
            
            int daily_min   = config.hoursPerDay != 0 ? config.hoursPerDay : 8;
            int[] day_hours = new int[] { 0, 0, 0, 0, 0, 0, 0 };

            var priority_chart      = new Dictionary<string, TimeEntryItem>();
            var priority_strings    = new string[]{"Administration", "Regression", "Iteration Planning", "Deployment Planning", "Environment Issue", "User Stories"};


            Echo.Out(my_id + " is my object_id", 5);
            Echo.Out(time_items.Count + " time entrys", 5);
            Echo.Out(". . . . . . . . . . .", 5);

            time_items = new List<TimeEntryItem>(); //force back down to zero for testing

            // if there are no time entry values, then we need to create some******

            if(time_items.Count == 0) {
                
                foreach(JToken task in tasks) { //need a way to figure out if a task has a time item or not! that is the key here.

                    // need to actually create a new time item so that i can get an id to pass to new time values
                    //Project + User + WeekStartDate + Task
                    // return string id of new timeentryitem db.CreateNewTimeEntryItem(string projectId, string userId, string taskId, DateTime weekStartDate)

                    var newId = ApiWrapper.CreateNewTimeEntryItem(config["projectId"], my_id, task.Value<string>("ObjectId"), week_begin_date);

                    newId = newId == string.Empty ? task.Value<string>("futuretimeitemid") : newId;

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

                Echo.Out("Story Name: "+ item.storyName, 4);
                Echo.Out("Task Name: " + item.taskName, 4);
                Echo.Out("Time Values:\n", 4);

                foreach(JToken time_value in item.timeEntryValues) {
                    //todo: create WeekTime object that takes in an array for adding to all the days at once
                    var t_val_date = Convert.ToDateTime(time_value["DateVal"].ToString());
                    day_hours[(int)t_val_date.DayOfWeek] += time_value.Value<int>("Hours");

                    Echo.Out("DateVal: "       + time_value.Value<string>("DateVal").GetPrettyDate(), 4);
                    Echo.Out("Hours: "         + time_value.Value<string>("Hours"), 4);
                    Echo.Out("Last Updated: "  + time_value.Value<string>("LastUpdated"), 4);
                    Echo.Out("----------", 4);
                }


                //needs to be after time calcs
                string priority_name = item.taskName.Contains(priority_strings, true);
                if(priority_name != string.Empty) {
                    if(!priority_chart.ContainsKey(priority_name)) {
                        priority_chart.Add(priority_name, item);
                    }
                }

                Echo.Out("==================================================\n\n\n", 4);
            }

            int required_hours = (int)target_date.DayOfWeek * daily_min; //this will not support sunday work. maybe support weekend autofilling as a bool switch next version. also allow config of the 8 hours to more or less

            Echo.Out("min hours needed to pass inspection: " + required_hours, 2);
            Echo.Out("You have " + day_hours.Sum() + " hours", 2); // if you do not match or exceed the min hours, then we will perform some autofilling

            //then add up the time for all days that week. if the total is less than sun-today * 8, then add new time entry values foreach day where total time < 8 (not in this method fyi)

            //consider while here while() TODO: instead of doing the submints in this loop. just generate a workload to perform later
            if (day_hours.Sum() < required_hours) {
                
                for(int i = 0; i<day_hours.Length; ++i) {

                    int this_days_time  = day_hours[i];
                    string day_name     = Enum.GetName(typeof(DayOfWeek), i);
                    int time_needed     = daily_min - this_days_time; //this allows for overages since we are adding in increments of 8. should only allow for a maximum of (inspection_amount + increment-1)
                    int week_total_hours= day_hours.Sum();//important to refresh each loop

                    if (week_total_hours < required_hours && time_needed > 0 /**/ && i!=0 && i !=6) { //hard code ignore of sunday and saturaday for now

                        Echo.Out(day_name + " FAILED inspection. Tai says, 'add some time motherfucker!'", 5);
                        
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
                                
                                Echo.Out(postJson.ToString(), 6);
                                Echo.Out("..........", 6);

                                ApiWrapper.PostNewTimeEntryValue(postJson); // store in workload array instead of executing in the loop
                                //if post was a success then add time to mem
                                day_hours[i] += (existing_hours+1);

                                time_needed -= (int)postJson["Hours"];
                                if(time_needed <= 0) {
                                    break;}                                

                            }
                        }

                    }else{
                        Echo.Out(day_name + " PASSED inspection");
                    }
                }

            }else {
                Echo.Out("Autofilling is not needed since you have already filled out the minimum necessary time");
            }
        }
        #endregion Savagery

        private static TaiConfig FixMissingEmailProperties(TaiConfig conf) {
            conf["emailGreeting"] = conf["emailGreeting"] ?? "Hi Boss";
            conf["emailSignature"] = conf["emailSignature"] ?? "-dev team";
            return conf;
        }

		private static string GetFarewell() {

			var farewell = new[]{
            "thinking of you",
            "your majesty",
            "with smugness",
            "smugly yours",
            "sincerely",
            "with great jubilation",
            "fearfully yours",
            "lurking behind you",
            "good day",
            "with regrets",
            "My Best",
            "My best to you",
            "Best",
            "All Best",
            "All the best",
            "Best Wishes",
            "Bests",
            "Best Regards",
            "Regards",
            "Warm Regards",
            "Warmest Regards",
            "Warmest",
            "Warmly",
            "Take care",
            "Many thanks",
            "Thanks for your consideration",
            "Hope this helps",
            "Looking forward",
            "Rushing",
            "In haste",
            "Be well",
            "Peace",
            "Yours Truly",
            "Yours",
            "Very Truly Yours",
            "Sincerely",
            "Sincerely Yours",
            "Cheers!"
            };

			return farewell[new Random().Next(0, farewell.Length)];
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

        private static StringBuilder GenerateMicrosoftHtmlTabularReport(List<JToken> storys, TaiConfig config) {

            config = FixMissingEmailProperties(config);

            var emailBody = new StringBuilder();

            var headerRow = string.Format("<tr><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{0}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{1}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{2}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{3}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{4}<o:p></o:p></p></td></tr>",
                "Person",
                "Story",
                "Finished On",
                "Status",
                "Risks"
                );

            emailBody.Append(string.Format("<p>{0},</p><br>Here is the latest status report.<br><br>", config["emailGreeting"]));
            emailBody.Append("<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 align=left style='border-collapse:collapse;border:none;margin-left:6.75pt;margin-right:6.75pt'>");
            emailBody.Append(headerRow);

            foreach (JToken story in storys) {

                var hero = story.Value<string>("HeroOfTime");
                var name = story.Value<string>("Name");
                var risk = story.Value<string>("BlockedReason");
                var status = story.Value<string>("ScheduleState");
                var date = story.Value<string>("AcceptedDate").GetPrettyDate();

                var dataRow = string.Format("<tr style='height:14.35pt'><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt;height:14.35pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{0}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border-top:none;border-left:none;border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;padding:0in 5.4pt 0in 5.4pt;height:14.35pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{1}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border-top:none;border-left:none;border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;padding:0in 5.4pt 0in 5.4pt;height:14.35pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{2}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border-top:none;border-left:none;border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;padding:0in 5.4pt 0in 5.4pt;height:14.35pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{3}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border-top:none;border-left:none;border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;padding:0in 5.4pt 0in 5.4pt;height:14.35pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{4}<o:p></o:p></p></td></tr>",
                    hero,
                    name,
                    date,
                    status,
                    risk
                    );

                emailBody.Append(dataRow);
            }
            emailBody.Append("</table>");

            var signature = string.Format("<br><br><p>{0},</p><br><p>-{1}</p>", GetFarewell(), config["emailSignature"]);

            emailBody.Append(signature);

            return emailBody;
        }

        private static StringBuilder GenerateGenerictHtmlTabularReport(List<JToken> storys, TaiConfig config) {

            config = FixMissingEmailProperties(config);

            var emailBody = new StringBuilder();

            var headerRow = string.Format("<tr><td><p>{0}</p></td> <td><p>{1}</p></td> <td><p>{2}</p></td> <td><p>{3}</p></td> <td><p>{4}</p></td></tr>",
                "Person",
                "Story",
                "Finished On",
                "Status",
                "Risks"
                );

            emailBody.Append(string.Format("<p>{0},</p><br>Here is the latest status report.<br><br>", config["emailGreeting"]));
            emailBody.Append("<table>");
            emailBody.Append(headerRow);

            foreach (JToken story in storys) {

                var hero = story.Value<string>("HeroOfTime");
                var name = story.Value<string>("Name");
                var risk = story.Value<string>("BlockedReason");
                var status = story.Value<string>("ScheduleState");
                var date = story.Value<string>("AcceptedDate").GetPrettyDate();

                var dataRow = string.Format("<tr><td><p>{0}</p></td> <td><p>{1}</p></td> <td><p>{2}</p></td> <td><p>{3}</p></td> <td><p>{4}</p></td></tr>",
                    hero,
                    name,
                    date,
                    status,
                    risk
                    );

                emailBody.Append(dataRow);
            }
            emailBody.Append("</table>");

            var signature = string.Format("<br><br><p>{0},</p><br><p>-{1}</p>", GetFarewell(), config["emailSignature"]);

            emailBody.Append(signature);

            return emailBody;
        }
    }
}
