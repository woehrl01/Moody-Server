﻿using Nancy;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;

namespace MoodServer
{
    public class MoodModule : Nancy.NancyModule
    {

        private DBManager db;

        public MoodModule()
        {
            db = new DBManager();

            Get[@"/"] = parameters =>
            {
                WebClient client = new WebClient();
                return client.DownloadString("views/form.html");
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
                WebClient client = new WebClient();
                if(!dayA.Equals("0") && !monthA.Equals("0") && !yearA.Equals("0"))
                {
                    return client.DownloadString("views/dia.html") + db.MakeLineChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)), new DateTime(Int32.Parse(yearA), Int32.Parse(monthA), Int32.Parse(dayA)));    
                }
                else
                {
                    return client.DownloadString("views/dia.html") + db.MakeBarChartScript(loc, new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day))) + "</html>";
                }
            };

            Get["/api/locations/"] = _ =>
            {
                return db.GetLocations();
            };

            Get["/api/locationscript"] = _ =>
            {
                return db.MakeSelectScript();
            };

            Get["/api/info"] = _ =>
            {
                return "<b>Moody by Ariel Simulevski</b><br>Server powered by <b>NancyFX</b><br>Client powered by <b>Xamarin</b>";
            };

            Post["/api/entry"] = _ =>
            {
                int mood = Request.Form.mood;
                int location = Request.Form.location;
                db.SaveMood(mood, location);
                return "";
            };
        }
    }

    public class StatusCodeHandler : IStatusCodeHandler
    {
        private readonly IRootPathProvider _rootPathProvider;

        public StatusCodeHandler(IRootPathProvider rootPathProvider)
        {
            _rootPathProvider = rootPathProvider;
        }

        public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == Nancy.HttpStatusCode.NotFound;
        }

        public void Handle(Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            context.Response.Contents = stream =>
            {
                using (var file = File.OpenRead(new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory + "/views/404.html"))
                {
                    file.CopyTo(stream);
                }
            };
        }
    }

    class Program
    {
        private string _url = "http://localhost";
        private int _port = 80;
        

        private void Start()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var uri = new Uri($"{_url}:{_port}/");
            using (var nancy = new NancyHost(uri))
            {
                try
                {
                    nancy.Start();
                    Console.WriteLine($"Started listennig port on {_port}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Console.ReadKey();
            }
        }

        static void Main(string[] args)
        {
            var p = new Program();
            p.Start();
        }
    }

    class DBManager
    {
        public SqlConnection connection;

        public DBManager()
        {
            String connectionString = "Data Source=PROTEUSIV\\SQLEXPRESS;Initial Catalog=mood;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            connection = new SqlConnection(connectionString);

            try
            {
                connection.Open();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can not open connection to SQL Server ! ");
                Console.WriteLine(ex.Message);
            }
        }

        public void SaveMood(int mood, int location)
        {
            DateTime localDate = DateTime.Now;
            Console.WriteLine(localDate.ToString());
            string guid = Guid.NewGuid().ToString(); ;

            string save = "INSERT into entries (Id,mood,location,date) VALUES (@id,@mood,@loc,@date)";

            using (SqlCommand command = new SqlCommand(save))
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                command.Connection = connection;
                command.Parameters.Add("@id", SqlDbType.VarChar, 36).Value = guid;
                command.Parameters.Add("@mood", SqlDbType.Int).Value = mood;
                command.Parameters.Add("@loc", SqlDbType.Int).Value = location;
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = localDate.ToString();

                try
                {
                    connection.Open();
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
                    Console.WriteLine("Something went wrong ! ");
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    connection.Close();
                    Console.WriteLine("SQL Connection Closed ! ");
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

            string script = "var x = document.getElementById('locations');";

            foreach (string s in locs)
            {
                script += "var option = document.createElement('option');option.text ='" + s + "';x.add(option); ";
            }
            script += "document.getElementById('day_1').value = '" + DateTime.Today.Day.ToString() + "';";
            script += "document.getElementById('month_1').value = '" + DateTime.Today.Month.ToString() + "';";
            script += "document.getElementById('year_1').value = '" + DateTime.Today.Year.ToString() + "';";
            return script;
        }

        public string MakeBarChartScript(string loc,DateTime date)
        {
            return "<script>$('#title').text('" + loc + " - " + date.ToString("MM/dd/yyyy") + "');document.title = '" + loc + " - " + date.ToString("MM/dd/yyyy") + "';" + GetEntriesForBarChart(GetIDByName(loc).ToString(), date) + "</script>";
        }

        public string MakeLineChartScript(string loc, DateTime datea, DateTime dateb)
        {
            return "<script>" + GetEntriesForLineChart(GetIDByName(loc).ToString(), datea, dateb) + "$('#title').text('" + loc + " - " + datea.ToString("MM/dd/yyyy") + " - " + dateb.ToString("MM/dd/yyyy") + "');document.title = '" + loc + " - " + datea.ToString("MM/dd/yyyy") + "';" + "</script></body>";
        }

        public int GetIDByName(string loc)
        {
            SqlCommand cmd;
            SqlDataReader reader = null;
            int id = 0;
            try
            {
                connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                cmd = new SqlCommand("SELECT Id FROM locations WHERE location = @loc", connection);
                cmd.Parameters.AddWithValue("loc", loc);
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting locationID from location " + loc + "...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    id = reader.GetInt32(0);
                }
            }
            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");

            return id;
        }

        public string GetEntriesForBarChart(string locationId, DateTime date)
        {
            SqlCommand cmd;
            SqlDataReader reader = null;
            List<string> x = new List<string>();
            List<string> y = new List<string>();
            try
            {
                connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                if (locationId.Equals("0"))
                {
                    cmd = new SqlCommand("select m.[Desc],count(e.mood) from mood m left join entries e on e.mood = m.Id and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])),101) = @date group by  m.[Desc], mood, m.id order by m.Id asc", connection);
                    cmd.Parameters.AddWithValue("date", date.ToString("MM/dd/yyyy"));
                }
                else
                {
                    cmd = new SqlCommand("select m.[Desc],count(e.mood) from mood m left join entries e on e.mood = m.Id and e.location = @locationId and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date])),101) = @date group by  m.[Desc], mood, m.id order by m.Id asc", connection);
                    cmd.Parameters.AddWithValue("date", date.ToString("MM/dd/yyyy"));
                    cmd.Parameters.AddWithValue("locationId", locationId);
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting entries for date " + date.ToString("MM/dd/yyyy") + " for locationID " + locationId + "...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    x.Add("'" + reader.GetString(0) + "'");
                    y.Add(reader.GetInt32(1).ToString());
                    Console.WriteLine(reader.GetString(0) + " - " + reader.GetInt32(1));
                }
            }

            string data = "var data = [{" + ToCoordinateString(x, "x") + ToCoordinateString(y, "y") + "marker:{color: ['rgba(44,160,44,1)', 'rgba(31,119,180,1)', 'rgba(255,127,14,1)', 'rgba(214,39,40,1)']},type: 'bar'}];Plotly.newPlot('diagram', data);";
            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");
            return data;
        }

        public string GetEntriesForLineChart(string locationId, DateTime datea, DateTime dateb)
        {
            if(datea > dateb)
            {
                return "alert('First date must not be later than second date!');window.history.back();";
            }
            else if(datea == dateb)
            {
                return GetEntriesForBarChart(locationId, datea);
            }

            string xAxis = "x: [";

            var dates = new List<DateTime>();
            Dictionary<DateTime, Mood> dic = new Dictionary<DateTime, Mood>();
            for (var dt = datea; dt <= dateb; dt = dt.AddDays(1))
            {
                dates.Add(dt);
                dic.Add(dt, new Mood());
                xAxis += "'" + dt.ToString("MM/dd/yyyy") + "',";
            }

            xAxis = xAxis.Remove(xAxis.Length - 1);
            xAxis += "],";

            string vgJS = "var vg = {" + xAxis + "y: [";
            string gJS = "var g = {" + xAxis + "y: [";
            string bJS = "var b = {" + xAxis + "y: [";
            string vbJS = "var vb = {" + xAxis + "y: [";

            GetAllEntriesBetweenDates(locationId, datea, dateb, dic);

            foreach (DateTime d in dates)
            {
                vgJS += dic[d].VeryGood + ",";
                gJS += dic[d].Good + ",";
                bJS += dic[d].Bad + ",";
                vbJS += dic[d].VeryBad + ",";
            }

            vgJS = vgJS.Remove(vgJS.Length - 1);
            gJS = gJS.Remove(gJS.Length - 1);
            bJS = bJS.Remove(bJS.Length - 1);
            vbJS = vbJS.Remove(vbJS.Length - 1);

            vgJS += "],name: 'Very Good',type: 'scatter'};";
            gJS += "],name: 'Good',type: 'scatter'};";
            bJS += "],name: 'Bad',type: 'scatter'};";
            vbJS += "],name: 'Very Bad',type: 'scatter'};";

            string layout = "var layout = {xaxis:{tickangle: -45}};";
            return vgJS + gJS + bJS + vbJS + "var data = [vg,g,b,vb];" + layout + "Plotly.newPlot('diagram', data, layout);";
        }

        public void GetAllEntriesBetweenDates(string location, DateTime datea, DateTime dateb, Dictionary<DateTime, Mood> dic)
        {
            SqlCommand cmd;
            SqlDataReader reader = null;
            try
            {
                connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                if (location.Equals("0"))
                {
                    cmd = new SqlCommand("SELECT CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101),mood,count(mood) FROM entries WHERE CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) between @datea and @dateb group by CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101), mood", connection);
                    cmd.Parameters.AddWithValue("datea", datea);
                    cmd.Parameters.AddWithValue("dateb", dateb.AddDays(1).AddSeconds(-1));
                }
                else
                {
                    cmd = new SqlCommand("SELECT CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101),mood,count(mood) FROM entries WHERE location = @location and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) between @datea and @dateb group by CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101), mood", connection);
                    cmd.Parameters.AddWithValue("datea", datea);
                    cmd.Parameters.AddWithValue("dateb", dateb.AddDays(1).AddSeconds(-1));
                    cmd.Parameters.AddWithValue("location", location);
                }
                reader = cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong ! ");
                Console.WriteLine(e.Message);
            }

            if (reader != null)
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        DateTime dateFromDB = reader.GetDateTime(0);
                        int mood = reader.GetInt32(1);
                        int amount = reader.GetInt32(2);
                        Mood m = dic[dateFromDB];

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

                        dic.Remove(dateFromDB);
                        dic.Add(dateFromDB, m);
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
            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");
        }

        [Obsolete ("Use Method GetAllEntriesBetweenDates instead")]
        public string GetEntriesForMoodOnDay(string moodID, string location, DateTime day)
        {
            SqlCommand cmd;
            SqlDataReader reader = null;
            string amount = null;
            try
            {
                connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                if (location.Equals("0"))
                {
                    cmd = new SqlCommand("SELECT count(mood) FROM entries WHERE mood = @moodID and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) = @day", connection);
                    cmd.Parameters.AddWithValue("moodID", moodID);
                    cmd.Parameters.AddWithValue("day", day.ToString("MM/dd/yyyy"));
                }
                else
                {
                    cmd = new SqlCommand("SELECT count(mood) FROM entries WHERE mood = @moodID and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, [date])),101) = @day and location = @location", connection);
                    cmd.Parameters.AddWithValue("moodID", moodID);
                    cmd.Parameters.AddWithValue("day", day.ToString("MM/dd/yyyy"));
                    cmd.Parameters.AddWithValue("location", location);
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting amount of moods with mood id " + moodID + " for location " + location + " on " + day.ToString("MM/dd/yyyy") + "...");
            }
            catch (Exception e)
            {
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

            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");
            return amount;
        }

        public string GetLocations()
        {
            SqlCommand cmd;
            SqlDataReader reader = null;
            try
            {
                connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                cmd = new SqlCommand("SELECT Id,location FROM locations", connection);
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting locations... ");
            }
            catch (Exception e)
            {
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

            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");

            string json = null;
            try
            {
                json = JsonConvert.SerializeObject(loc, Formatting.Indented);
            }
            catch (Exception)
            {
                Console.WriteLine("Something went wrong ! ");
            }
            return json;
        }

        public string ToCoordinateString(List<string> coordAsList, string type)
        {
            string xy = type + ": [";
            bool set = false;
            foreach (string loc in coordAsList)
            {
                if (set)
                {
                    xy += "," + loc;
                }
                else
                {
                    xy += loc;
                    set = true;
                }
            }
            xy += "],";
            return xy;
        }
    }

    public class Loc
    {
        public Loc(int id, string location)
        {
            this.Identiefier = id;
            this.Location = location;
        }

        public string Location
        {
            get; set;
        }

        public int Identiefier
        {
            get; set;
        }
    }

    public class Mood
    {
        private int _veryGood = 0;
        private int _good = 0;
        private int _bad = 0;
        private int _veryBad = 0;

        public int VeryGood
        {
            get { return _veryGood; }
            set { _veryGood = value; }
        }
        public int Good
        {
            get { return _good; }
            set { _good = value; }
        }
        public int Bad
        {
            get { return _bad; }
            set { _bad = value; }
        }
        public int VeryBad
        {
            get { return _veryBad; }
            set { _veryBad = value; }
        }
    }
}
