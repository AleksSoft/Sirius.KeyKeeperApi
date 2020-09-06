using System;
using System.ComponentModel;
using System.Threading.Tasks;
using KeyKeeperApi.Common.Persistence.TransactionApprovalRequests;
using KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.VaultAgent.MessagingContract.TransactionApprovalConfirmations;

namespace KeyKeeperApi.Worker.MessageConsumers
{
    public class TransactionApprovalConfirmationAddedConsumer : IConsumer<TransactionApprovalConfirmationAdded>
    {
        private readonly ITransactionApprovalRequestsRepository _transactionApprovalRequestsRepository;
        private readonly ILogger<BlockchainUpdatesConsumer> _logger;

        public TransactionApprovalConfirmationAddedConsumer(
            ITransactionApprovalRequestsRepository transactionApprovalRequestsRepository,
            ILogger<BlockchainUpdatesConsumer> logger)
        {
            _transactionApprovalRequestsRepository = transactionApprovalRequestsRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TransactionApprovalConfirmationAdded> context)
        {
            var @event = context.Message;

            var transactionApprovalRequest = await _transactionApprovalRequestsRepository
                .GetOrDefaultAsync(@event.TransactionApprovalConfirmationId, @event.KeyKeeperId);

            if (transactionApprovalRequest == null)
            {
                throw new Exception(
                    $"Transaction approval request not found. Id: {@event.TransactionApprovalRequestId} KeyKeeperId: {@event.KeyKeeperId}");
            }

            transactionApprovalRequest.Status = @event.Status switch
            {
                TransactionApprovalConfirmationStatus.Confirmed => TransactionApprovalRequestStatus.Confirmed,
                TransactionApprovalConfirmationStatus.Rejected => TransactionApprovalRequestStatus.Rejected,
                TransactionApprovalConfirmationStatus.Skipped => TransactionApprovalRequestStatus.Skipped,
                _ => throw new InvalidEnumArgumentException(nameof(@event.Status),
                    (int) @event.Status,
                    typeof(TransactionApprovalConfirmationStatus))
            };

            await _transactionApprovalRequestsRepository.UpdateAsync(transactionApprovalRequest);

            _logger.LogInformation($"{nameof(TransactionApprovalConfirmationAdded)} has been processed {{@context}}",
                @event);
        }
    }
}
