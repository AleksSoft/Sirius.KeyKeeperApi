using Microsoft.AspNetCore.Mvc;

namespace KeyKeeperApi.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static string FormatRequestId(this ControllerBase controller, string tenantId, string requestId)
        {
            return $"KeyKeeperApi:{tenantId}:{requestId}";
        }
    }
}
