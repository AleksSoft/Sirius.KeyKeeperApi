using Swisschain.KeyKeeperApi.ApiClient.Common;
using Swisschain.Sirius.GuardianValidatorApi;
using Swisschain.Sirius.ValidatorApi;

namespace Swisschain.KeyKeeperApi.ApiClient
{
    public class ApiClient : BaseGrpcClient, IApiClient
    {
        public ApiClient(string serverGrpcUrl, string apiKey) : base(serverGrpcUrl, apiKey)
        {
            ValidatorsClient = new Validators.ValidatorsClient(CallInvoker);
            TransfersClient = new Transfers.TransfersClient(CallInvoker);
            InvitesClient = new Invites.InvitesClient(CallInvoker);
            VersionClient = new Version.VersionClient(CallInvoker);
        }

        public Validators.ValidatorsClient ValidatorsClient { get; }
        public Transfers.TransfersClient TransfersClient { get; }
        public Invites.InvitesClient InvitesClient { get; }
        public Version.VersionClient VersionClient { get; }
    }
}
