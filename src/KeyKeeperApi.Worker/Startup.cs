using System;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Common.HostedServices;
using KeyKeeperApi.Common.Persistence;
using KeyKeeperApi.Worker.MessageConsumers;
using Swisschain.Sdk.Server.Common;

namespace KeyKeeperApi.Worker
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            services
                .AddPersistence(Config.Db.ConnectionString)
                .AddHostedService<MigrationHost>()
                .AddMessageConsumers()
                .AddMassTransit(configurator =>
                {
                    configurator.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(factoryConfigurator =>
                    {
                        factoryConfigurator.Host(Config.RabbitMq.HostUrl,
                            host =>
                            {
                                host.Username(Config.RabbitMq.Username);
                                host.Password(Config.RabbitMq.Password);
                            });

                        factoryConfigurator.UseMessageRetry(retryConfigurator =>
                            retryConfigurator.Exponential(5,
                                TimeSpan.FromMilliseconds(100),
                                TimeSpan.FromMilliseconds(10_000),
                                TimeSpan.FromMilliseconds(100)));

                        factoryConfigurator.SetLoggerFactory(provider.Container.GetRequiredService<ILoggerFactory>());

                        factoryConfigurator.ReceiveEndpoint(
                            "sirius-key-keeper-api-blockchain-updates",
                            endpoint =>
                            {
                                endpoint.Consumer(provider.Container.GetRequiredService<BlockchainUpdatesConsumer>);
                            });

                        factoryConfigurator.ReceiveEndpoint(
                            "sirius-key-keeper-api-transaction-approval-confirmations-updates",
                            endpoint =>
                            {
                                endpoint.Consumer(provider.Container
                                    .GetRequiredService<TransactionApprovalConfirmationAddedConsumer>);
                            });

                        factoryConfigurator.ReceiveEndpoint(
                            "sirius-key-keeper-api-transaction-approval-request-updates",
                            endpoint =>
                            {
                                endpoint.Consumer(provider.Container
                                    .GetRequiredService<TransactionApprovalRequestAddedConsumer>);
                            });

                        factoryConfigurator.ReceiveEndpoint(
                            "sirius-key-keeper-api-vault-updates",
                            endpoint =>
                            {
                                endpoint.Consumer(provider.Container.GetRequiredService<VaultUpdatedConsumer>);
                            });
                    }));
                })
                .AddHostedService<BusHost>();
        }
    }
}
