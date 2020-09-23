using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Common.HostedServices;
using KeyKeeperApi.Common.Persistence;
using KeyKeeperApi.Grpc;
using Microsoft.AspNetCore.Routing;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sirius.VaultAgent.ApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace KeyKeeperApi
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
            AddJwtAuth(Config.Auth.JwtSecret, Config.Auth.Audience);
        }

        protected override void ConfigureExt(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            services
                .AddHttpClient()
                .AddTransient<IVaultAgentClient>(factory => new VaultAgentClient(Config.VaultAgent.Url))
                .AddPersistence(Config.Db.ConnectionString)
                .AddHostedService<DbSchemaValidationHost>();
        }

        protected override void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGrpcService<MonitoringService>();
            endpoints.MapGrpcService<TransfersService>();
            endpoints.MapGrpcService<InvitesService>();
        }

        protected override void ConfigureContainerExt(ContainerBuilder builder)
        {
            builder.RegisterInstance(Config.TestPubKeys).AsSelf().SingleInstance();
            builder.RegisterInstance(Config.Auth).AsSelf().SingleInstance();
        }
    }
}
