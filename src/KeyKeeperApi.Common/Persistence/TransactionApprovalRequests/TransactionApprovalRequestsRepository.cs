using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KeyKeeperApi.Common.Persistence.TransactionApprovalRequests
{
    public class TransactionApprovalRequestsRepository : ITransactionApprovalRequestsRepository
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public TransactionApprovalRequestsRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<TransactionApprovalRequest> GetOrDefaultAsync(long transactionApprovalRequestId,
            long keyKeeperId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            return await context.TransactionApprovalRequests
                .Where(entity => entity.Id == transactionApprovalRequestId && entity.KeyKeeperId == keyKeeperId)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<TransactionApprovalRequest>> GetByKeyKeeperIdAsync(long keyKeeperId,
            TransactionApprovalRequestStatus status)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            return await context.TransactionApprovalRequests
                .Where(entity => entity.KeyKeeperId == keyKeeperId && entity.Status == status)
                .ToListAsync();
        }

        public async Task InsertOrIgnoreAsync(IReadOnlyList<TransactionApprovalRequest> transactionApprovalRequests)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            try
            {
                context.TransactionApprovalRequests.AddRange(transactionApprovalRequests);
                await context.SaveChangesAsync();
            }
            catch (Exception exception) when (exception.InnerException is PostgresException pgException &&
                                              pgException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // ignore
            }
        }

        public async Task UpdateAsync(TransactionApprovalRequest transactionApprovalRequest)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            context.TransactionApprovalRequests.Update(transactionApprovalRequest);
            await context.SaveChangesAsync();
        }
    }
}
