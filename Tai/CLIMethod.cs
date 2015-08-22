using System;
using System.Text;
using Tai.Extensions;
using Tai.UtilityBelt;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai {
    internal static class CLIMethod {

        internal static void WriteStatusReportForAnIteration(TaiConfig config) {

            config = SetRequiredProperties(config, "targetUser", "projectId", "iterationNumber");

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
                Echo.Out("..........", 5);

                string UserStoryURL = ApiWrapper.GetUserStoryReferenceUrl(config["storyId"]);

                JObject postJson = new JObject();
                postJson["c_BuildID"] = config["buildId"];

                ApiWrapper.UpdateStoryBuildId(postJson, UserStoryURL);
                Echo.Out("success", 1);
            }else{
                Echo.Out("you must have both a storyId and buildId in order to perform action: SetStoryBuildId", 1);
            }
        }

        internal static void AutomaticallyFillTaskTime(TaiConfig config) {

            config = SetRequiredProperties(config, "targetUser", "projectId", "burndownDate", "hoursPerDay", "taskNames");

            var target_date = DateTime.Parse(config["burndownDate"]);
            var week_begin_date = target_date.AddDays(-(int)target_date.DayOfWeek);
            
            var my_id = ApiWrapper.GetTargetUserObjectId(config["targetUser"]);
            var tasks = ApiWrapper.GetTasks(my_id, week_begin_date);

            CreateEmptyTimeCardForTasks(tasks, config);

            int daily_min = Convert.ToInt32(config["hoursPerDay"]);
            int[] total_hours = SumTaskHoursByWeekStart(tasks, week_begin_date);
            var priority_chart = BuildTaskPriorityChart(tasks, config);
            int required_hours_this_week = (int)target_date.DayOfWeek * daily_min;

            Echo.Out(string.Format("burn down hours from {0} to {1} as role: {2}", week_begin_date.ToString("MMMM dd yyyy"), target_date.ToString("MMMM dd yyyy"), "dev"), 2);
            Echo.Out(my_id + " is my ObjectID", 5);
            Echo.Out(tasks.Count + " tasks since "+week_begin_date.ToString("yyyy MM dd"), 5);
            Echo.Out(". . . . . . . . . . .", 5);
            Echo.TaskReport(tasks, week_begin_date);
            Echo.Out("min hours needed to pass inspection: " + required_hours_this_week, 2);
            Echo.Out("You have " + total_hours.Sum() + " hours", 2);

            if (total_hours.Sum() < required_hours_this_week) {
                
                var workload = new List<JObject>();

                for(int i = 0; i<total_hours.Length; ++i) {

                    int this_days_time      = total_hours[i];
                    string day_name         = Enum.GetName(typeof(DayOfWeek), i);
                    int daily_time_needed   = daily_min - this_days_time; //this allows for overages since we are adding in increments of 8. should only allow for a maximum of (inspection_amount + increment-1)
                    
                    if(total_hours.Sum() < required_hours_this_week && daily_time_needed > 0 /**/ && i!=0 && i !=6) { //hard code ignore of sunday and saturaday for now

                        Echo.Out(day_name + " FAILED inspection.", 2);                        

                        int hoursToAdd = (int)Math.Ceiling(((double)daily_time_needed / (double)priority_chart.Count));

                        foreach(KeyValuePair<string, Task> priority in priority_chart) {

                            var task = priority.Value;

                            DateTime post_date = week_begin_date.AddDays(i);

                            JObject newTimeEntryValuePost = new JObject();

                            if(task.weeklyTime[week_begin_date].dailyTime.ContainsKey(post_date)) {
                                    
                                newTimeEntryValuePost["Verb"] = "update";
                                newTimeEntryValuePost["ObjectID"] = task.weeklyTime[week_begin_date].dailyTime[post_date].Value<string>("ObjectID");
                                newTimeEntryValuePost["Hours"] = task.weeklyTime[week_begin_date].dailyTime[post_date].Value<int>("Hours") + hoursToAdd;
                            }else{
                                newTimeEntryValuePost["Verb"] = "insert";
                                newTimeEntryValuePost["DateVal"] = post_date.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                newTimeEntryValuePost["TimeEntryItem"] = task.weeklyTime[week_begin_date].timeEntryObjectId;
                                newTimeEntryValuePost["Hours"] = hoursToAdd;
                            }

                            Echo.Out(newTimeEntryValuePost.ToString(), 6);
                            Echo.Out("..........", 6);

                            workload.Add(newTimeEntryValuePost);
                            total_hours[i] += hoursToAdd;
                            daily_time_needed -= hoursToAdd;
                            if(daily_time_needed <= 0){break;}
                        }

                    }else{
                        Echo.Out(day_name + " PASSED inspection", 2);
                    }
                }

                ApiWrapper.SubmitTaskTimeValue(workload);

            }else {
                Echo.Out("Autofilling is not needed since you have already filled out the minimum necessary time", 2);
            }
        }

        internal static void CreateTaskForStory(TaiConfig config){
            
            config = SetRequiredProperties(config, "targetUser", "storyId", "taskNames", "estimateHours", "taskState");

            JObject newTask = new JObject();
            newTask["Name"] = config["taskName"];//just create one for now. later iterate over these and create one for each name
            newTask["Description"] = config["description"] ?? "";
            newTask["Notes"] = config["notes"] ?? "";
            newTask["Owner"] = ApiWrapper.GetTargetUserObjectId(config["targetUser"]);
            newTask["Estimate"] = config["estimateHours"];
            newTask["State"] = config["taskState"];
            newTask["TaskIndex"] = 1;
            newTask["WorkProduct"] = ApiWrapper.GetUserStory(config["storyId"]).Value<string>("ObjectID");

            Echo.Out(newTask.ToString(Newtonsoft.Json.Formatting.Indented), 1);
            Echo.Out(ApiWrapper.CreateNewTask(newTask).ToString(Newtonsoft.Json.Formatting.None), 5);
        }

        internal static void GetCurrentIterationNumber(TaiConfig config){
            
            config = SetRequiredProperties(config, "targetUser");

            var project_id = ApiWrapper.GetProjectId(config["targetUser"]);

            Echo.Out(ApiWrapper.GetIterationNumber(project_id).Trim(), 1);
        }

        internal static void GetIterationFormattedStoryIdsByTeam(TaiConfig config){

            config = SetRequiredProperties(config, "targetUser", "projectId", "iterationNumber");

            var iterations  = ApiWrapper.GetIteration(config["projectId"], config["iterationNumber"]);
            var storys      = ApiWrapper.GetUserStories(config["projectId"], iterations);            

            var sb = new StringBuilder();
            foreach(JToken story in storys) {
                sb.AppendFormat("{0},", story.Value<string>("FormattedID"));}

            Echo.Out(sb.ToString().Substring(0, sb.Length-1), 1);
        }

        internal static void GetStorysForUser(TaiConfig config){
            
            config = SetRequiredProperties(config, "targetUser", "projectId", "humanName");

            var iterations  = ApiWrapper.GetIteration(config["projectId"]);
            var storys      = ApiWrapper.GetUserStories(config["projectId"], iterations);
            var tasks       = ApiWrapper.GetTasks(storys);
            storys          = AssignTaskMastersToStorys(storys, tasks, new string[]{config["humanName"]});
            var sorted      = GetStorysSortedByProgrammer(storys);

            if(sorted.ContainsKey(config["humanName"])){

                var sb = new StringBuilder();
                var count = 0;

                foreach(JToken story in sorted[config["humanName"]]){
                    count ++;
                    sb.AppendFormat(@"
{0}:            {1}
justification:  {2}
criteria:       {3}
expert(s):      {4}
link:           {5}
                    ", story.Value<string>("FormattedID"),
                    story.Value<string>("Name"),
                    story.Value<string>("c_Benefit"),
                    story.Value<string>("c_AcceptanceCriteria"),
                    story.Value<string>("c_ResponsibleParty"),
                    story.Value<string>("_ref"));
                }
 
                var header = string.Format("{0} has {1} stories assigned. \n", config["humanName"], count);
                Echo.Out(header + sb.ToString(), 1);
            }
        }

        #region Private Helpers
        private static TaiConfig SetRequiredProperties(TaiConfig conf, params string[] requiredProperties) {
            /* only some properties can be safely defaulted. this section belongs to those properties can be safely assumed */
            var defaults = new Dictionary<string, Func<string, string>> () {
                {"targetUser", val => { return val ?? conf["username"];}},
                {"storyId", val => { return val ?? "US00000";}}, //todo: get most recent story by latest task update/modified
                {"taskName", val => { return val ?? "new task";}},
                {"estimateHours", val => { return val ?? "10";}},
                {"taskState", val => { return val ?? "Defined";}},
                {"projectId", val  => {return val ?? ApiWrapper.GetProjectId(conf["targetUser"]);}},
                {"iterationNumber", val => {return val ?? ApiWrapper.GetIterationNumber(conf["projectId"]);}},
                {"burndownDate", val => {return val ?? DateTime.Today.ToString("yyyy-MM-dd");}},
                {"hoursPerDay", val => {return val ?? "8";}},
                {"taskNames", val => {return val ?? "Administration,Regression,Iteration Planning,Deployment Planning,Environment Issue,User Stories".Split(',').ToJson();}},
                {"emailGreeting", val => {return val ?? "Hi Boss";}},
                {"emailSignature", val => {return val ?? "dev team";}},
                {"humanName", val => {return val ?? ApiWrapper.GetTargetUserHumanName(conf["targetUser"]);}},
            };
        
            foreach(string property in requiredProperties){
                conf[property] = defaults[property](conf[property]);}

            return conf;
        }

        private static void CreateEmptyTimeCardForTasks(List<Task> tasks, TaiConfig config){

            var target_date = DateTime.Parse(config["burndownDate"]);
            var week_begin_date = target_date.AddDays(-(int)target_date.DayOfWeek);
            var my_id = ApiWrapper.GetTargetUserObjectId(config["targetUser"]);

            foreach(Task task in tasks) {

                if(!task.weeklyTime.ContainsKey(week_begin_date.Date)) {
                    /* this can and will create an invisible time item for tasks that don't belong to this week. no damage will be done */
                    task.weeklyTime.Add(week_begin_date.Date, ApiWrapper.CreateNewTimeEntryItem(config["projectId"], my_id, task.taskObjectId, week_begin_date));
                }
            }
        }

        private static int[] SumTaskHoursByWeekStart(List<Task> tasks, DateTime weekStart){
            
            int[] total_hours = new int[]{0,0,0,0,0,0,0};
            
            foreach(Task task in tasks) {
                total_hours = total_hours.Combine(task.GetWeekHoursTotal(weekStart));}

            return total_hours;
        }

        private static Dictionary<string, Task> BuildTaskPriorityChart(List<Task> tasks, TaiConfig config){

            string[] priority_strings = JArray.Parse(config["taskNames"]).ToArray();
            var priority_chart = new Dictionary<string, Task>();

            foreach(Task task in tasks) {

                string priority_name = task.taskName.ContainsMatch(priority_strings);

                if(priority_name != string.Empty) {
                    if(!priority_chart.ContainsKey(priority_name)) {
                        priority_chart.Add(priority_name, task);
                    }
                }
            }

            return priority_chart;
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

                try{
                    if (!user_name.Contains(includeNames)) {
                        continue;}
                }catch{
                    continue;
                }


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

        private static Dictionary<string, List<JToken>> GetStorysSortedByProgrammer(List<JToken> mutatedStorys) {

            var sortedStories = new Dictionary<string, List<JToken>>();

            foreach(var story in mutatedStorys){

                if(sortedStories.ContainsKey(story.Value<string>("HeroOfTime"))){
                    sortedStories[story.Value<string>("HeroOfTime")].Add(story);
                }else{
                    sortedStories.Add(story.Value<string>("HeroOfTime"), new List<JToken>(){{story}});}
            }

            return sortedStories;
        }

        private static StringBuilder GenerateMicrosoftHtmlTabularReport(List<JToken> storys, TaiConfig config) {

            config = SetRequiredProperties(config, "emailGreeting", "emailSignature");

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

            config = SetRequiredProperties(config, "emailGreeting", "emailSignature");

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
        #endregion Private Helpers
    }
}
