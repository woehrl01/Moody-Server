using Nancy;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace MoodServer
{

    public class MoodModule : Nancy.NancyModule
    {

        private DBManager db;

        public MoodModule()
        {
            db = new DBManager();

            Get["/"] = _ =>
            {
                //TODO add chart page
                return "";
            };

            Get["/api/info"] = _ =>
            {
                return "<b>Moody by Ariel Simulevski</b><br>Server powered by <b>NancyFX</b><br>Client powered by <b>Xamarin</b>";
            };

            Get["/api/entry/{mood}&{location}"] = parameters =>
            {
                int mood = parameters.mood;
                int location = parameters.location;
                db.SaveMood(mood, location);
                return mood.ToString() + " " + location;
            };

            Get["/api/locations/"] = _ =>
            {
                return db.GetLocations();
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

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.NotFound;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
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
            connection.Close();
            Console.WriteLine("SQL Connection Closed ! ");

            String json = JsonConvert.SerializeObject(loc, Formatting.Indented);
            return json;
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
