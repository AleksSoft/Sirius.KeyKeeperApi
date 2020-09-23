using System.Collections.Generic;

namespace KeyKeeperApi.Common.Configuration
{
    public class AppConfig
    {
        public DbConfig Db { get; set; }

        public AuthConfig Auth { get; set; }

        public RabbitMqConfig RabbitMq { get; set; }

        public VaultAgentConfig VaultAgent { get; set; }

        public TestKeys TestPubKeys { get; set; } = new TestKeys();
    }

    public class TestKeys : Dictionary<string, string>
    {

    }
}
