using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using KeyKeeperApi.Common.ReadModels.Vaults;
using Z.EntityFramework.Plus;

namespace KeyKeeperApi.Common.Persistence.Vaults
{
    public class VaultsRepository : IVaultsRepository
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public VaultsRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<Vault> GetByIdOrDefaultAsync(long vaultId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            return await context.Vaults.FindAsync(vaultId);
        }

        public async Task InsertOrUpdateAsync(Vault vault)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var affectedRowsCount = await context.Vaults
                .Where(entity => entity.Id == vault.Id && entity.UpdatedAt <= vault.UpdatedAt)
                .UpdateAsync(x => new Vault
                {
                    Id = vault.Id,
                    Name = vault.Name,
                    Type = vault.Type,
                    Status = vault.Status,
                    TenantId = vault.TenantId,
                    CreatedAt = vault.CreatedAt,
                    UpdatedAt = vault.UpdatedAt
                });

            if (affectedRowsCount == 0)
            {
                try
                {
                    context.Vaults.Add(vault);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException exception) when (exception.InnerException is PostgresException pgException &&
                                                          pgException.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    // ignore
                }
            }
        }
    }
}
