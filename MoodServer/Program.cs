using Nancy.Hosting.Self;
using System;
using System.Globalization;
using System.Threading;

namespace MoodServer
{
    public class Program
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

        public static void Main(string[] args)
        {
            var p = new Program();
            p.Start();
        }
    }
}
