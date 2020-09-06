using System;
using Swisschain.Sirius.Sdk.Primitives;

namespace KeyKeeperApi.Common.ReadModels.Blockchains
{
    public sealed class Blockchain
    {
        public string Id { get; set; }

        public string TenantId { get; set; }

        public string Name { get; set; }

        public Protocol Protocol { get; set; }

        public NetworkType NetworkType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
