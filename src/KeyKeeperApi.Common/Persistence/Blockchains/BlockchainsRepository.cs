using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using KeyKeeperApi.Common.ReadModels.Blockchains;

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

        public async Task AddOrUpdateAsync(Blockchain blockchain)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            try
            {
                context.Blockchains.Add(blockchain);
                await context.SaveChangesAsync();
            }
            catch (Exception exception) when (exception.InnerException is PostgresException pgException &&
                                              pgException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                context.Blockchains.Update(blockchain);

                await context.SaveChangesAsync();
            }
        }
    }
}
