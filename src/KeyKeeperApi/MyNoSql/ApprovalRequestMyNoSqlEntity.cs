using System;
using MyNoSqlServer.Abstractions;

namespace KeyKeeperApi.MyNoSql
{
    public class ApprovalRequestMyNoSqlEntity : IMyNoSqlDbEntity
    {
        public static string TableName = "validator-approval-request-3";

        public string ValidatorId { get; set; }

        public string TransferSigningRequestId { get; set; }

        public string SecretEnc { get; set; }

        public string MessageEnc { get; set; }

        public string IvNonce { get; set; }

        public string ResolutionDocumentEncBase64 { get; set; }

        public string ResolutionSignature { get; set; }

        public string VaultId { get; set; }

        public string TenantId { get; set; }

        public  bool IsOpen { get; set; }


        public static ApprovalRequestMyNoSqlEntity Generate(string validatorId, string transferSigningRequestId)
        {
            var entity = new ApprovalRequestMyNoSqlEntity()
            {
                PartitionKey = GeneratePartitionKey(validatorId),
                RowKey = GenerateRowKey(transferSigningRequestId),
                TransferSigningRequestId = transferSigningRequestId,
                ValidatorId = validatorId,
                IsOpen = true
            };
            return entity;
        }

        public static string GeneratePartitionKey(string validatorId) => validatorId;
        public static string GenerateRowKey(string transferSigningRequestId) => transferSigningRequestId;

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }

        public enum ResolutionType
        {
            Empty = 0,
            Approve = 1,
            Reject = 2,
            Skip = 3
        }
    }
}
