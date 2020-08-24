using System;
using System.Threading.Tasks;
using KeyKeeperApi.Common.Persistence.Vaults;
using KeyKeeperApi.Common.ReadModels.Vaults;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.VaultAgent.MessagingContract.Vaults;
using VaultType = Swisschain.Sirius.VaultAgent.MessagingContract.Vaults.VaultType;

namespace KeyKeeperApi.Worker.MessageConsumers
{
    public class VaultUpdatedConsumer : IConsumer<VaultUpdated>
    {
        private readonly IVaultsRepository _vaultsRepository;
        private readonly ILogger<VaultUpdatedConsumer> _logger;

        public VaultUpdatedConsumer(
            IVaultsRepository vaultsRepository,
            ILogger<VaultUpdatedConsumer> logger)
        {
            _vaultsRepository = vaultsRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<VaultUpdated> context)
        {
            var @event = context.Message;

            var vault = new Vault
            {
                Id = @event.VaultId,
                TenantId = @event.TenantId,
                Name = @event.Name,
                Type = @event.Type switch
                {
                    VaultType.Private => Common.ReadModels.Vaults.VaultType.Private,
                    VaultType.Shared => Common.ReadModels.Vaults.VaultType.Shared,
                    _ => throw new ArgumentOutOfRangeException(nameof(@event.Type), @event.Type, null)
                },
                CreatedAt = @event.CreatedAt,
                UpdatedAt = @event.UpdatedAt
            };

            await _vaultsRepository.AddOrUpdateAsync(vault);

            _logger.LogInformation($"{nameof(VaultUpdated)} has been processed {{@context}}", @event);
        }
    }
}
