using System;
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
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Swisschain.Sdk.Server.Authorization;
using Swisschain.Sirius.ValidatorApi;

namespace KeyKeeperApi.Grpc
{
    [Authorize]
    public class TransfersService : Transfers.TransfersBase
    {
        private readonly TestKeys _testPubKeys;
        private readonly IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> _approvalRequestReader;
        private readonly IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> _approvalRequestWriter;

        public TransfersService(TestKeys testPubKeys, IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> approvalRequestReader,
            IMyNoSqlServerDataWriter<ApprovalRequestMyNoSqlEntity> approvalRequestWriter)
        {
            _testPubKeys = testPubKeys;
            _approvalRequestReader = approvalRequestReader;
            _approvalRequestWriter = approvalRequestWriter;
        }

        private static Random _rnd = new Random();

        public override Task<GetApprovalRequestsResponse> GetApprovalRequests(GetApprovalRequestsRequests request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);

            var res = new GetApprovalRequestsResponse();

            var requests = _approvalRequestReader.Get(ApprovalRequestMyNoSqlEntity.GeneratePartitionKey(validatorId))
                .Where(r => r.Resolution == ApprovalRequestMyNoSqlEntity.ResolutionType.Empty);
            //todo: add filter by tenant from API key, need to add tenant into api key

            foreach (var entity in requests)
            {
                var item = new GetApprovalRequestsResponse.Types.ApprovalRequest()
                {
                    TransferSigningRequestId = entity.TransferSigningRequestId,
                    Status = GetApprovalRequestsResponse.Types.ApprovalRequest.Types.RequestStatus.Open,
                    TransactionDetailsEncBase64 = Convert.ToBase64String(entity.MessageEnc),
                    SecretEncBase64 = Convert.ToBase64String(entity.SecretEnc)
                };
                res.Payload.Add(item);
            }

            // add fake data to test

            if (_testPubKeys.TryGetValue(validatorId, out var publicKey))
            {

                var requestId = Guid.NewGuid().ToString("N");
                var transaction = new TransactionDetails()
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    Amount = _rnd.Next(10).ToString(),
                    Asset = new TransactionDetails.AssetModel()
                    {
                        AssetAddress = string.Empty,
                        AssetId = string.Empty,
                        Symbol = _rnd.Next(10) > 5 ? "BTC" : "ETH"
                    },
                    NetworkType = _rnd.Next(10) > 5 ? "mainnet" : "testnet",
                    BlockchainId = string.Empty,
                    Source = new TransactionDetails.AddressData()
                    {
                        Address = Guid.NewGuid().ToString("N"),
                        AddressGroup = "Broker Account #1",
                        Name = string.Empty,
                        Tag = string.Empty,
                        TagType = string.Empty
                    },
                    Destination = new TransactionDetails.AddressData()
                    {
                        Address = Guid.NewGuid().ToString("N"),
                        AddressGroup = _rnd.Next(10) < 8 ? string.Empty : "Broker Account #2",
                        Name = string.Empty,
                        Tag = string.Empty,
                        TagType = string.Empty
                    },
                    FeeLimit = "0.001",
                    ClientContext = new TransactionDetails.ClientContextModel()
                    {
                        AccountReferenceId = _rnd.Next(1000000).ToString(),
                        ApiKeyId = "Api key #" + _rnd.Next(5),
                        IP = "172.164.20.2",
                        Timestamp = DateTime.UtcNow.ToString("O"),
                        UserId = _rnd.Next(100) < 80 ? string.Empty : "alexey.novichikhin",
                        WithdrawalReferenceId = "account-" + _rnd.Next(1000000)
                    }
                };
                transaction.BlockchainId = transaction.Asset.Symbol == "BTC" ? "Bitcoin" : "Ethereum";

                var json = JsonConvert.SerializeObject(transaction);

                var approvalRequest = new GetApprovalRequestsResponse.Types.ApprovalRequest();

                var symcrypto = new SymmetricEncryptionService();
                var secret = symcrypto.GenerateKey();
                var (message, nonce) = symcrypto.Encrypt(Encoding.UTF8.GetBytes(json), secret);
                approvalRequest.TransactionDetailsEncBase64 = Convert.ToBase64String(message);

                var asynccrypto = new AsymmetricEncryptionService();
                var secretEnc = asynccrypto.Encrypt(secret, publicKey);
                approvalRequest.SecretEncBase64 = Convert.ToBase64String(secretEnc);

                approvalRequest.IvNonce = Convert.ToBase64String(nonce);

                approvalRequest.Status = GetApprovalRequestsResponse.Types.ApprovalRequest.Types.RequestStatus.Open;
                approvalRequest.TransferSigningRequestId = requestId;


                res.Payload.Add(approvalRequest);
                res.Payload.Add(new GetApprovalRequestsResponse.Types.ApprovalRequest()
                {
                    Status = GetApprovalRequestsResponse.Types.ApprovalRequest.Types.RequestStatus.Open,
                    TransferSigningRequestId = requestId + "-1",
                    TransactionDetailsEncBase64 = Convert.ToBase64String(message),
                    SecretEncBase64 = Convert.ToBase64String(secretEnc),
                    IvNonce = Convert.ToBase64String(nonce)
                });
            }

            return Task.FromResult(res);
        }

        public override async Task<ResolveApprovalRequestsResponse> ResolveApprovalRequests(ResolveApprovalRequestsRequest request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);

            var approvalRequest = _approvalRequestReader.Get(
                    ApprovalRequestMyNoSqlEntity.GeneratePartitionKey(validatorId),
                    ApprovalRequestMyNoSqlEntity.GenerateRowKey(request.TransferSigningRequestId));

            if (approvalRequest == null || approvalRequest.Resolution != ApprovalRequestMyNoSqlEntity.ResolutionType.Empty)
            {
                return new ResolveApprovalRequestsResponse();
            }

            switch (request.Resolution)
            {
                case ResolveApprovalRequestsRequest.Types.ResolutionStatus.Approve:
                    approvalRequest.Resolution = ApprovalRequestMyNoSqlEntity.ResolutionType.Approve;
                    break;
                case ResolveApprovalRequestsRequest.Types.ResolutionStatus.Reject:
                    approvalRequest.Resolution = ApprovalRequestMyNoSqlEntity.ResolutionType.Reject;
                    break;
                case ResolveApprovalRequestsRequest.Types.ResolutionStatus.Skip:
                    approvalRequest.Resolution = ApprovalRequestMyNoSqlEntity.ResolutionType.Skip;
                    break;
            }

            approvalRequest.ResolutionMessage = request.ResolutionMessage;
            approvalRequest.ResolutionSignature = request.Signature;

            await _approvalRequestWriter.InsertOrReplaceAsync(approvalRequest);
            
            return new ResolveApprovalRequestsResponse();
        }


        public class TransactionDetails
        {
            public string OperationId { get; set; }
            public string BlockchainId { get; set; }
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
