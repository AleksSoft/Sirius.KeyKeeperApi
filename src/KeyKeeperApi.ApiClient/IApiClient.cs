using Swisschain.Sirius.GuardianValidatorApi;
using Swisschain.Sirius.ValidatorApi;

namespace Swisschain.KeyKeeperApi.ApiClient
{
    public interface IApiClient
    {
        Validators.ValidatorsClient ValidatorsClient { get; }

        Transfers.TransfersClient TransfersClient { get; }

        Invites.InvitesClient InvitesClient { get; }

        Version.VersionClient VersionClient { get; }
    }
}
