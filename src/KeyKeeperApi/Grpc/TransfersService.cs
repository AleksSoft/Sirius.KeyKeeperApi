using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Consts;
using KeyKeeperApi.Grpc.tools;
using KeyKeeperApi.MyNoSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Swisschain.Sdk.Server.Authorization;
using Swisschain.Sirius.ValidatorApi;

namespace KeyKeeperApi.Grpc
{
    [Authorize]
    public class TransfersService : Transfers.TransfersBase
    {
        private readonly IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> _approvalRequestReader;
        private readonly IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> _approvalRequestWriter;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validatorLinkReader;
        private readonly ILogger<TransfersService> _logger;

        public TransfersService(IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> approvalRequestReader,
            IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> approvalRequestWriter,
            IMyNoSqlServerDataReader<ValidatorLinkEntity> validatorLinkReader,
            ILogger<TransfersService> logger)
        {
            _approvalRequestReader = approvalRequestReader;
            _approvalRequestWriter = approvalRequestWriter;
            _validatorLinkReader = validatorLinkReader;
            _logger = logger;
        }

        private static Random _rnd = new Random();
        private static Dictionary<string, KeyValuePair<byte[], byte[]>> _fakeRequestSecretAndNonce = new Dictionary<string, KeyValuePair<byte[], byte[]>>();

        public override Task<GetApprovalRequestsResponse> GetApprovalRequests(GetApprovalRequestsRequests request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);
            var tenantId = context.GetHttpContext().User.GetTenantIdOrDefault();
            var apiKeyId = context.GetHttpContext().User.GetClaimOrDefault(Claims.ApiKeyId);

            if (string.IsNullOrEmpty(tenantId))
            {
                return Task.FromResult(new GetApprovalRequestsResponse
                {
                    Error = new ValidatorApiError
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.Unknown,
                        Message = "Tenant Id required"
                    }
                });
            }

            var validatorLinkEntity = _validatorLinkReader.Get(
                ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(apiKeyId));

            if (validatorLinkEntity == null)
            {
                return Task.FromResult(new GetApprovalRequestsResponse
                {
                    Error = new ValidatorApiError
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.ExpiredApiKey,
                        Message = "API key is expired or deleted"
                    }
                });
            }

            var res = new GetApprovalRequestsResponse();

            var requests = _approvalRequestReader.Get(ApprovalRequestMyNoSqlEntity.GeneratePartitionKey(validatorId))
                .Where(r => r.IsOpen && r.TenantId == tenantId);
            //todo: add filter by tenant from API key, need to add tenant into api key

            foreach (var entity in requests)
            {
                var item = new GetApprovalRequestsResponse.Types.ApprovalRequest()
                {
                    TransferSigningRequestId = entity.TransferSigningRequestId,
                    Status = GetApprovalRequestsResponse.Types.ApprovalRequest.Types.RequestStatus.Open,
                    TransactionDetailsEncBase64 = entity.MessageEnc,
                    SecretEncBase64 = entity.SecretEnc,
                    IvNonce = entity.IvNonce
                };
                res.Payload.Add(item);
            }

            _logger.LogInformation("Return {Count} ApprovalRequests to ValidatorId='{ValidatorId}'", res.Payload.Count, validatorId);

            return Task.FromResult(res);
        }

        public override async Task<ResolveApprovalRequestsResponse> ResolveApprovalRequests(ResolveApprovalRequestsRequest request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);
            var tenantId = context.GetHttpContext().User.GetTenantIdOrDefault();
            var apiKeyId = context.GetHttpContext().User.GetClaimOrDefault(Claims.ApiKeyId);

            var validatorLinkEntity = _validatorLinkReader.Get(
                ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(apiKeyId));

            if (validatorLinkEntity == null)
            {
                return new ResolveApprovalRequestsResponse
                {
                    Error = new ValidatorApiError
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.ExpiredApiKey,
                        Message = "API key is expired or deleted"
                    }
                };
            }

            Console.WriteLine($"===============================");
            Console.WriteLine("Receive ResolveApprovalRequests:");
            Console.WriteLine($"{DateTime.UtcNow:s}");
            Console.WriteLine($"validatorId: {validatorId}");
            Console.WriteLine($"DeviceInfo: {request.DeviceInfo}");
            Console.WriteLine($"TransferSigningRequestId: {request.TransferSigningRequestId}");
            Console.WriteLine($"Signature: {request.Signature}");
            Console.WriteLine($"ResolutionDocumentEncBase64: {request.ResolutionDocumentEncBase64}");
            Console.WriteLine($"-------------------------------");


            var approvalRequest = _approvalRequestReader.Get(
                    ApprovalRequestMyNoSqlEntity.GeneratePartitionKey(validatorId),
                    ApprovalRequestMyNoSqlEntity.GenerateRowKey(request.TransferSigningRequestId));

            if (approvalRequest == null || !approvalRequest.IsOpen)
            {
                _logger.LogInformation("ResolveApprovalRequests skip because active request not found. TransferSigningRequestId={TransferSigningRequestId}; ValidatorId={ValidatorId}", request.TransferSigningRequestId, validatorId);
                return new ResolveApprovalRequestsResponse();
            }

            approvalRequest.ResolutionDocumentEncBase64 = request.ResolutionDocumentEncBase64;
            approvalRequest.ResolutionSignature = request.Signature;
            approvalRequest.IsOpen = false;

            await _approvalRequestWriter.InsertOrReplaceAsync(approvalRequest);

            _logger.LogInformation("ResolveApprovalRequests processed. TransferSigningRequestId={TransferSigningRequestId}; ValidatorId={ValidatorId}", request.TransferSigningRequestId, validatorId);

            return new ResolveApprovalRequestsResponse();
        }


        public class TransactionDetails
        {
            public string OperationId { get; set; }
            public string BlockchainId { get; set; }
            public string BlockchainProtocolId { get; set; }
            public string NetworkType { get; set; }
            public AssetModel Asset { get; set; }
            public AddressData Source { get; set; }
            public AddressData Destination { get; set; }
            public string Amount { get; set; }
            public string FeeLimit { get; set; }

            public ClientContextModel ClientContext { get; set; }


            public class ClientContextModel
            {
                public string WithdrawalReferenceId { get; set; }
                public string AccountReferenceId { get; set; }
                public string Timestamp { get; set; }
                public string UserId { get; set; }
                public string ApiKeyId { get; set; }
                public string IP { get; set; }
            }

            public class AssetModel
            {
                public string Symbol { get; set; }
                public string AssetAddress { get; set; }
                public string AssetId { get; set; }
            }

            public class AddressData
            {
                public string Address { get; set; }
                public string AddressGroup { get; set; }
                public string Name { get; set; }
                public string Tag { get; set; }
                public string TagType { get; set; }
            }
        }
    }

    
}
