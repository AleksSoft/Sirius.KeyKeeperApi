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
        private readonly ILogger<TransfersService> _logger;

        static TransfersService()
        {
            var algo = new SymmetricEncryptionService();
            _fakeRequestSecretAndNonce["-fake-0"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-1"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-2"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-3"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-4"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-5"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-6"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-7"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-8"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
            _fakeRequestSecretAndNonce["-fake-9"] = new KeyValuePair<byte[], byte[]>(algo.GenerateKey(), algo.GenerateNonce());
        }

        public TransfersService(IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> approvalRequestReader,
            IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> approvalRequestWriter,
            ILogger<TransfersService> logger)
        {
            _approvalRequestReader = approvalRequestReader;
            _approvalRequestWriter = approvalRequestWriter;
            _logger = logger;
        }

        private static Random _rnd = new Random();
        private static Dictionary<string, KeyValuePair<byte[], byte[]>> _fakeRequestSecretAndNonce = new Dictionary<string, KeyValuePair<byte[], byte[]>>();

        public override Task<GetApprovalRequestsResponse> GetApprovalRequests(GetApprovalRequestsRequests request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);
            var tenantId = context.GetHttpContext().User.GetTenantIdOrDefault();

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

            Console.WriteLine($"===============================");
            Console.WriteLine("Receive ResolveApprovalRequests:");
            Console.WriteLine($"{DateTime.UtcNow:s}");
            Console.WriteLine($"validatorId: {validatorId}");
            Console.WriteLine($"DeviceInfo: {request.DeviceInfo}");
            Console.WriteLine($"TransferSigningRequestId: {request.TransferSigningRequestId}");
            Console.WriteLine($"Signature: {request.Signature}");
            Console.WriteLine($"ResolutionDocumentEncBase64: {request.ResolutionDocumentEncBase64}");
            Console.WriteLine($"-------------------------------");

            if (request.TransferSigningRequestId.Contains("-fake-"))
            {
                Console.WriteLine($"detect fake ID");
                var strIndex = request.TransferSigningRequestId.Last();
                if (int.TryParse($"{strIndex}", out var index) && index >= 0 && index <= 9)
                {
                    Console.WriteLine($"detect ID = {index}");
                    var secret = _fakeRequestSecretAndNonce[$"-fake-{index}"].Key;
                    var nonce = _fakeRequestSecretAndNonce[$"-fake-{index}"].Value;

                    Console.WriteLine($"Secret: {Convert.ToBase64String(secret)}");
                    Console.WriteLine($"Nonce: {Convert.ToBase64String(nonce)}");


                    var dataEnc = Convert.FromBase64String(request.ResolutionDocumentEncBase64);
                    var symcrypto = new SymmetricEncryptionService();
                    var data = symcrypto.Decrypt(dataEnc, secret, nonce);

                    var json = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"Receive Resolution: {request.TransferSigningRequestId} from {validatorId}");
                    Console.WriteLine(json);

                    var publicKey = context.GetHttpContext().User.GetClaimOrDefault(Claims.PublicKeyPem);
                    if (!string.IsNullOrEmpty(publicKey))
                    {
                        var algo = new AsymmetricEncryptionService();
                        var verify = algo.VerifySignature(data, Convert.FromBase64String(request.Signature), publicKey);
                        Console.WriteLine($"Signature verification result: {verify.ToString().ToUpper()}");
                    }
                }
            }


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
