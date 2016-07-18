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
        

        private void Start(int port)
        {
            ExceptionlessClient.Default.Startup("3lSIuYT0NR6iXMffO7FIi46Ga5DJL8K3G1xmS2E0");
            Console.Title = "Mood Server Console";
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var uri = new Uri($"{_url}:{port}/");
            using (var nancy = new NancyHost(uri))
            {
                try
                {
                    nancy.Start();
                    Console.WriteLine($"Started listennig port on {port} \n");
                }
                catch (Exception e)
                {
                    e.ToExceptionless().Submit();
                    Console.WriteLine(e.Message);
                }
                Console.ReadKey();
            }
        }

        public static void Main()
        {
            Console.Clear();
            int port = 80;
            try
            {
                String[] arguments = Environment.GetCommandLineArgs();
                String argument = arguments[1];
                port = int.Parse(argument.Split('=')[1]);
            }
            catch (Exception)
            {
                Console.WriteLine("No port specified, listening on port 80! \n");
            }

            var p = new Program();
            p.Start(port);
        }
    }
}
