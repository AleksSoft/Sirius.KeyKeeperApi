using System;

namespace KeyKeeperApi.WebApi.Models.TransactionApprovalRequests
{
    public class TransactionApprovalRequestResponse
    {
        public long Id { get; set; }

        public long VaultId { get; set; }

        public string VaultName { get; set; }

        public string BlockchainId { get; set; }

        public string BlockchainName { get; set; }

        public string Message { get; set; }

        public string Secret { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
