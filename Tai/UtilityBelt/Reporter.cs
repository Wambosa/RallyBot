using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tai.UtilityBelt {
    public static class Reporter {

        public static StringBuilder GenerateCliReport(List<JToken> fullTeam, List<JToken> iterations, List<JToken> storys, List<JToken> tasks) {

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

            return report;
        }

        public static StringBuilder GenerateMicrosoftHtmlTabularReport(List<JToken> storys, TaiConfig config) {

            var emailBody = new StringBuilder();

            var headerRow = string.Format("<tr><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{0}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{1}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{2}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{3}<o:p></o:p></p></td><td width=125 valign=top style='width:93.5pt;border:solid windowtext 1.0pt;border-left:none;padding:0in 5.4pt 0in 5.4pt'><p class=MsoNormal style='mso-element:frame;mso-element-frame-hspace:9.0pt;mso-element-wrap:around;mso-element-anchor-vertical:paragraph;mso-element-anchor-horizontal:margin;mso-element-top:-.65pt;mso-height-rule:exactly'>{4}<o:p></o:p></p></td></tr>",
                "Person",
                "Story",
                "Finished On",
                "Status",
                "Risks"
                );

            emailBody.Append(string.Format("<p>{0},</p><br>Here is the latest status report.<br><br>", config.emailGreeting));
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

            var signature = string.Format("<br><br><p>{0},</p><br><p>-{1}</p>", GoodSamaritan.GetFarewell(), config.emailSignature);

            emailBody.Append(signature);

            return emailBody;
        }

        public static StringBuilder GenerateGenerictHtmlTabularReport(List<JToken> storys, TaiConfig config) {

            var emailBody = new StringBuilder();

            var headerRow = string.Format("<tr><td><p>{0}</p></td> <td><p>{1}</p></td> <td><p>{2}</p></td> <td><p>{3}</p></td> <td><p>{4}</p></td></tr>",
                "Person",
                "Story",
                "Finished On",
                "Status",
                "Risks"
                );

            emailBody.Append(string.Format("<p>{0},</p><br>Here is the latest status report.<br><br>", config.emailGreeting));
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

            var signature = string.Format("<br><br><p>{0},</p><br><p>-{1}</p>", GoodSamaritan.GetFarewell(), config.emailSignature);

            emailBody.Append(signature);

            return emailBody;
        }
    }
}
