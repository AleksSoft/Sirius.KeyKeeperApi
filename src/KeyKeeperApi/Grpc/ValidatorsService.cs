using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using KeyKeeperApi.Grpc.tools;
using KeyKeeperApi.MyNoSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Swisschain.Sirius.GuardianValidatorApi;

namespace KeyKeeperApi.Grpc
{
    
    [Authorize]
    public class ValidatorsService  : Validators.ValidatorsBase
    {
        private readonly IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> _dataWriter;
        private readonly IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> _dataReader;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validatorLinkReader;
        private readonly ILogger<ValidatorsService> _logger;

        public ValidatorsService(
            IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> dataWriter,
            IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> dataReader,
            IMyNoSqlServerDataReader<ValidatorLinkEntity> validatorLinkReader,
            ILogger<ValidatorsService> logger)
        {
            _dataWriter = dataWriter;
            _dataReader = dataReader;
            _validatorLinkReader = validatorLinkReader;
            _logger = logger;
        }

        public override async Task<CreateApprovalRequestResponse> CreateApprovalRequest(CreateApprovalRequestRequest request, ServerCallContext context)
        {
            var tenantId = context.GetTenantId();
            var vaultId = context.GetVaultId();

            foreach (var validatorRequest in request.ValidatorRequests)
            {
                var entity = ApprovalRequestMyNoSqlEntity.Generate(validatorRequest.ValidaditorId, request.TransferSigningRequestId);
                entity.TenantId = tenantId;
                entity.MessageEnc = validatorRequest.TransactionDetailsEncBase64;
                entity.SecretEnc = validatorRequest.SecretEncBase64;
                entity.IvNonce = validatorRequest.IvNonce;
                entity.IsOpen = true;
                entity.VaultId = vaultId;

                await _dataWriter.InsertOrReplaceAsync(entity);

                _logger.LogInformation("CreateApprovalRequest processed. TransferSigningRequestId={TransferSigningRequestId}; TenantId={TenantId}; VaultId={VaultId}; ValidatorId={ValidatorId}", request.TransferSigningRequestId, tenantId, vaultId, validatorRequest.ValidaditorId);
            }

            //await _dataWriter.BulkInsertOrReplaceAsync(list, DataSynchronizationPeriod.Immediately);

            var resp = new CreateApprovalRequestResponse();

            return resp;
        }

        public override Task<GetApprovalResponse> GetApprovalResults(GetApprovalResultsRequest request, ServerCallContext context)
        {
            var tenantId = context.GetTenantId();
            var vaultId = context.GetVaultId()?.ToString();

            var list = _dataReader.Get()
                .Where(e => e.TenantId == tenantId && e.VaultId == vaultId)
                .Where(e => !e.IsOpen)
                .ToList();

            var resp = new GetApprovalResponse();

            foreach (var entity in list)
            {
                var item = new GetApprovalResponse.Types.ApprovalResponse()
                {
                    ValidatorId = entity.ValidatorId,
                    TransferSigningRequestId = entity.TransferSigningRequestId,
                    ResolutionDocumentEncBase64 = entity.ResolutionDocumentEncBase64,
                    Signature = entity.ResolutionSignature
                };
                
                resp.Payload.Add(item);

                _logger.LogInformation("GetApprovalResults response. TransferSigningRequestId={TransferSigningRequestId}; TenantId={TenantId}; VaultId={VaultId}; ValidatorId={ValidatorId}", item.TransferSigningRequestId, tenantId, vaultId, item.ValidatorId);
            }

            return Task.FromResult(resp);
        }

        public override async Task<AcknowledgeResultResponse> AcknowledgeResult(AcknowledgeResultRequest request, ServerCallContext context)
        {
            var vaultId = context.GetVaultId();

            var item = _dataReader.Get(ApprovalRequestMyNoSqlEntity.GeneratePartitionKey(request.ValidatorId),
                ApprovalRequestMyNoSqlEntity.GenerateRowKey(request.TransferSigningRequestId));

            if (item != null && item.VaultId == vaultId)
            {
                await _dataWriter.DeleteAsync(ApprovalRequestMyNoSqlEntity.GeneratePartitionKey(request.ValidatorId),
                    ApprovalRequestMyNoSqlEntity.GenerateRowKey(request.TransferSigningRequestId));

                _logger.LogInformation("Acknowledge ApprovalResults. TransferSigningRequestId={TransferSigningRequestId}; TenantId={TenantId}; VaultId={VaultId}; ValidatorId={ValidatorId}", item.TransferSigningRequestId, item.TenantId, item.VaultId, item.ValidatorId);
            }

            return new AcknowledgeResultResponse();
        }

        public override Task<ActiveValidatorsResponse> GetActiveValidators(ActiveValidatorsRequest request, ServerCallContext context)
        {
            var tenantId = context.GetTenantId();

            var listAll = _validatorLinkReader.Get()
                .Where(v => v.TenantId == tenantId)
                .Where(v => v.IsAccepted)
                .Where(v => !v.IsBlocked)
                .Select(v => new { v.ValidatorId, v.PublicKeyPem});

            var response = new ActiveValidatorsResponse();

            var hashset = new HashSet<string>();

            foreach (var item in listAll)
            {
                if (!hashset.Contains(item.ValidatorId))
                {
                    response.ActiveValidatorsRequest.Add(new ActiveValidatorsResponse.Types.ActiveValidator()
                    {
                        ValidatorId = item.ValidatorId,
                        ValidatorPublicKeyPem = item.PublicKeyPem
                    });
                    hashset.Add(item.ValidatorId);
                }
            }

            _logger.LogInformation("Return validator list TenantId={TenantId}; Count={Count}", tenantId, response.ActiveValidatorsRequest.Count);

            return Task.FromResult(response);
        }
    }

    
}
