using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Nancy;

namespace MoodServer
{
    public class MoodModule : NancyModule
    {

        private readonly IDbManager _db;

        public MoodModule(IDbManager db)
        {
            _db = db;

            Get[@"/"] = _ =>
            {
                using(WebClient client = new WebClient())
                {
                    return client.DownloadString("views/entry.html");
                }
            };

            Post["/"] = _ =>
            {
                string loc = Request.Form.location;
                int mood = Int32.Parse(Request.Form.mood);
                _db.SaveMood(mood, _db.GetIdByName(loc));
                return "<script>alert('Thanks for voting!');window.history.back();window.stop();</script>";
            };

            Get["/res/{res}"] = parameter =>
            {
                string response = "res/" + parameter.res;
                return Response.AsFile(response);
            };

            Get["/results"] = parameters =>
            {
                using (WebClient client = new WebClient())
                {
                    return client.DownloadString("views/form.html");

                }
            };

            Post["/results/diagram"] = _ =>
            {
                string loc = Request.Form.location;
                string day = Request.Form.day;
                string month = Request.Form.month;
                string year = Request.Form.year;
                string dayA = Request.Form.day2;
                string monthA = Request.Form.month2;
                string yearA = Request.Form.year2;
                string chart = Request.Form.chart;
                using(WebClient client = new WebClient())
                {
                    StringBuilder script = new StringBuilder();
                    script.Append(client.DownloadString("views/dia.html"));

                    if (chart.Equals("1"))
                    {
                        if (!dayA.Equals("0") && !monthA.Equals("0") && !yearA.Equals("0"))
                        {                            
                            script.Append(_db.MakePeriodChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)), new DateTime(Int32.Parse(yearA), Int32.Parse(monthA), Int32.Parse(dayA)), "1"));
                            return script.ToString();
                        }
                        else
                        {
                            script.Append(_db.MakeBarChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day))));
                            return script.ToString();
                        }
                    }
                    else if(chart.Equals("2"))
                    {
                        if (!dayA.Equals("0") && !monthA.Equals("0") && !yearA.Equals("0"))
                        {
                            script.Append(_db.MakePeriodChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)), new DateTime(Int32.Parse(yearA), Int32.Parse(monthA), Int32.Parse(dayA)), "2"));
                            return script.ToString();
                        }
                        else
                        {     
                            script.Append(_db.MakeBarChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day))));
                            return script.ToString();
                        }
                    }
                    else if (chart.Equals("3"))
                    {
                        if (!dayA.Equals("0") && !monthA.Equals("0") && !yearA.Equals("0"))
                        {
                            script.Append(_db.MakePeriodChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)), new DateTime(Int32.Parse(yearA), Int32.Parse(monthA), Int32.Parse(dayA)), "3"));
                            return script.ToString();
                        }
                        else
                        {
                            script.Append(_db.MakeBarChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day))));
                            return script.ToString();
                        }
                    }
                    else if (chart.Equals("4"))
                    {
                        if (!dayA.Equals("0") && !monthA.Equals("0") && !yearA.Equals("0"))
                        {
                            script.Append(_db.MakePieChartScriptPeriod(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)), new DateTime(Int32.Parse(yearA), Int32.Parse(monthA), Int32.Parse(dayA))));
                            return script.ToString();
                        }
                        else
                        {
                            script.Append(_db.MakePieChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day))));
                            return script.ToString();
                        }
                    }
                }
                return "alert('Something went wrong!');window.history.back();";
            };

            Get["/api/locations/"] = _ => _db.GetLocations();

            Get["/api/locationscript"] = _ => _db.MakeSelectScript();

            Get["/api/info"] = _ => "<b>Moody by Ariel Simulevski</b><br>Server powered by <b>NancyFX</b><br>Client powered by <b>Xamarin</b>";

            Post["/api/entry"] = _ =>
            {
                int mood = Request.Form.mood;
                int location = Request.Form.location;
                _db.SaveMood(mood, location);
                return "";
            };
        }
    }
}