using System;
using System.Text;
using Tai.Extensions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai.UtilityBelt {

    internal static class Echo {

        internal static Byte LOG_LEVEL = 10;
        internal static string DELIMITER = ","; //todo: honor everywhere a cli report is generated (or other places that make sense)

        internal static void TaskReport(List<Task> tasks, DateTime weekToReportOn){
        
            foreach(Task task in tasks) {

                Echo.Out("Story Name: "+ task.storyName, 5);
                Echo.Out("Task Name: " + task.taskName, 5);
                Echo.Out("Time Values:\n", 5);

                foreach(KeyValuePair<DateTime, JToken> pair in task.weeklyTime[weekToReportOn].dailyTime) {

                    var time_value = pair.Value;

                    Echo.Out("DateVal: "       + time_value.Value<string>("DateVal").GetPrettyDate(), 6);
                    Echo.Out("Hours: "         + time_value.Value<string>("Hours"), 6);
                    Echo.Out("Last Updated: "  + time_value.Value<string>("LastUpdated"), 6);
                    Echo.Out("----------", 6);
                }

                Echo.Out("==================================================\n\n\n", 5);
            }
        }

        internal static void IterationReport(List<JToken> fullTeam, List<JToken> iterations, List<JToken> storys, List<JToken> tasks) {

            var report = new StringBuilder();

            foreach (JToken iteration in iterations){
                report.AppendLine(iteration.Value<string>("Name"));}
            report.AppendLine("==========================\n\n");

            report.AppendLine("Team Members");
            foreach (JToken person in fullTeam){
                report.AppendLine(person.Value<string>("DisplayName"));}
            report.AppendLine("==========================\n\n");

            foreach (JToken story in storys){

                var hero = story.Value<string>("HeroOfTime");
                var name = story.Value<string>("Name");
                var risk = story.Value<string>("BlockedReason");
                var status = story.Value<string>("ScheduleState");
                var date = story.Value<string>("AcceptedDate").GetPrettyDate();

                var line = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", hero, name, date, status, risk);

                report.AppendLine(line);
                report.AppendLine("-----------------------\n");
            }

            Out(report, 1);
        }

		internal static void WelcomeText() {
			Out("\n\nTai \n" +
			 @"
                         ..sSs$$$$$$b.                                       
                       .$$$$$$$$$$$$$$$.                                     
                    .$$$$$$$$$$$$$$$$$$$$$b.                                 
                  .$$$$$$$$$$$$$$$$$$$$$$$$$                                 
                 $$$$$$$$$$$$$$$$$S'   `$$$$                                 
                 $$$$$$$$$$$$$$S'        $$$                                 
                 $$$$$$$$$$$$$'          `$$.                                
                 `$$$$$$$$$$$$$           `$$$.                              
                   `$$$$$$$$$'       .s$$$ $$ $                              
                     $$$$$$$$$.sSs .s$$s'   s s                              
                      $$$$$$$$$$$$           $P                              
                      `$$$$$$$$$$$s          $                               
                        $$$$$$$$$$$.    ',                                   
                        `$$$$$$$$$$sS$                                       
        s$$$.            `$$$$$$$$$$$$.s''   .$.                             
        $$$$$.              `$$$$$$$$$$.    .$$$Ss.s$s.                      
         $$$$$.              `$$$$$$$$$P   .$$$$$$$$$$$$.                    
         $$$$$$.               `$$$$$$$' .$$$$$$$$$$$$$$$$.                  
         `$$$$$$.                 $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$s.          
           $$$$$$.                `$$$$$$$$$ $$$$$$$$$$$$$$$$$$$$$$$$s.      
         .s$$$$$$$.                 `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$.    
         s  $$$$$$$.                .$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$.   
         `$$$$$$$$$$.             .$$$' $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$.  
          `$$$$$$$$$$.           s$'   $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$.
          $$$$$$$$$$$$e         $$$     `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        .' $$$$$$$$$$7         $$$$       `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
       '   `$$$$$$$$7         $$$$$       .$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      $$Ss..$$$$$$$7        $$$$$$$    .s$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
       $$$$$$$$$$$$        $$$$$$$$ .s$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        $$$$$$$$$$$     .$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      .$$$$$$$$$$$$$   .$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      $$$$$$$$$$$$$$  .$$$$$$$$$$$ $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      $$$$$$$$$$$$$$ .$$$$$$$$$$$' `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      $$$$$$$$$$$$$$ $$$$$$$$$$$$   `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      $$$$$$$$$$$$$$$$$$$$$$$$$$$    `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
      `$$$$$$$$$$$$$$$$$$$$$$$$$$     `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
       $$$$$$$$$$$$$$$$$$$$$$$$$$.     $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
       `$$$$$$$$$$$$$$$$$$$$$$$$$$     `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
         $$$$$$$$$$$$$$$$$$$$$$$$$      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
          $$$$$$$$$$$$$$$$$$$$$$$$      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
           $$$$$$$$$$ $$$$$$$$$$$$      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
            $$$$$$$$$$$$$$$$$$$$$$      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
             $$$$$$$$$$$$$$$$$$$$'      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
             `$$$$$$$$$$$$$$$$$$$       $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
              `$$$$$$$$$$$$$$$$$$.      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
               `$$$$$$$$$$$$$$$$$$      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                 $$$$$$$$$$$$$$$$$.     $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                  `$$$$$$$$$$$$$$$$     $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                    $$$$$$$$$$$$$$$     `$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                    $$$$$$$$$$$$$$$      $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
$           $$$'  `$$$$$    $$$$$$$      `$$$$'  `$$$   $   $$   q$   $     $
$           $$'    `$$$$    $$$$$$$   $b  `$$'    `$$.  $  .$$    q   $   $$$
$$$$     $$$$'  db  `$$$$$$$$$$$$$$   $P  .$'  db  `$$     $$$        $   $$$
$$$$     $$$$   $$   $$$$$$$$$$$$$$      .$$   $$   $$.   .$$$        $     $
$$$$b   d$$$$   $$   $$$    $$$$$$$   $$$$$$   $$   $$$   $$$$        $   $$$
$$$$$   $$$$$        $$$    $$$$$$$   $$$$$$        $$$   $$$$   b    $   $$$
$$$$$   $$$$$   $$   $$$    $$$$$$$   $$$$$$   $$   $$$   $$$$   $b   $     $
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ dp $            
            ", 5);
		}

		internal static void OffensiveGesture() {
			Out(@"┌∩┐(>_<)┌∩┐", 5);
		}

		internal static void HelpText() {
			Out(@"
                Tai
                designed to lessen the burden of admin tasks

                todo: write a better help file
            ", 2);
		}

		internal static void ErrorReport(string[] badInput) {
			var errReport = new StringBuilder();
			foreach (var misunderstoodWord in badInput)
				errReport.AppendLine("No switch available for: " + misunderstoodWord);
			
			Out(errReport, 1);
		}

        public static void Out(string message, int noisiness = 1) {
            
            if(noisiness <= LOG_LEVEL) {
                Console.WriteLine(message);
            }
        }

        public static void Out(StringBuilder sb, int noisiness = 1) {
            
            if(noisiness <= LOG_LEVEL) {
                Console.WriteLine(sb);
            }
        }

    }
}
