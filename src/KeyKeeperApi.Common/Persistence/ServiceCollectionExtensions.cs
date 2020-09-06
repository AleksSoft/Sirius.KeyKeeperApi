using KeyKeeperApi.Common.Persistence.Blockchains;
using KeyKeeperApi.Common.Persistence.TransactionApprovalRequests;
using KeyKeeperApi.Common.Persistence.Vaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KeyKeeperApi.Common.Persistence
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IBlockchainsRepository, BlockchainsRepository>();
            services.AddTransient<ITransactionApprovalRequestsRepository, TransactionApprovalRequestsRepository>();
            services.AddTransient<IVaultsRepository, VaultsRepository>();

            services.AddSingleton(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
                optionsBuilder
                    .UseLoggerFactory(x.GetRequiredService<ILoggerFactory>())
                    .UseNpgsql(connectionString,
                        builder => builder.MigrationsHistoryTable(
                            DatabaseContext.MigrationHistoryTable,
                            DatabaseContext.SchemaName));

                return optionsBuilder;
            });

            return services;
        }
    }
}
