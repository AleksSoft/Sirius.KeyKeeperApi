using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeyKeeperApi.Common.Persistence.Blockchains;
using KeyKeeperApi.Common.Persistence.TransactionApprovalRequests;
using KeyKeeperApi.Common.Persistence.Vaults;
using KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.VaultAgent.MessagingContract.TransactionApprovalRequests;

namespace KeyKeeperApi.Worker.MessageConsumers
{
    public class TransactionApprovalRequestAddedConsumer : IConsumer<TransactionApprovalRequestAdded>
    {
        private readonly ITransactionApprovalRequestsRepository _transactionApprovalRequestsRepository;
        private readonly IVaultsRepository _vaultsRepository;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly ILogger<BlockchainUpdatesConsumer> _logger;

        public TransactionApprovalRequestAddedConsumer(
            ITransactionApprovalRequestsRepository transactionApprovalRequestsRepository,
            IVaultsRepository vaultsRepository,
            IBlockchainsRepository blockchainsRepository,
            ILogger<BlockchainUpdatesConsumer> logger)
        {
            _transactionApprovalRequestsRepository = transactionApprovalRequestsRepository;
            _vaultsRepository = vaultsRepository;
            _blockchainsRepository = blockchainsRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TransactionApprovalRequestAdded> context)
        {
            var @event = context.Message;

            var blockchain = await _blockchainsRepository.GetByIdAsync(@event.BlockchainId);

            if (blockchain == null)
                throw new Exception($"Blockchain not found. Id: {@event.BlockchainId}");

            var vault = await _vaultsRepository.GetByIdAsync(@event.VaultId);

            if (vault == null)
                throw new Exception($"Vault not found. Id: {@event.VaultId}");

            var transactionRequests = new List<TransactionApprovalRequest>();

            foreach (var keyKeeperRequest in @event.KeyKeeperRequests)
            {
                transactionRequests.Add(new TransactionApprovalRequest
                {
                    Id = @event.TransactionApprovalRequestId,
                    KeyKeeperId = keyKeeperRequest.KeyKeeperId,
                    TenantId = @event.TenantId,
                    VaultId = @event.VaultId,
                    TransactionSigningRequestId = @event.TransactionSigningRequestId,
                    VaultName = vault.Name,
                    BlockchainId = @event.BlockchainId,
                    BlockchainName = blockchain.Name,
                    Status = TransactionApprovalRequestStatus.Pending,
                    Message = keyKeeperRequest.Message,
                    Secret = keyKeeperRequest.Secret,
                    CreatedAt = @event.CreatedAt
                });
            }

            await _transactionApprovalRequestsRepository.InsertOrIgnoreAsync(transactionRequests);

            _logger.LogInformation($"{nameof(TransactionApprovalRequestAdded)} has been processed {{@context}}",
                @event);
        }
    }
}
