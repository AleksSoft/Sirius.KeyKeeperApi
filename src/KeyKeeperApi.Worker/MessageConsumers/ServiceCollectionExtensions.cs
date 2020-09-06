using Microsoft.Extensions.DependencyInjection;

namespace KeyKeeperApi.Worker.MessageConsumers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageConsumers(this IServiceCollection services)
        {
            services.AddTransient<BlockchainUpdatesConsumer>();
            services.AddTransient<TransactionApprovalConfirmationAddedConsumer>();
            services.AddTransient<TransactionApprovalRequestAddedConsumer>();
            services.AddTransient<VaultUpdatedConsumer>();

            return services;
        }
    }
}
