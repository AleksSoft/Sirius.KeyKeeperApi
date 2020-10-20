using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Common.HostedServices;
using KeyKeeperApi.Common.Persistence;
using KeyKeeperApi.Grpc;
using KeyKeeperApi.MyNoSql;
using KeyKeeperApi.Services;
using Microsoft.AspNetCore.Routing;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sirius.VaultAgent.ApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;

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
            endpoints.MapGrpcService<ValidatorsService>();
            endpoints.MapGrpcService<VersionService>();
            
        }

        protected override void ConfigureContainerExt(ContainerBuilder builder)
        {
            builder.RegisterInstance(Config.Auth).AsSelf().SingleInstance();

            builder.RegisterType<PushNotificator>()
                .As<IPushNotificator>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterInstance(Config.FireBaseMessaging)
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MyNoSqlLifetimeManager>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            var noSqlClient = new MyNoSqlTcpClient(
                () => Config.MyNoSqlServer.ReaderServiceUrl,
                $"{ApplicationInformation.AppName}-{Environment.MachineName}");

            builder.Register(ctx => noSqlClient)
                .AsSelf()
                .As<IMyNoSqlSubscriber>()
                .SingleInstance();

            RegisterNoSqlReaderAndWriter<ApprovalRequestMyNoSqlEntity>(builder, ApprovalRequestMyNoSqlEntity.TableName);
            RegisterNoSqlReaderAndWriter<ValidatorLinkEntity>(builder, ValidatorLinkEntity.TableName);
            RegisterNoSqlReaderAndWriter<PingMessageMyNoSqlEntity>(builder, PingMessageMyNoSqlEntity.TableName);
            


        }

        private void RegisterNoSqlReaderAndWriter<TEntity>(ContainerBuilder builder, string table) where TEntity : IMyNoSqlDbEntity, new()
        {
            builder
                .Register(ctx => new MyNoSqlReadRepository<TEntity>(ctx.Resolve<IMyNoSqlSubscriber>(), table))
                .As<IMyNoSqlServerDataReader<TEntity>>()
                .SingleInstance();

            builder.Register(ctx =>
                {
                    return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<TEntity>(() => Config.MyNoSqlServer.WriterServiceUrl,
                        table);
                })
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}
