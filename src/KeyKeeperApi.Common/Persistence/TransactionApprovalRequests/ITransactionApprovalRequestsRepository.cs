using System.Collections.Generic;
using System.Threading.Tasks;
using KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests;

namespace KeyKeeperApi.Common.Persistence.TransactionApprovalRequests
{
    public interface ITransactionApprovalRequestsRepository
    {
        Task<TransactionApprovalRequest> GetOrDefaultAsync(long transactionApprovalRequestId,
            long keyKeeperId);

        Task<IReadOnlyList<TransactionApprovalRequest>> GetByKeyKeeperIdAsync(long keyKeeperId,
            TransactionApprovalRequestStatus status);

        Task InsertOrIgnoreAsync(IReadOnlyList<TransactionApprovalRequest> transactionApprovalRequests);

        Task UpdateAsync(TransactionApprovalRequest transactionApprovalRequest);
    }
}
