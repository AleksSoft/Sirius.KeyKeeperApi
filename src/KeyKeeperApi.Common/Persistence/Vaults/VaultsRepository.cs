using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using KeyKeeperApi.Common.ReadModels.Vaults;

namespace KeyKeeperApi.Common.Persistence.Vaults
{
    public class VaultsRepository : IVaultsRepository
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public VaultsRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<Vault> GetByIdAsync(long vaultId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            return await context.Vaults.FindAsync(vaultId);
        }

        public async Task AddOrUpdateAsync(Vault vault)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            try
            {
                context.Vaults.Add(vault);
                await context.SaveChangesAsync();
            }
            catch (Exception exception) when (exception.InnerException is PostgresException pgException &&
                                              pgException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                context.Vaults.Update(vault);
                await context.SaveChangesAsync();
            }
        }
    }
}
