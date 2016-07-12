using System;
using System.IO;
using Newtonsoft.Json;

namespace MoodServer
{
    class ExternalDatabaseConfig : IDatabaseConfig
    {
        public string connection;
        public string ConnectionString
        {
            get
            {
                try
                {
                    using (StreamReader sr = new StreamReader("cfg.json"))
                    {
                        string line = sr.ReadToEnd();
                        ExternalDatabaseConfig e = JsonConvert.DeserializeObject<ExternalDatabaseConfig>(line);
                        return e.connection;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not load the configuration");
                }
                return "";
            }
        }
    }
}
