using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MoodServer
{
    class TestManager : IDbManager 
    {
        public int GetIdByName(string loc)
        {
            return -1;
        }

        public string GetLocations()
        {
            return new TestScript().Location;
        }

        public string MakeBarChartScript(string loc, DateTime date)
        {
            return new TestScript().BarChartScript;
        }

        public string MakePeriodChartScript(string loc, DateTime datea, DateTime dateb, string type)
        {
            return new TestScript().PeriodScript;
        }

        public string MakePieChartScript(string loc, DateTime date)
        {
            return new TestScript().PieChartScript;
        }

        public string MakePieChartScriptPeriod(string loc, DateTime datea, DateTime dateb)
        {
            return MakeBarChartScript(loc, datea);
        }

        public string MakeSelectScript()
        {
            string locations = GetLocations();
            List<Loc> locationList = JsonConvert.DeserializeObject<List<Loc>>(locations);
            List<string> locs = new List<string>();

            foreach (Loc l in locationList)
            {
                locs.Add(l.Location);
            }

            StringBuilder script = new StringBuilder();
            script.Append("var x = document.getElementById('locations');");

            foreach (string s in locs)
            {
                script.Append("var option = document.createElement('option');option.text ='" + s + "';x.add(option); ");
            }
            script.Append("document.getElementById('day_1').value = '" + DateTime.Today.Day.ToString() + "';");
            script.Append("document.getElementById('month_1').value = '" + DateTime.Today.Month.ToString() + "';");
            script.Append("document.getElementById('year_1').value = '" + DateTime.Today.Year.ToString() + "';");
            return script.ToString();
        }

        public void SaveMood(int mood, int location)
        {
        }
    }

    class TestScript
    {
        public string PeriodScript =
            "<script type='text/javascript'>" +
            "var vg = {x: ['06/11/2016','06/12/2016','06/13/2016','06/14/2016','06/15/2016','06/16/2016','06/17/2016','06/18/2016','06/19/2016','06/20/2016','06/21/2016','06/22/2016','06/23/2016','06/24/2016','06/25/2016','06/26/2016','06/27/2016','06/28/2016','06/29/2016','06/30/2016','07/01/2016','07/02/2016','07/03/2016','07/04/2016','07/05/2016','07/06/2016','07/07/2016','07/08/2016','07/09/2016','07/10/2016','07/11/2016']," +
            "y: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,15,8,0,0,6,3,1,1,4,0,0,3],name: 'Very Good',line: {shape: 'spline',color: 'rgba(44,160,44,1)'},type: 'scatter'};" +
            "var g = {x: ['06/11/2016','06/12/2016','06/13/2016','06/14/2016','06/15/2016','06/16/2016','06/17/2016','06/18/2016','06/19/2016','06/20/2016','06/21/2016','06/22/2016','06/23/2016','06/24/2016','06/25/2016','06/26/2016','06/27/2016','06/28/2016','06/29/2016','06/30/2016','07/01/2016','07/02/2016','07/03/2016','07/04/2016','07/05/2016','07/06/2016','07/07/2016','07/08/2016','07/09/2016','07/10/2016','07/11/2016']," +
            "y: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,23,0,0,11,2,1,1,5,0,0,2],name: 'Good',line: {shape: 'spline',color: 'rgba(31,119,180,1)'},type: 'scatter'};" +
            "var b = {x: ['06/11/2016','06/12/2016','06/13/2016','06/14/2016','06/15/2016','06/16/2016','06/17/2016','06/18/2016','06/19/2016','06/20/2016','06/21/2016','06/22/2016','06/23/2016','06/24/2016','06/25/2016','06/26/2016','06/27/2016','06/28/2016','06/29/2016','06/30/2016','07/01/2016','07/02/2016','07/03/2016','07/04/2016','07/05/2016','07/06/2016','07/07/2016','07/08/2016','07/09/2016','07/10/2016','07/11/2016']," +
            "y: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,14,0,0,4,1,0,0,3,0,0,2],name: 'Bad',line: {shape: 'spline',color: 'rgba(255,127,14,1)'},type: 'scatter'};" +
            "var vb = {x: ['06/11/2016','06/12/2016','06/13/2016','06/14/2016','06/15/2016','06/16/2016','06/17/2016','06/18/2016','06/19/2016','06/20/2016','06/21/2016','06/22/2016','06/23/2016','06/24/2016','06/25/2016','06/26/2016','06/27/2016','06/28/2016','06/29/2016','06/30/2016','07/01/2016','07/02/2016','07/03/2016','07/04/2016','07/05/2016','07/06/2016','07/07/2016','07/08/2016','07/09/2016','07/10/2016','07/11/2016']," +
            "y: [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,5,0,0,5,1,1,1,2,0,0,1],name: 'Very Bad',line: {shape: 'spline',color: 'rgba(214,39,40,1)'},type: 'scatter'};" +
            "var data = [vg,g,b,vb];" +
            "var layout = {title: 'All locations - 06/11/2016 - 07/11/2016',xaxis:{tickangle: -45,title: 'Time'},yaxis: {title: 'Amount of people per mood'}};" +
            "Plotly.newPlot(gd, data, layout);document.title = 'All locations - 06/11/2016 - 07/11/2016';" +
            "</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>";

        public string BarChartScript =
            "<script>" +
            "document.title = 'All locations - 07/04/2016';" +
            "var layout = {title: 'All locations - 07/04/2016'};" +
            "var data = [{x: ['Very Good','Good','Bad','Very Bad']," +
            "y: [6,11,4,5],marker:{color: ['rgba(44,160,44,1)', 'rgba(31,119,180,1)', 'rgba(255,127,14,1)', 'rgba(214,39,40,1)']},type: 'bar'}];" +
            "Plotly.newPlot(gd, data, layout);" +
            "</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>";

        public string PieChartScript =
            "<script>" +
            "var data = [{values: [41,48,24,18],labels: ['Very Good','Good','Bad','Very Bad']," +
            "type: 'pie',marker:{colors:['rgba(44,160,44,1)', 'rgba(31,119,180,1)', 'rgba(255,127,14,1)', 'rgba(214,39,40,1)'] }}];" +
            "Plotly.newPlot(gd, data);document.title = 'All locations - 06/11/2016 - 07/11/2016';" +
            "</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>";

        public string Location =
            "[ { \"Location\": \"Kassel\", \"Identiefier\": 1 }, { \"Location\": \"Baar\", \"Identiefier\": 2 }, { \"Location\": \"Berlin\", \"Identiefier\": 3 }, { \"Location\": \"Wien\", \"Identiefier\": 4 } ]";
    }
}
