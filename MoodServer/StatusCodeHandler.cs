using System.IO;
using Nancy;
using Nancy.ErrorHandling;

namespace MoodServer
{
    public class StatusCodeHandler : IStatusCodeHandler
    {
        public IRootPathProvider RootPathProvider { get; }

        public StatusCodeHandler(IRootPathProvider rootPathProvider)
        {
            RootPathProvider = rootPathProvider;
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
}