/* keeping mono options for this project because reasons */
using System;
using Mono.Options;
using Tai.UtilityBelt;
using System.Collections.Generic;

namespace Tai {

    class Program {

        delegate void TaiMethod(TaiConfig config);

        private enum TAI_COMMAND {NONE, REPORT, BURNDOWN, SETBUILDID, CREATETASK};

        private static TAI_COMMAND SESSION_ACTION = TAI_COMMAND.NONE;

        private static Dictionary<TAI_COMMAND, TaiMethod> TaiTakeCareOfThis = new Dictionary<TAI_COMMAND, TaiMethod>() {
            {TAI_COMMAND.REPORT, CLIMethod.WriteStatusReportForAnIteration},
            {TAI_COMMAND.BURNDOWN, CLIMethod.AutomaticallySetTime_Alpha},
            {TAI_COMMAND.SETBUILDID, CLIMethod.SetStoryBuildId}
        };

        public static void Main(string[] args) {
            
            TaiConfig config = new TaiConfig(@Grapple.GetThisFolder() + "tai.conf");

            //muahahha anonymouse functions in c# !!!
	        var options = new OptionSet() {
				{ "iteration-report", "", _ => {SESSION_ACTION = TAI_COMMAND.REPORT;}},
				{ "fill-time-values", "", _ => {SESSION_ACTION = TAI_COMMAND.BURNDOWN;}},
				{ "set-story-build", "", _ => {SESSION_ACTION = TAI_COMMAND.SETBUILDID;}},
				//todo: generic task creation
                { "?|h|help", "", _ => Echo.HelpText()},
                                
                {"api-url=", "", url => {config["apiUrl"] = url;}}, 
                {"project-id=", "", proj => {config["projectId"] = proj;}},                
                {"target-user=", "", user => {config["targetUser"] = user;}},
                {"story-id=", "", storyId => {config["storyId"] = storyId;}},
                {"build-id=", "", buildId => {config["buildId"] = buildId;}},
                {"burndown-date=", "", date => {config["burndownDate"] = date;}},
                {"email-greeting=", "", hi => {config["emailGreeting"] = hi;}},
                {"email-signature=", "", me => {config["emailSignature"] = me;}},
                {"iteration-number=", "", num => {config["iterationNumber"] = num;}},
                {"status-report-names=", "", names => {config["statusReportNames"] = names;}}, //this one will have to be split later by comma

				{"v=|verbosity=", "", noise => {Echo.LOG_LEVEL = Convert.ToByte(noise);}},


                /* i don't like the idea of passing credentials, however added it for completion */
                {"username=", "", user => {config["username"] = user;}},
                {"password=", "", pass => {config["password"] = pass;}}
			};

            var badInput = options.Parse(args);

            if(badInput.Count > 0) {
                Echo.ErrorReport(badInput.ToArray());
                Echo.OffensiveGesture();
                Echo.HelpText();
            }

            Echo.WelcomeText();

            config = ConfirmConfigCredentials(config);
            ApiWrapper.Initialize(config);

            TaiTakeCareOfThis[SESSION_ACTION](config);

            Echo.Out("done", 2);
        }

		private static TaiConfig ConfirmConfigCredentials(TaiConfig conf) {

			if(!conf.ContainsKey("username")) {
				conf["username"] = Grapple.GetUsernameFromTerminal();}

			if(!conf.ContainsKey("password")) {
				conf["password"] = Grapple.GetPasswordFromTerminal();}
			
            return conf;
		}
    }
}