﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Common.HostedServices;
using KeyKeeperApi.Common.Persistence;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sirius.VaultAgent.ApiClient;

namespace KeyKeeperApi
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
            AddJwtAuth(Config.Auth.JwtSecret, Config.Auth.Audience);
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            services
                .AddHttpClient()
                .AddTransient<IVaultAgentClient>(factory => new VaultAgentClient(Config.VaultAgent.Url))
                .AddPersistence(Config.Db.ConnectionString)
                .AddHostedService<DbSchemaValidationHost>();
        }
    }
}