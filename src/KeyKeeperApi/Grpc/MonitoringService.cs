

using System.Threading.Tasks;
using Grpc.Core;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sirius.KeyKeeperApi.ApiContract.Monitoring;

namespace KeyKeeperApi.Grpc
{
    public class MonitoringService : Swisschain.Sirius.KeyKeeperApi.ApiContract.Monitoring.Monitoring.MonitoringBase
    {
        public override Task<Swisschain.Sirius.KeyKeeperApi.ApiContract.Monitoring.IsAliveResponce> IsAlive(Swisschain.Sirius.KeyKeeperApi.ApiContract.Monitoring.IsAliveRequest request, ServerCallContext context)
        {
            var result = new Swisschain.Sirius.KeyKeeperApi.ApiContract.Monitoring.IsAliveResponce
            {
                Name = ApplicationInformation.AppName,
                Version = ApplicationInformation.AppVersion,
                StartedAt = ApplicationInformation.StartedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return Task.FromResult(result);
        }

            }
}
