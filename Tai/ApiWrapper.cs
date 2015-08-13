using System;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tai {

    public class ApiWrapper {

        private string BASE_URL = "https://rally1.rallydev.com/slm/webservice/v2.0/";
        private CachedAuthentication CACHED_AUTH;

        public ApiWrapper(TaiConfig config) {

            BASE_URL = config.apiUrl ?? BASE_URL;

            CACHED_AUTH = GetCachedAuthentication(config.username, config.password);
        }

        private CredentialCache MakeCredentials(Uri aUri) {
            var credentials = new CredentialCache();
            credentials.Add(aUri, "Basic", new NetworkCredential(CACHED_AUTH.username, CACHED_AUTH.password));
            return credentials;
        }

        private CredentialCache MakeCredentials(Uri aUri, string username, string password) {
            var credentials = new CredentialCache();
            credentials.Add(aUri, "Basic", new NetworkCredential(username, password));
            return credentials;
        }

        private CachedAuthentication GetCachedAuthentication(string username, string password) {
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

        public string TestSecurityToken() {
            return CACHED_AUTH.token != null ? "Connection was made successfully!" : "YOUR XBOX IS BRICKED! AGAIN! there is no security token";
        }

        private JToken GetJsonObject(string url) {

            var uri = new Uri(url);

            return JToken.Parse(HttpService.GetRawJson(uri, MakeCredentials(uri)));
        }

        public List<JToken> GetIteration(string projectId, string iterationNum) {

            //https:// rally1.rallydev.com/slm/webservice/v2.0/Project/15188699182/Iterations?query=(Name%20contains%20%2292%22)

            var query       = string.Format("Project/{0}/Iterations?query=(Name contains {1})", projectId, iterationNum);
            var json        = GetJsonObject(BASE_URL + query);
            var iterations  = new List<JToken>();

            iterations.AddRange(json["QueryResult"]["Results"]);
            return iterations;
        }

        public List<JToken> GetTeamMembers(string projectId) {

            var people  = new List<JToken>();
            var query   = string.Format("Project/{0}/TeamMembers", projectId);
            var json    = GetJsonObject(BASE_URL + query);

            people.AddRange(json["QueryResult"]["Results"]);
            return people;
        }

        public List<JToken> GetUserStories(string projectId, List<JToken> iterations) {

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

        public List<JToken> GetTasks(List<JToken> storys) {

            List<JToken> tasks = new List<JToken>();

            foreach (JToken story in storys) {

                var ref_url = story["Tasks"].Value<string>("_ref");
                var json    = GetJsonObject(ref_url);
                var results= json["QueryResult"]["Results"];

                tasks.AddRange(results);
            }

            return tasks;
        }

        public List<JToken> GetTasks(string userId, DateTime weekStart) {
            //https:// rally1.rallydev.com/slm/webservice/v2.0/task?query=(Owner.ObjectId%20=%2033470899520)&fetch=true

            List<JToken> tasks = new List<JToken>();

            var query = string.Format("task?query=(Owner.ObjectId = {0})&pagesize=200&fetch=true", userId);
            var json = GetJsonObject(BASE_URL + query);
            var results = json["QueryResult"]["Results"];

            foreach(JToken task in results) {

                var task_creation = Convert.ToDateTime(task.Value<string>("CreationDate"));

                if(task_creation.Date >= weekStart.Date) {
                    tasks.Add(task);}
            }

            return tasks;
        }
        

        public string GetSpecificTeamMemberObjectId(List<JToken> myTeam, string targetUsername) {
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

        public List<TimeEntryItem> GetTimeEntryItemsForOneTeamMember(List<JToken> tasks, DateTime weekStart) {
            //https:// rally1.rallydev.com/slm/webservice/v2.0/timeentryitem?query=((Project.ObjectId = 15188699182) and (User.ObjectId = 33470899520))

            var time_entrys = new List<TimeEntryItem>();

            foreach(JToken task in tasks) {
                
                var query       = string.Format("timeentryitem?query=(Task.ObjectId = {0})&pagesize=200", task.Value<string>("ObjectID"));
                var json        = GetJsonObject(BASE_URL + query);
                var results     = json["QueryResult"]["Results"];

                var task_state  = task.Value<string>("State");
                var task_creation = Convert.ToDateTime(task.Value<string>("CreationDate"));

                if(task_creation.Date >= weekStart && task_creation.Date <= weekStart.AddDays(7) && (task_state == "Defined" || task_state == "In-Progress")) {
                
                    //todo: remove partials and just fetch the entire object with new learned flag fetch
                    foreach(JToken partial_item in results) {
            

                        string item_url         = partial_item.Value<string>("_ref");
                        JToken full_time_item   = GetJsonObject(item_url);
                        var item_week_start     = Convert.ToDateTime(full_time_item["TimeEntryItem"].Value<string>("WeekStartDate"));//this is not accurate enough unfortunately.
                        
                        if(item_week_start.Date == weekStart.Date) {
                            var custom = new TimeEntryItem() {
                                self = full_time_item["TimeEntryItem"],
                                taskName = full_time_item["TimeEntryItem"].Value<string>("TaskDisplayString"),
                                storyName = full_time_item["TimeEntryItem"].Value<string>("WorkProductDisplayString"),
                                timeEntryObjectId = full_time_item["TimeEntryItem"].Value<string>("ObjectID"),
                                timeEntryValues = new List<JToken>()
                            };
                    
                            string value_url = full_time_item["TimeEntryItem"]["Values"].Value<string>("_ref");
                            JToken value_list = GetJsonObject(value_url);
                            custom.timeEntryValues.AddRange(value_list["QueryResult"]["Results"]);
                            time_entrys.Add(custom);
                        }
                    }                
                }
            }
            
            return time_entrys;
        }

        public string CreateNewTimeEntryItem(string projectId, string userId, string taskId, DateTime weekStarttDate) {

            var url = string.Format("https://rally1.rallydev.com/slm/webservice/v2.0/timeentryitem/create?key={0}", CACHED_AUTH.token);

            var final_post = new JObject();
            final_post["TimeEntryItem"] = new JObject();
            final_post["TimeEntryItem"]["Project"] = projectId;
            final_post["TimeEntryItem"]["Task"] = taskId;
            final_post["TimeEntryItem"]["WeekStartDate"] = weekStarttDate.ToString("yyyy-MM-ddThh:mm:ss+00:00");

            var response = JToken.Parse(HttpService.PostJson(url, final_post.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), CACHED_AUTH));
            
            bool has_error = response["CreateResult"].Value<string>("Errors").Contains("violation");

            return has_error ? string.Empty : response["CreateResult"].Value<string>("ObjectID");
        }

        public JToken PostNewTimeEntryValue(JObject postJson) {
            //todo: clean this up

            var url_action = (string)postJson["Verb"] == "insert" ? "create?key=" + CACHED_AUTH.token : (string)postJson["ObjectID"] + "?key=" + CACHED_AUTH.token;

            var url = string.Format("https://rally1.rallydev.com/slm/webservice/v2.0/timeentryvalue/{0}", url_action);

            var final_post = new JObject();
            final_post["TimeEntryValue"] = new JObject();

            final_post["TimeEntryValue"]["DateVal"] = postJson.Value<string>("DateVal");
            final_post["TimeEntryValue"]["Hours"] = postJson.Value<string>("Hours");
            final_post["TimeEntryValue"]["TimeEntryItem"] = postJson.Value<string>("TimeEntryItem");

            //var response = HttpService.PostJson(url, final_post.ToString(Newtonsoft.Json.Formatting.None), MakeCredentials(new Uri(url)), RALLY_AUTH);
            //todo: 
            return final_post; //response;
        }

        public JToken PostNewBuildId(JObject postJson, string userStoryURL) {
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

        public string GetUserStoryRef(string storyId) {

            var query = string.Format("HierarchicalRequirement?query=(FormattedID = {0})", storyId);
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