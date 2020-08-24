using System;

namespace KeyKeeperApi.Common.ReadModels.Vaults
{
    public class Vault
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public VaultType Type { get; set; }

        public VaultStatus Status { get; set; }

        public string TenantId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
