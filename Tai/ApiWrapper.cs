using System;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tai {

    public static class ApiWrapper {

        private static string BASE_URL = "https://rally1.rallydev.com/slm/webservice/v2.0/";
        private static CachedAuthentication CACHED_AUTH;

        public static bool Initialize(TaiConfig config) {

            BASE_URL = config["apiUrl"] ?? BASE_URL;

            try{
                CACHED_AUTH = GetCachedAuthentication(config["username"], config["password"]);

            }catch{
                Console.WriteLine("No Internet Connection");//meah
                Environment.Exit(0);
            }

            return true;
        }

        private static CredentialCache MakeCredentials(Uri aUri) {
            var credentials = new CredentialCache();
            credentials.Add(aUri, "Basic", new NetworkCredential(CACHED_AUTH.username, CACHED_AUTH.password));
            return credentials;
        }

        private static CredentialCache MakeCredentials(Uri aUri, string username, string password) {
            var credentials = new CredentialCache();
            credentials.Add(aUri, "Basic", new NetworkCredential(username, password));
            return credentials;
        }

        private static CachedAuthentication GetCachedAuthentication(string username, string password) {
            /*
            manual example (like in web browser)
            "https://username@gmail.com:password@rally1.rallydev.com/slm/webservice/v2.0/security/authorize"
            */
           
            var uri         = new Uri(BASE_URL + "security/authorize");
            var newAuth     = HttpService.GetCachedAuthentication(uri, MakeCredentials(uri, username, password));
            var jobject     = JObject.Parse(newAuth.rawAuthResponse);
            newAuth.token   = jobject["OperationResult"].Value<string>("SecurityToken");
            newAuth.username= username;
            newAuth.password= password;

            return newAuth;
        }

        private static JToken GetJsonObject(string url) {

            var uri = new Uri(url);

            return JToken.Parse(HttpService.GetRawJson(uri, MakeCredentials(uri)));
        }

        public static string GetProjectId(string userName) {
            // assumes the team you are on based on recent activity
            // https:// rally1.rallydev.com/slm/webservice/v2.0/task?query=(Owner.UserName = name@gmail.com)&fetch=true&order=CreationDate desc

            var query       = string.Format("task?query=(Owner.UserName = {0})&fetch=true&order=CreationDate desc", userName);
            var json        = GetJsonObject(BASE_URL + query);
            var iterations  = new List<JToken>();

            var project_url = json["QueryResult"]["Results"].First["Project"].Value<string>("_ref");
            var split = project_url.Split('/');

            return split[split.Length-1];
        }

        public static string GetIterationNumber(string projectId) {
            // assumes the latest iteration
            // https:// rally1.rallydev.com/slm/webservice/v2.0/Project/15188699182/Iterations?order=EndDate&pagesize=200

            var query       = string.Format("Project/{0}/Iterations?order=EndDate desc&pagesize=1", projectId);
            var json        = GetJsonObject(BASE_URL + query);
            var iterations  = new List<JToken>();

            var most_recent = json["QueryResult"]["Results"].First;
            var assumed_iteration_num = Regex.Match(most_recent.Value<string>("Name"), @"\s\d\d").Value;

            return assumed_iteration_num;
        }

        public static List<JToken> GetIteration(string projectId) {
            // assumes the latest iteration
            // https:// rally1.rallydev.com/slm/webservice/v2.0/Project/15188699182/Iterations?order=EndDate&pagesize=200

            var query       = string.Format("Project/{0}/Iterations?order=EndDate desc&pagesize=1", projectId);
            var json        = GetJsonObject(BASE_URL + query);
            var iterations  = new List<JToken>();

            var most_recent = json["QueryResult"]["Results"].First;
            var assumed_iteration_num = Regex.Match(most_recent.Value<string>("Name"), @"\s\d\d").Value;

            return GetIteration(projectId, assumed_iteration_num);
        }

        public static List<JToken> GetIteration(string projectId, string iterationNum) {

            //https:// rally1.rallydev.com/slm/webservice/v2.0/Project/15188699182/Iterations?query=(Name%20contains%20%2292%22)

            var query       = string.Format("Project/{0}/Iterations?query=(Name contains {1})", projectId, iterationNum);
            var json        = GetJsonObject(BASE_URL + query);
            var iterations  = new List<JToken>();

            iterations.AddRange(json["QueryResult"]["Results"]);
            return iterations;
        }

        public static List<JToken> GetTeamMembers(string projectId) {

            var people  = new List<JToken>();
            var query   = string.Format("Project/{0}/TeamMembers", projectId);
            var json    = GetJsonObject(BASE_URL + query);

            people.AddRange(json["QueryResult"]["Results"]);
            return people;
        }

        public static JToken GetUserStory(string formattedId) {

            var query = string.Format("HierarchicalRequirement?query=(FormattedID = {0})&fetch=true", formattedId);
            var jsonQuery = GetJsonObject(BASE_URL + query);
            var jsonQueryResults = jsonQuery["QueryResult"]["Results"];

            return jsonQueryResults[0]; //check that there is a result. may need try catch
        }

        public static List<JToken> GetUserStories(string projectId, List<JToken> iterations) {

            //https:// rally1.rallydev.com/slm/webservice/v2.0/HierarchicalRequirement?query=((Project.ObjectID%20=%2015188699182)%20and%20(Iteration.ObjectID%20=%2038080627861))

            List<JToken> storys = new List<JToken>();

            foreach(JToken iteration in iterations) {

                string iteration_id = iteration.Value<string>("ObjectID");

                //TODO: get the full object with new learned flag. be sure to test out before commit
                var query   = string.Format("HierarchicalRequirement?query=((Project.ObjectID = {0}) and (Iteration.ObjectID = {1}))", projectId, iteration_id);
                var json    = GetJsonObject(BASE_URL + query);

                var results = json["QueryResult"]["Results"];

                foreach (JToken partial_story in results) {

                    string true_url = partial_story.Value<string>("_ref");
                    JToken full_story = GetJsonObject(true_url);

                    storys.Add(full_story["HierarchicalRequirement"]);
                }
            }

            return storys;
        }

        public static List<JToken> GetTasks(List<JToken> storys) {

            List<JToken> tasks = new List<JToken>();

            foreach (JToken story in storys) {

                var ref_url = story["Tasks"].Value<string>("_ref");
                var json    = GetJsonObject(ref_url);
                var results= json["QueryResult"]["Results"];

                tasks.AddRange(results);
            }

            return tasks;
        }

        public static List<Task> GetTasks(string userId, DateTime weekStart) {
            //https:// rally1.rallydev.com/slm/webservice/v2.0/task?query=(Owner.ObjectId%20=%2033470899520)&fetch=true

            List<Task> tasks = new List<Task>();

            var query = string.Format("task?query=(Owner.ObjectId = {0})&pagesize=200&fetch=true", userId);
            var json = GetJsonObject(BASE_URL + query);
            var results = json["QueryResult"]["Results"];

            foreach(JToken task in results) {

                var task_creation = Convert.ToDateTime(task.Value<string>("CreationDate"));

                //may also need to confirm state at some point
                if(task_creation.Date >= weekStart.Date) {

                    tasks.Add(new Task() {
                        json = task,
                        taskName = task.Value<string>("Name"),
                        taskObjectId = task.Value<string>("ObjectID"),
                        storyName = task["WorkProduct"].Value<string>("_refObjectName"),
                        weeklyTime = ApiWrapper.GetTimeEntryItems(task.Value<string>("ObjectID"))
                    });
                }
            }

            return tasks;
        }

        public static string GetTargetUserObjectId(string userName) {
            // https:// rally1.rallydev.com/slm/webservice/v2.0/user?query=(EmailAddress = bond@jbond.com)&fetch=true        
            var query   = string.Format("user?query=(EmailAddress = {0})&fetch=true", userName);
            var json    = GetJsonObject(BASE_URL + query);

            //check for errors .... meah
            string id = json["QueryResult"]["Results"][0].Value<string>("ObjectID");

            return id;
        }

        public static string GetSpecificTeamMemberObjectId(List<JToken> myTeam, string targetUsername) {
            //todo: just query the api directly in next version using the email (should be unique enough to get accurate results)
            var object_id = string.Empty;

            foreach(JToken person in myTeam) {
            
                string a_username = person.Value<string>("UserName");

                if(a_username.ToLower() == targetUsername.ToLower()) {
                    object_id = person.Value<string>("ObjectID");
                    break;
                }
            }

            return object_id;
        }

        private static Dictionary<DateTime, TimeEntryItem> GetTimeEntryItems(string taskObjectId) {
            
            var weekly_time = new Dictionary<DateTime, TimeEntryItem>();
                
            var query       = string.Format("timeentryitem?query=(Task.ObjectId = {0})&pagesize=200&fetch=true", taskObjectId);
            var json        = GetJsonObject(BASE_URL + query);
            var results     = json["QueryResult"]["Results"];

            foreach(JToken week in results) {
                
                var week_obj = new TimeEntryItem(){
                    json = week,
                    weekStartDate = week.Value<DateTime>("WeekStartDate").Date,
                    taskName = week.Value<string>("TaskDisplayString"),
                    storyName = week.Value<string>("WorkProductDisplayString"),
                    timeEntryObjectId = week.Value<string>("ObjectID"),
                    dailyTime = new Dictionary<DateTime, JToken>()
                };
                    
                string day_url = week["Values"].Value<string>("_ref");
                JToken day_list = GetJsonObject(day_url);

                foreach(JToken day in day_list["QueryResult"]["Results"]) {
                    week_obj.dailyTime.Add(day.Value<DateTime>("DateVal").Date, day);}

                weekly_time.Add(week_obj.weekStartDate, week_obj);
            }

            return weekly_time;
        }

        public static TimeEntryItem CreateNewTimeEntryItem(string projectId, string userId, string taskId, DateTime weekStarttDate) {

            var url = string.Format("https://rally1.rallydev.com/slm/webservice/v2.0/timeentryitem/create?key={0}", CACHED_AUTH.token);

            var final_post = new JObject();
            final_post["TimeEntryItem"] = new JObject();
            final_post["TimeEntryItem"]["Project"] = projectId;
            final_post["TimeEntryItem"]["Task"] = taskId;
            final_post["TimeEntryItem"]["WeekStartDate"] = weekStarttDate.ToString("yyyy-MM-ddThh:mm:ss+00:00");

            var response = JToken.Parse(HttpService.PostJson(url, final_post.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), CACHED_AUTH));
            
            bool has_error = ((JArray)response["CreateResult"]["Errors"]).Count > 0;
            
            if(has_error) {
                Console.WriteLine("Duplicate Time Entry Item creation attempt", 0);
            }
            
            return new TimeEntryItem(){
                
                json = response["CreateResult"],
                taskName = response["CreateResult"].Value<string>("TaskDisplayString"),
                storyName = "",
                timeEntryObjectId = response["CreateResult"].Value<string>("ObejctID"),
                weekStartDate = weekStarttDate,
                dailyTime = new Dictionary<DateTime,JToken>()
            };
        }

        public static JToken CreateNewAttachment(JObject newAttachment) {
            //create attachment seperately and after the new task is created only if there are attachments to be had.
            //https:// rally1.rallydev.com/slm/webservice/v2.0/attachment/create

            var url = string.Format("https://rally1.rallydev.com/slm/webservice/v2.0/attachment/create?key={0}", CACHED_AUTH.token);

            var json = new JObject();
            json["Task"] = new JObject();
            json["Task"]["Artifact"] = newAttachment.Value<string>("Artifact"); // the task or story to be associated with (use the ref url)
            json["Task"]["Content"] = newAttachment.Value<string>("Content"); //base64Binary (might need to create a new jobject with name [attachment content][Content])
            json["Task"]["ContentType"] = newAttachment.Value<string>("ContentType"); // "application/octet-stream"
            json["Task"]["Name"] = newAttachment.Value<string>("Name"); // max chara limit 1024
            json["Task"]["User"] = newAttachment.Value<string>("WorkProduct"); // user object id

            var response = HttpService.PostJson(url, json.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), CACHED_AUTH);

            return response;
        }

        public static JToken CreateNewTask(JObject newTask) {
            
            var url = string.Format("https://rally1.rallydev.com/slm/webservice/v2.0/task/create?key={0}", CACHED_AUTH.token);

            var json = new JObject();
            json["Task"] = new JObject();
            json["Task"]["Name"] = newTask.Value<string>("Name");
            json["Task"]["Description"] = newTask.Value<string>("Description");
            json["Task"]["Owner"] = newTask.Value<string>("Owner");
            json["Task"]["Estimate"] = newTask.Value<string>("Estimate");
            json["Task"]["State"] = newTask.Value<string>("State"); /* "Defined", "In-Progress", "Completed" */
            json["Task"]["TaskIndex"] = newTask.Value<string>("TaskIndex"); // just default to 1 ?
            json["Task"]["WorkProduct"] = newTask.Value<string>("WorkProduct"); // story formattedId ? probably not.

            var response = HttpService.PostJson(url, json.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), CACHED_AUTH);

            // foreach json["Task"]["Attachments"], create the attachment object AFTER succesful task creation. because i will need the ref url to associate the attachments

            return response;
        }

        public static List<JToken> SubmitTaskTimeValue(List<JObject> timeCard) {         
            // see if it is possible to update multiple objects at once... until then loop

            var responses = new List<JToken>();

            foreach(JObject punch in timeCard) {

                var url_action = punch.Value<string>("Verb") == "insert" ? "create?key=" + CACHED_AUTH.token : punch.Value<string>("ObjectID") + "?key=" + CACHED_AUTH.token;

                var url = string.Format("https://rally1.rallydev.com/slm/webservice/v2.0/timeentryvalue/{0}", url_action);

                var timeValueJson = new JObject();
                timeValueJson["TimeEntryValue"] = new JObject();
                timeValueJson["TimeEntryValue"]["DateVal"] = punch.Value<string>("DateVal");
                timeValueJson["TimeEntryValue"]["Hours"] = punch.Value<string>("Hours");
                timeValueJson["TimeEntryValue"]["TimeEntryItem"] = punch.Value<string>("TimeEntryItem");

                responses.Add(HttpService.PostJson(url, timeValueJson.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), CACHED_AUTH));
            }

            return responses;
        }

        public static JToken UpdateStoryBuildId(JObject postJson, string userStoryURL) {
            var final_post = new JObject();
            final_post["HierarchicalRequirement"] = new JObject(); 

            final_post["HierarchicalRequirement"]["c_BuildID"] = postJson.Value<string>("c_BuildID");
            final_post["HierarchicalRequirement"]["c_UnitTestComplete"] = "true";

            var preppedURL = userStoryURL + "{0}";
            var authenticateURL = "?key=" + CACHED_AUTH.token;
            var url = string.Format(preppedURL, authenticateURL);

            var response = HttpService.PostJson(url, final_post.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), CACHED_AUTH);
            return response;
        }

        public static string GetUserStoryReferenceUrl(string formattedId) {

            var query = string.Format("HierarchicalRequirement?query=(FormattedID = {0})", formattedId);
            var jsonQuery = GetJsonObject(BASE_URL + query);   
            var jsonQueryResults = jsonQuery["QueryResult"]["Results"];

            string true_url = "";
            //todo: M - not sure how to pull out the _ref any other way...gotta fix later
            foreach (JToken token in jsonQueryResults) {
                true_url = token.Value<string>("_ref");
                Console.WriteLine(true_url);
            }
            return true_url;
        }

    }
}