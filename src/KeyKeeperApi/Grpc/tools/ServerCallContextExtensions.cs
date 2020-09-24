using Grpc.Core;
using Swisschain.Sdk.Server.Authorization;

namespace KeyKeeperApi.Grpc.tools
{
    public static class ServerCallContextExtensions
    {
        public static long? GetApiKeyId(this ServerCallContext context)
        {
            var apiKeyIdClaim = context.GetHttpContext().User.GetClaimOrDefault(VaultClaims.ApiKeyId);

            if (string.IsNullOrEmpty(apiKeyIdClaim))
                return null;

            if (!long.TryParse(apiKeyIdClaim, out var apiKeyId))
                return null;

            return apiKeyId;
        }

        public static string GetTenantId(this ServerCallContext context)
        {
            return context.GetHttpContext().User.GetTenantIdOrDefault();
        }

        public static string GetVaultId(this ServerCallContext context)
        {
            var vaultIdClaim = context.GetHttpContext().User.GetClaimOrDefault(VaultClaims.VaultId);

            return vaultIdClaim;
        }

        public class VaultClaims
        {
            public const string ApiKeyId = "api-key-id";

            public const string VaultId = "vault-id";

            public const string VaultType = "vault-type";
        }
    }

    
}
