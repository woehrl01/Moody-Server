using Nancy.Hosting.Self;
using System;
using System.Globalization;
using System.Threading;
using Exceptionless;

namespace MoodServer
{
    public class Program
    {
        private string _url = "http://localhost";
        private int _port = 80;
        

        private void Start()
        {
            Console.Clear();
            ExceptionlessClient.Default.Startup("3lSIuYT0NR6iXMffO7FIi46Ga5DJL8K3G1xmS2E0");
            Console.Title = "Mood Server Console";
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var uri = new Uri($"{_url}:{_port}/");
            using (var nancy = new NancyHost(uri))
            {
                try
                {
                    nancy.Start();
                    Console.WriteLine($"Started listennig port on {_port} \n");
                }
                catch (Exception e)
                {
                    e.ToExceptionless().Submit();
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
