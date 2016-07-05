using Nancy;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

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
                WebClient client = new WebClient();
                return client.DownloadString("views/dia.html") + "<script src='/api/diagramscript/" + loc + "&" + day + "&" + month + "&" + year + "'></script></html>";
            };

            Get["/api/diagramscript/{loc}&{day}&{month}&{year}"] = parameters =>
            {
                return db.MakeDiagramScript(parameters.loc,parameters.month + "/" + parameters.day + "/" + parameters.year);
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
            String guid = Guid.NewGuid().ToString(); ;

            String save = "INSERT into entries (Id,mood,location,date) VALUES (@id,@mood,@loc,@date)";

            using (SqlCommand command = new SqlCommand(save))
            {
                command.Connection = connection;
                command.Parameters.Add("@id", SqlDbType.VarChar, 36).Value = guid;
                command.Parameters.Add("@mood", SqlDbType.Int).Value = mood;
                command.Parameters.Add("@loc", SqlDbType.Int).Value = location;
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = localDate;

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

            if(reader != null)
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
            script += "$('#day_1').val('" + DateTime.Today.Day.ToString() + "');";
            script += "$('#month_1').val('" + DateTime.Today.Month.ToString() + "');";
            script += "$('#year_1').val('" + DateTime.Today.Year.ToString() + "');";
            return script;
        }

        public string MakeDiagramScript(string loc,string date)
        {
            string script = "$('#title').text('" + loc + " - " + date + "');" + GetEntries(GetIDByName(loc).ToString(), date) + "Plotly.newPlot('diagram', data);";
            return script;
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
                cmd = new SqlCommand("SELECT Id FROM locations WHERE location = '" + loc + "'", connection);
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

        public string GetEntries(string locationId, string date)
        {
            SqlCommand cmd;
            SqlDataReader reader = null;
            List<string> x = new List<string>();
            List<string> y = new List<string>();
            try
            {
                connection.Open();
                Console.WriteLine("SQL Connection Open ! ");
                cmd = new SqlCommand("select m.[Desc],count(e.mood) from mood m left join entries e on e.mood = m.Id and e.location =" + locationId + "and CONVERT(DATETIME, FLOOR(CONVERT(FLOAT, e.[date]))) = '" + date +"' group by  m.[Desc], mood, m.id order by m.Id asc", connection);
                reader = cmd.ExecuteReader();
                Console.WriteLine("Getting entries for date " + date + " for locationID " + locationId + "...");
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

            string data = "var data = [{" + ToCoordinateString(x, "x") + ToCoordinateString(y, "y") + "marker:{color: ['rgba(44,160,44,1)', 'rgba(31,119,180,1)', 'rgba(255,127,14,1)', 'rgba(214,39,40,1)']},type: 'bar'}];";
            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");
            return data;
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
}
