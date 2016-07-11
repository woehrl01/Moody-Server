using Nancy;
using Nancy.TinyIoc;

namespace MoodServer
{
    public class MoodyBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<IDbManager, TestManager>();
            container.Register<IDatabaseConfig, EmbeddedDatabaseConfig>();
        }
    }
}