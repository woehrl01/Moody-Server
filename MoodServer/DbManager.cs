using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Threading;
using Exceptionless;
using Newtonsoft.Json;

namespace MoodServer
{
    public class DbManager : IDbManager
    {
        private SqlConnection Connection { get;  }

        public DbManager(IDatabaseConfig dbConfig)
        {
            string connectionString = dbConfig.ConnectionString;
            Connection = new SqlConnection(connectionString);
            
            Console.WriteLine("Trying to connect to the database...");
            try
            {
                Connection.Open();
                Connection.Close();
                Console.WriteLine("Connection succesful! \n");
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                Console.WriteLine("Can not open connection to SQL Server ! ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        public void SaveMood(int mood, int location)
        {
            DateTime localDate = DateTime.Now;
            string guid = Guid.NewGuid().ToString(); ;

            string save = "INSERT into entries (Id,mood,location,date) VALUES (@id,@mood,@loc,@date)";

            using (SqlCommand command = new SqlCommand(save))
            {
                command.Connection = Connection;
                command.Parameters.Add("@id", SqlDbType.VarChar, 36).Value = guid;
                command.Parameters.Add("@mood", SqlDbType.Int).Value = mood;
                command.Parameters.Add("@loc", SqlDbType.Int).Value = location;
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = localDate;

                try
                {
                    Connection.Open();
                    Console.WriteLine("SQL Connection Open ! ");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Inserting data... ");

                    string query = command.CommandText;

                    foreach (SqlParameter p in command.Parameters)
                    {
                        query = query.Replace(p.ParameterName, p.Value.ToString());
                    }
                    Console.WriteLine("[" + query + "]");
                }
                catch (SqlException e)
                {
                    e.ToExceptionless().Submit();
                    Console.WriteLine("Something went wrong ! ");
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    Connection.Close();
                    Console.WriteLine("SQL Connection Closed ! \n");
                }
            }
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

        public string MakeBarChartScript(string loc,DateTime date)
        {
            StringBuilder script = new StringBuilder();
            script.Append("<script>document.title = '");
            script.Append(loc);
            script.Append(" - ");
            script.Append(date.ToString("MM'/'dd'/'yyyy"));
            script.Append("';");
            script.Append(GetEntriesForBarChart(GetIdByName(loc).ToString(), date, loc));
            script.Append("</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>");
            return script.ToString();
        }

        public string MakePeriodChartScript(string loc, DateTime datea, DateTime dateb,string type)
        {
            StringBuilder script = new StringBuilder();
            script.Append("<script type='text/javascript'>");
            script.Append(GetEntriesForPeriodChart(GetIdByName(loc).ToString(), datea, dateb, loc, type));
            script.Append("document.title = '" + loc + " - " + datea.ToString("MM'/'dd'/'yyyy") + " - " + dateb.ToString("MM'/'dd'/'yyyy") + "';");
            script.Append("</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>");
            return script.ToString();
        }

        public string MakePieChartScript(string loc, DateTime date)
        {
            StringBuilder script = new StringBuilder();
            script.Append("<script>document.title = '");
            script.Append(loc);
            script.Append(date.ToString("MM'/'dd'/'yyyy"));
            script.Append("';");
            script.Append(GetEntriesForPieChart(GetIdByName(loc).ToString(), date));
            script.Append("</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>");
            return script.ToString();
        }

        public string MakePieChartScriptPeriod(string loc, DateTime datea, DateTime dateb)
        {
            StringBuilder script = new StringBuilder();
            script.Append("<script type='text/javascript'>");
            script.Append(GetEntriesForPieChartPeriod(GetIdByName(loc).ToString(), datea, dateb));
            script.Append("document.title = '");
            script.Append(loc);
            script.Append(" - ");
            script.Append(datea.ToString("MM'/'dd'/'yyyy"));
            script.Append(" - ");
            script.Append(dateb.ToString("MM'/'dd'/'yyyy"));
            script.Append("';</script><script src='/res/response.js'><style>.js-plotly-plot{margin:0;}</style></script></body></html>");
            return script.ToString();
        }

        public int GetIdByName(string loc)
        {
            SqlDataReader reader = null;
            int id = 0;
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                var cmd = new SqlCommand("SELECT Id FROM locations WHERE location = @loc", Connection);
                cmd.Parameters.AddWithValue("loc", loc);
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting locationID from location " + loc + "...");
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }
            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    id = reader.GetInt32(0);
                }
            }
            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");

            return id;
        }

        private string GetEntriesForBarChart(string locationId, DateTime date, string locationName)
        {
            SqlDataReader reader = null;
            List<string> x = new List<string>();
            List<string> y = new List<string>();
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                SqlCommand cmd;
                if (locationId.Equals("0"))
                {
                    cmd = new SqlCommand("select m.[Desc],count(e.mood) from mood m left join entries e on e.mood = m.Id and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])),101) = @date group by  m.[Desc], mood, m.id order by m.Id asc", Connection);
                    cmd.Parameters.AddWithValue("date", date);
                }
                else
                {
                    cmd = new SqlCommand("select m.[Desc],count(e.mood) from mood m left join entries e on e.mood = m.Id and e.location = @locationId and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])),101) = @date group by  m.[Desc], mood, m.id order by m.Id asc", Connection);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("locationId", locationId);
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting entries for date " + date.ToString("MM'/'dd'/'yyyy") + " for locationID " + locationId + "...");
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }
            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    x.Add("'" + reader.GetString(0) + "'");
                    y.Add(reader.GetInt32(1).ToString());
                    Console.WriteLine(reader.GetString(0) + " - " + reader.GetInt32(1));
                }
            }

            StringBuilder script = new StringBuilder();
            script.Append("var layout = {title: '");
            script.Append(locationName);
            script.Append(" - ");
            script.Append(date.ToString("MM'/'dd'/'yyyy"));
            script.Append("'};");
            script.Append("var data = [{");
            script.Append(ToCoordinateString(x, "x"));
            script.Append(ToCoordinateString(y, "y"));
            script.Append("marker:{color: ['rgba(44,160,44,1)', 'rgba(31,119,180,1)', 'rgba(255,127,14,1)', 'rgba(214,39,40,1)']},type: 'bar'}];Plotly.newPlot(gd, data, layout);");
            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");
            return script.ToString();
        }

        private string GetEntriesForPeriodChart(string locationId, DateTime datea, DateTime dateb, string locationName, string type)
        {
            if(datea > dateb)
            {
                return "alert('First date must not be later than second date!');window.history.back();";
            }
            else if(datea == dateb)
            {
                return GetEntriesForBarChart(locationId, datea,locationName);
            }

            StringBuilder xAxis = new StringBuilder();
            xAxis.Append("x: [");

            var dates = new List<DateTime>();
            Dictionary<DateTime, Mood> dic = new Dictionary<DateTime, Mood>();
            for (var dt = datea; dt <= dateb; dt = dt.AddDays(1))
            {
                dates.Add(dt);
                dic.Add(dt, new Mood());
                xAxis.Append("'" + dt.ToString("MM'/'dd'/'yyyy") + "',");
            }

            xAxis.Length -= 1;
            xAxis.Append("],");

            StringBuilder vgJs = new StringBuilder();
            vgJs.Append("var vg = {");
            vgJs.Append(xAxis);
            vgJs.Append("y: [");
            StringBuilder gJs = new StringBuilder();
            gJs.Append("var g = {");
            gJs.Append(xAxis);
            gJs.Append("y: [");
            StringBuilder bJs = new StringBuilder();
            bJs.Append("var b = {");
            bJs.Append(xAxis);
            bJs.Append("y: [");
            StringBuilder vbJs = new StringBuilder();
            vbJs.Append("var vb = {");
            vbJs.Append(xAxis);
            vbJs.Append("y: [");

            GetAllEntriesBetweenDates(locationId, datea, dateb, dic);

            foreach (DateTime d in dates)
            {
                vgJs.Append(dic[d].VeryGood);
                vgJs.Append(",");
                gJs.Append(dic[d].Good);
                gJs.Append(",");
                bJs.Append(dic[d].Bad);
                bJs.Append(",");
                vbJs.Append(dic[d].VeryBad);
                vbJs.Append(",");
            }

            vgJs.Length -= 1;
            gJs.Length -= 1;
            bJs.Length -= 1;
            vbJs.Length -= 1;

            if (type.Equals("1"))
            {
                vgJs.Append("],name: 'Very Good',line: {shape: 'spline',color: 'rgba(44,160,44,1)'},type: 'scatter'};");
                gJs.Append("],name: 'Good',line: {shape: 'spline',color: 'rgba(31,119,180,1)'},type: 'scatter'};");
                bJs.Append("],name: 'Bad',line: {shape: 'spline',color: 'rgba(255,127,14,1)'},type: 'scatter'};");
                vbJs.Append("],name: 'Very Bad',line: {shape: 'spline',color: 'rgba(214,39,40,1)'},type: 'scatter'};");
            }
            else if(type.Equals("2") || type.Equals("3"))
            {
                vgJs.Append("],name: 'Very Good',type: 'bar',marker: {color: 'rgba(44,160,44,1)'}};");
                gJs.Append("],name: 'Good',type: 'bar',marker: {color: 'rgba(31,119,180,1)'}};");
                bJs.Append("],name: 'Bad',type: 'bar',marker: {color: 'rgba(255,127,14,1)'}};");
                vbJs.Append("],name: 'Very Bad',type: 'bar',marker: {color: 'rgba(214,39,40,1)'}};");
            }

            StringBuilder layout = new StringBuilder();
            layout.Append("var layout = {title: '");
            layout.Append(locationName + " - " + datea.ToString("MM'/'dd'/'yyyy") + " - " + dateb.ToString("MM'/'dd'/'yyyy"));
            if (type.Equals("1"))
            {
                layout.Append("',xaxis:{tickangle: -45,title: 'Time'},yaxis: {title: 'Amount of people per mood'}};");
            }
            else if (type.Equals("2"))
            {
                layout.Append("',barmode: 'group',xaxis:{tickangle: -45,title: 'Time'},yaxis: {title: 'Amount of people per mood'}};");
            }
            else if (type.Equals("3"))
            {
                layout.Append("',barmode: 'stack',xaxis:{tickangle: -45,title: 'Time'},yaxis: {title: 'Amount of people per mood'}};");
            }

            StringBuilder script = new StringBuilder();
            script.Append(vgJs);
            script.Append(gJs);
            script.Append(bJs);
            script.Append(vbJs);
            script.Append("var data = [vg,g,b,vb];");
            script.Append(layout);
            script.Append("Plotly.newPlot(gd, data, layout);");
            return script.ToString();
        }

        private string GetEntriesForPieChart(string locationId, DateTime date)
        {
            SqlDataReader reader = null;
            Dictionary<string, string> data = new Dictionary<string, string>();
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                SqlCommand cmd;
                if (locationId.Equals("0"))
                {
                    cmd = new SqlCommand("SELECT count(e.mood),m.[Desc] FROM entries e left join mood m on e.mood = m.Id WHERE CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])), 101) = @date group by e.mood, m.[Desc], m.Id", Connection);
                    cmd.Parameters.AddWithValue("date", date);
                }
                else
                {
                    cmd = new SqlCommand("SELECT count(e.mood),m.[Desc] FROM entries e left join mood m on e.mood = m.Id WHERE CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])), 101) = @date and e.location = @location group by e.mood, m.[Desc], m.Id", Connection);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("location", locationId);
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting entries for date " + date.ToString("MM'/'dd'/'yyyy") + " for locationID " + locationId + "...");
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }
            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    data.Add(reader.GetString(1), reader.GetInt32(0).ToString());
                }
            }
            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");
            return ToPieChartScript(data);
        }

        private string GetEntriesForPieChartPeriod(string locationId, DateTime datea, DateTime dateb)
        {
            SqlDataReader reader = null;
            Dictionary<string, string> data = new Dictionary<string, string>();
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                SqlCommand cmd;
                if (locationId.Equals("0"))
                {
                    cmd = new SqlCommand("SELECT count(e.mood),m.[Desc] FROM entries e left join mood m on e.mood = m.Id WHERE CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])), 101) between @datea and @dateb group by e.mood, m.[Desc], m.Id", Connection);
                    cmd.Parameters.AddWithValue("datea", datea);
                    cmd.Parameters.AddWithValue("dateb", dateb);
                }
                else
                {
                    cmd = new SqlCommand("SELECT count(e.mood),m.[Desc] FROM entries e left join mood m on e.mood = m.Id WHERE CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])), 101) between @datea and @dateb and e.location = @location group by e.mood, m.[Desc], m.Id", Connection);
                    cmd.Parameters.AddWithValue("datea", datea);
                    cmd.Parameters.AddWithValue("dateb", dateb);
                    cmd.Parameters.AddWithValue("location", locationId);
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting entries for locationID " + locationId + " between " + datea.ToString("MM'/'dd'/'yyyy") + " and " + dateb.ToString("MM'/'dd'/'yyyy") +  "...");
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }
            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    data.Add(reader.GetString(1), reader.GetInt32(0).ToString());
                }
            }
            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");
            return ToPieChartScript(data);
        }

        private void GetAllEntriesBetweenDates(string locationId, DateTime datea, DateTime dateb, Dictionary<DateTime, Mood> dic)
        {
            SqlDataReader reader = null;
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                Console.WriteLine("Getting all entries for locationId " + locationId + " between " + datea.ToString("MM'/'dd'/'yyyy") + " and " + dateb.ToString("MM'/'dd'/'yyyy"));
                SqlCommand cmd;
                if (locationId.Equals("0"))
                {
                    cmd = new SqlCommand("SELECT CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101),mood,count(mood) FROM entries WHERE CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) between @datea and @dateb group by CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101), mood", Connection);
                    cmd.Parameters.AddWithValue("datea", datea);
                    cmd.Parameters.AddWithValue("dateb", dateb.AddDays(1).AddSeconds(-1));
                }
                else
                {
                    cmd = new SqlCommand("SELECT CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101),mood,count(mood) FROM entries WHERE location = @location and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) between @datea and @dateb group by CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101), mood", Connection);
                    cmd.Parameters.AddWithValue("datea", datea);
                    cmd.Parameters.AddWithValue("dateb", dateb.AddDays(1).AddSeconds(-1));
                    cmd.Parameters.AddWithValue("location", locationId);
                }
                reader = cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }

            if (reader != null)
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        DateTime dateFromDb = reader.GetDateTime(0);
                        int mood = reader.GetInt32(1);
                        int amount = reader.GetInt32(2);
                        Mood m = dic[dateFromDb];

                        switch (mood)
                        {
                            case 1:
                                m.VeryGood = amount;
                                break;
                            case 2:
                                m.Good = amount;
                                break;
                            case 3:
                                m.Bad = amount;
                                break;
                            case 4:
                                m.VeryBad = amount;
                                break;
                        }

                        dic.Remove(dateFromDb);
                        dic.Add(dateFromDb, m);
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }
            }
            else
            {
                Console.WriteLine("Something went wrong ! ");
            }
            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");
        }

        [Obsolete ("Use Method GetAllEntriesBetweenDates instead")]
        public string GetEntriesForMoodOnDay(string moodId, string location, DateTime day)
        {
            SqlDataReader reader = null;
            string amount = null;
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                SqlCommand cmd;
                if (location.Equals("0"))
                {
                    cmd = new SqlCommand("SELECT count(mood) FROM entries WHERE mood = @moodID and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) = @day", Connection);
                    cmd.Parameters.AddWithValue("moodID", moodId);
                    cmd.Parameters.AddWithValue("day", day);
                }
                else
                {
                    cmd = new SqlCommand("SELECT count(mood) FROM entries WHERE mood = @moodID and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) = @day and location = @location", Connection);
                    cmd.Parameters.AddWithValue("moodID", moodId);
                    cmd.Parameters.AddWithValue("day", day);
                    cmd.Parameters.AddWithValue("location", location);
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting amount of moods with mood id " + moodId + " for location " + location + " on " + day.ToString("MM'/'dd'/'yyyy") + "...");
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }

            if (reader != null)
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        amount = reader.GetInt32(0).ToString();
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }
            }
            else
            {
                Console.WriteLine("Something went wrong ! ");
            }

            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");
            return amount;
        }

        public string GetLocations()
        {
            SqlDataReader reader = null;
            try
            {
                Connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                var cmd = new SqlCommand("SELECT Id,location FROM locations", Connection);
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting locations... ");
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }

            List<Loc> loc = new List<Loc>();

            if (reader != null)
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        loc.Add(new Loc(reader.GetInt32(0), reader.GetString(1)));
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }
            }
            else
            {
                Console.WriteLine("Something went wrong ! ");
            }

            Connection.Close();
            Console.WriteLine("SQL Connection Closed ! \n");

            string json = null;
            try
            {
                json = JsonConvert.SerializeObject(loc, Formatting.Indented);
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
                Console.WriteLine("Something went wrong ! ");
            }
            return json;
        }

        private string ToPieChartScript(Dictionary<string,string> data)
        {
            StringBuilder script = new StringBuilder();
            script.Append("var data = [{");

            StringBuilder values = new StringBuilder();
            values.Append("values: [");
            StringBuilder labels = new StringBuilder();
            labels.Append("labels: [");

            foreach(KeyValuePair<string, string> entry in data)
            {
                values.Append(entry.Value + ",");
                labels.Append("'" + entry.Key + "',");
            }

            values.Length -= 1;
            labels.Length -= 1;

            values.Append("],");
            labels.Append("],");

            script.Append(values);
            script.Append(labels);
            script.Append("type: 'pie',marker:{colors:['rgba(44,160,44,1)', 'rgba(31,119,180,1)', 'rgba(255,127,14,1)', 'rgba(214,39,40,1)'] }}];");   
            script.Append("Plotly.newPlot(gd, data);");

            return script.ToString();
        }

        private string ToCoordinateString(List<string> coordAsList, string type)
        {
            StringBuilder xy = new StringBuilder();
            xy.Append(type);
            xy.Append(": [");
            bool set = false;
            foreach (string loc in coordAsList)
            {
                xy.Append(loc);
                xy.Append(",");
            }
            xy.Length -= 1;
            xy.Append("],");
            return xy.ToString();
        }
    }
}