using Nancy;
using Nancy.TinyIoc;

namespace MoodServer
{
    public class MoodyBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<IDbManager, DbManager>();
            container.Register<IDatabaseConfig, ExternalDatabaseConfig>();
        }
    }
}