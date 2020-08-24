using Microsoft.Extensions.DependencyInjection;

namespace KeyKeeperApi.Worker.MessageConsumers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageConsumers(this IServiceCollection services)
        {
            services.AddTransient<TransactionApprovalRequestAddedConsumer>();

            return services;
        }
    }
}
