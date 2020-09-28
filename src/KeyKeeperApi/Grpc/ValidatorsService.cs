using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using KeyKeeperApi.Grpc.tools;
using KeyKeeperApi.MyNoSql;
using Microsoft.AspNetCore.Authorization;
using MyNoSqlServer.Abstractions;
using Swisschain.Sirius.GuardianValidatorApi;

namespace KeyKeeperApi.Grpc
{
    
    [Authorize]
    public class ValidatorsService  : Validators.ValidatorsBase
    {
        private readonly IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> _dataWriter;
        private readonly IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> _dataReader;

        public ValidatorsService(IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> dataWriter,
            IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> dataReader)
        {
            _dataWriter = dataWriter;
            _dataReader = dataReader;
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
            }

            return new AcknowledgeResultResponse();
        }
    }

    
}
