using System;
using MyNoSqlServer.Abstractions;

namespace KeyKeeperApi.MyNoSql
{
    public class ValidatorLinkEntity : IMyNoSqlDbEntity
    {
        public const string TableName = "validator-api-keys";

        public string ValidatorId { get; set; }

        public string PublicKeyPem { get; set; }

        public string InvitationToken { get; set; }

        public string TenantId { get; set; }

        public DateTime LastActivity { get; set; }

        public string ApiKeyId { get; set; }
        
        public bool IsBlocked { get; set; }

        public bool IsAccepted { get; set; }

        public string DeviceInfo { get; set; }

        public string Name { get; set; }

        public string Position { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedByAdminId { get; set; }
        public string CreatedByAdminEmail { get; set; }


        public static string GeneratePartitionKey(string tenantId) => tenantId;

        public static string GenerateRowKey(string apiKeyId) => apiKeyId;

        public static ValidatorLinkEntity Generate(string tenantId, string apiKeyId)
        {
            var entity = new ValidatorLinkEntity()
            {
                PartitionKey = GeneratePartitionKey(tenantId),
                RowKey = GenerateRowKey(apiKeyId),
                IsBlocked = false,
                IsAccepted = false,
                TenantId = tenantId,
                ApiKeyId = apiKeyId,
                LastActivity = DateTime.UtcNow
            };
            return entity;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
