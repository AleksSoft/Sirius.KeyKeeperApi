using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KeyKeeperApi.Common.Persistence.TransactionApprovalRequests;
using KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests;
using KeyKeeperApi.Consts;
using KeyKeeperApi.Extensions;
using KeyKeeperApi.WebApi.Models.TransactionApprovalRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swisschain.Sdk.Server.Authorization;
using Swisschain.Sirius.VaultAgent.ApiClient;
using Swisschain.Sirius.VaultAgent.ApiContract.TransactionApprovalRequests;

namespace KeyKeeperApi.WebApi
{
    [Authorize]
    [ApiController]
    [Route("/api/transaction-approval-requests")]
    public class TransactionApprovalRequestsController : ControllerBase
    {
        private readonly IVaultAgentClient _vaultAgentClient;
        private readonly ITransactionApprovalRequestsRepository _transactionApprovalRequestsRepository;

        public TransactionApprovalRequestsController(IVaultAgentClient vaultAgentClient,
            ITransactionApprovalRequestsRepository transactionApprovalRequestsRepository)
        {
            _vaultAgentClient = vaultAgentClient;
            _transactionApprovalRequestsRepository = transactionApprovalRequestsRepository;
        }

        /// <summary>
        /// Returns key keeper pending transaction approval requests.
        /// </summary>
        /// <remarks>
        /// Allows key keeper to get pending transaction approval requests.
        /// </remarks>
        /// <returns>
        /// A collection of transaction approval requests associated with key keeper.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(TransactionApprovalRequestResponse[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<TransactionApprovalRequestResponse[]>> GetAsync()
        {
            var keyKeeperIdClaim = User.GetClaimOrDefault(Claims.KeyKeeperId);

            if (!long.TryParse(keyKeeperIdClaim, out var keyKeeperId))
            {
                ModelState.AddFormattedModelError("", "Key keeper id is not presented in claims.");
                return StatusCode(StatusCodes.Status403Forbidden, ModelState);
            }

            var requests = await _transactionApprovalRequestsRepository.GetByKeyKeeperIdAsync(
                keyKeeperId,
                TransactionApprovalRequestStatus.Pending);

            var response = requests
                .Select(request => new TransactionApprovalRequestResponse
                {
                    Id = request.Id,
                    VaultId = request.VaultId,
                    VaultName = request.VaultName,
                    BlockchainId = request.BlockchainId,
                    BlockchainName = request.BlockchainName,
                    Message = request.Message,
                    Secret = request.Secret,
                    CreatedAt = request.CreatedAt.UtcDateTime
                })
                .ToList();

            return Ok(response);
        }

        /// <summary>
        /// Confirms transaction approval request.
        /// </summary>
        /// <remarks>
        /// Allows key keeper to confirm transaction approval request.
        /// </remarks>
        /// <param name="requestId">
        /// Should be system-wide unique for each action that you want do. Guid fits well for this.
        /// If you repeat request with the same <paramref name="requestId"/> you'll get the same result.
        /// </param>
        /// <param name="request">
        /// The transaction approval request confirmation parameters. 
        /// </param>
        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ConfirmAsync([Required, FromHeader(Name = "X-Request-ID")]
            string requestId,
            [FromBody] TransactionApprovalConfirmationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = User.GetTenantId();

            var formatRequestId = this.FormatRequestId(tenantId, requestId);

            var keyKeeperIdClaim = User.GetClaimOrDefault(Claims.KeyKeeperId);

            if (!long.TryParse(keyKeeperIdClaim, out var keyKeeperId))
            {
                ModelState.AddFormattedModelError("", "Key keeper id is not presented in claims.");
                return StatusCode(StatusCodes.Status403Forbidden, ModelState);
            }

            var confirmRequest = new ConfirmTransactionApprovalRequestRequest
            {
                RequestId = formatRequestId,
                TenantId = tenantId,
                TransactionApprovalRequestId = request.TransactionApprovalRequestId,
                KeyKeeperId = keyKeeperId,
                Message = request.Message,
                Secret = request.Secret
            };

            var confirmResponse = await _vaultAgentClient.TransactionApprovalRequests.ConfirmAsync(confirmRequest);

            if (confirmResponse.BodyCase == ConfirmTransactionApprovalRequestResponse.BodyOneofCase.Error)
            {
                ModelState.AddFormattedModelError("", confirmResponse.Error.ErrorMessage);
                return BadRequest(ModelState);
            }

            return NoContent();
        }

        /// <summary>
        /// Rejects transaction approval request.
        /// </summary>
        /// <remarks>
        /// Allows key keeper to reject transaction approval request.
        /// </remarks>
        /// <param name="requestId">
        /// Should be system-wide unique for each action that you want do. Guid fits well for this.
        /// If you repeat request with the same <paramref name="requestId"/> you'll get the same result.
        /// </param>
        /// <param name="request">
        /// The transaction approval request reject parameters. 
        /// </param>
        [HttpPost("reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RejectAsync([Required, FromHeader(Name = "X-Request-ID")]
            string requestId,
            [FromBody] TransactionApprovalRejectRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = User.GetTenantId();

            var formatRequestId = this.FormatRequestId(tenantId, requestId);

            var keyKeeperIdClaim = User.GetClaimOrDefault(Claims.KeyKeeperId);

            if (!long.TryParse(keyKeeperIdClaim, out var keyKeeperId))
            {
                ModelState.AddFormattedModelError("", "Key keeper id is not presented in claims.");
                return StatusCode(StatusCodes.Status403Forbidden, ModelState);
            }

            var rejectRequest = new RejectTransactionApprovalRequestRequest
            {
                RequestId = formatRequestId,
                TenantId = tenantId,
                KeyKeeperId = keyKeeperId,
                TransactionApprovalRequestId = request.TransactionApprovalRequestId
            };

            var rejectResponse = await _vaultAgentClient.TransactionApprovalRequests.RejectAsync(rejectRequest);

            if (rejectResponse.BodyCase == RejectTransactionApprovalRequestResponse.BodyOneofCase.Error)
            {
                ModelState.AddFormattedModelError("", rejectResponse.Error.ErrorMessage);
                return BadRequest(ModelState);
            }

            return NoContent();
        }

        /// <summary>
        /// Skips transaction approval request.
        /// </summary>
        /// <remarks>
        /// Allows key keeper to skip transaction approval request.
        /// </remarks>
        /// <param name="requestId">
        /// Should be system-wide unique for each action that you want do. Guid fits well for this.
        /// If you repeat request with the same <paramref name="requestId"/> you'll get the same result.
        /// </param>
        /// <param name="request">
        /// The transaction approval request skip parameters. 
        /// </param>
        [HttpPost("skip")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SkipAsync([Required, FromHeader(Name = "X-Request-ID")]
            string requestId,
            [FromBody] TransactionApprovalSkipRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = User.GetTenantId();

            var formatRequestId = this.FormatRequestId(tenantId, requestId);

            var keyKeeperIdClaim = User.GetClaimOrDefault(Claims.KeyKeeperId);

            if (!long.TryParse(keyKeeperIdClaim, out var keyKeeperId))
            {
                ModelState.AddFormattedModelError("", "Key keeper id is not presented in claims.");
                return StatusCode(StatusCodes.Status403Forbidden, ModelState);
            }

            var skipRequest = new SkipTransactionApprovalRequestRequest
            {
                RequestId = formatRequestId,
                TenantId = tenantId,
                KeyKeeperId = keyKeeperId,
                TransactionApprovalRequestId = request.TransactionApprovalRequestId
            };

            var skipResponse = await _vaultAgentClient.TransactionApprovalRequests.SkipAsync(skipRequest);

            if (skipResponse.BodyCase == SkipTransactionApprovalRequestResponse.BodyOneofCase.Error)
            {
                ModelState.AddFormattedModelError("", skipResponse.Error.ErrorMessage);
                return BadRequest(ModelState);
            }

            return NoContent();
        }
    }
}
