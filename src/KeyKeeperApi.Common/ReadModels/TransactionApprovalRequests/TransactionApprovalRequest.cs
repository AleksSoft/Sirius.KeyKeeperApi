using System;

namespace KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests
{
    public class TransactionApprovalRequest
    {
        public long Id { get; set; }

        public long KeyKeeperId { get; set; }

        public string TenantId { get; set; }

        public long VaultId { get; set; }

        public long TransactionSigningRequestId { get; set; }

        public string VaultName { get; set; }

        public string BlockchainId { get; set; }

        public string BlockchainName { get; set; }

        public TransactionApprovalRequestStatus Status { get; set; }

        public string Message { get; set; }

        public string Secret { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
