using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using KeyKeeperApi.Common.ReadModels.Blockchains;
using Z.EntityFramework.Plus;

namespace KeyKeeperApi.Common.Persistence.Blockchains
{
    public class BlockchainsRepository : IBlockchainsRepository
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public BlockchainsRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<Blockchain> GetByIdAsync(string blockchainId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            return await context.Blockchains.FindAsync(blockchainId);
        }

        public async Task InsertOrUpdateAsync(Blockchain blockchain)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var affectedRowsCount = await context.Blockchains
                .Where(entity => entity.Id == blockchain.Id && entity.UpdatedAt <= blockchain.UpdatedAt)
                .UpdateAsync(entity => new Blockchain
                {
                    Id = blockchain.Id,
                    TenantId = blockchain.TenantId,
                    Name = blockchain.Name,
                    Protocol = blockchain.Protocol,
                    NetworkType = blockchain.NetworkType,
                    CreatedAt = blockchain.CreatedAt,
                    UpdatedAt = blockchain.UpdatedAt
                });

            if (affectedRowsCount == 0)
            {
                try
                {
                    context.Blockchains.Add(blockchain);
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
