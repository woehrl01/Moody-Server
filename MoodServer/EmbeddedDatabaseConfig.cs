﻿namespace MoodServer
{
    class EmbeddedDatabaseConfig : IDatabaseConfig
    {
        public string ConnectionString { get; } =
            "Data Source=PROTEUSIV\\SQLEXPRESS;Initial Catalog=mood;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
    }
}
