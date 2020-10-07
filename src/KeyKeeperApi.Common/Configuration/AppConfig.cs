using System.Collections.Generic;

namespace KeyKeeperApi.Common.Configuration
{
    public class AppConfig
    {
        public DbConfig Db { get; set; }

        public AuthConfig Auth { get; set; }

        public RabbitMqConfig RabbitMq { get; set; }

        public VaultAgentConfig VaultAgent { get; set; }

        public MyNoSqlConfig MyNoSqlServer { get; set; }

        public FireBaseMessagingConfig FireBaseMessaging { get; set; }
    }

    public class FireBaseMessagingConfig
    {
        public string FireBasePrivateKeyJson { get; set; }
    }
}
