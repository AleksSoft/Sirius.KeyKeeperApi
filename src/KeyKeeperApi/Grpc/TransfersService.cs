using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Consts;
using KeyKeeperApi.Grpc.tools;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Swisschain.Sdk.Server.Authorization;
using Swisschain.Sirius.KeyKeeperApi.ApiContract;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace KeyKeeperApi.Grpc
{
    [Authorize]
    public class TransfersService : Transfers.TransfersBase
    {
        private readonly TestKeys _testPubKeys;

        public TransfersService(TestKeys testPubKeys)
        {
            _testPubKeys = testPubKeys;
        }

        public override Task GetRequestToApprovalStream(RequestToApprovalRequests request,
            IServerStreamWriter<RequestToApprovalResponse> responseStream,
            ServerCallContext context)
        {
            return base.GetRequestToApprovalStream(request, responseStream, context);
        }

        private static Random _rnd = new Random();

        public override Task<RequestToApprovalResponse> GetRequestToApproval(RequestToApprovalRequests request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);

            if (!_testPubKeys.TryGetValue(validatorId, out var publicKey))
            {
                return Task.FromResult(new RequestToApprovalResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.InternalServerError,
                        Message = "Validator do not found by ID"
                    }
                });
            }


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
                    WithdrawalReferenceId = "account-"+_rnd.Next(1000000)
                }
            };
            transaction.BlockchainId = transaction.Asset.Symbol == "BTC" ? "Bitcoin" : "Ethereum";

            var json = JsonConvert.SerializeObject(transaction);

            var rta = new RequestToApprovalResponse.Types.RequestToApproval();

            var symcrypto = new SymmetricEncryptionService();
            var secret = symcrypto.GenerateKey();
            var message = symcrypto.Encrypt(Encoding.UTF8.GetBytes(json), secret);
            rta.TransactionDetailEnc = ByteString.CopyFrom(message);

            var asynccrypto = new AsymmetricEncryptionService();
            var secretEnc = asynccrypto.Encrypt(secret, publicKey);
            rta.SecretEnc = ByteString.CopyFrom(secretEnc);

            rta.Status = RequestToApprovalResponse.Types.RequestToApproval.Types.RequestStatus.Open;
            rta.TransferSigningRequestId = requestId;

            var res = new RequestToApprovalResponse();
            res.Payload.Add(rta);
            res.Payload.Add(new RequestToApprovalResponse.Types.RequestToApproval()
            {
                Status = RequestToApprovalResponse.Types.RequestToApproval.Types.RequestStatus.Open,
                TransferSigningRequestId = requestId + "-1",
                TransactionDetailEnc = ByteString.CopyFrom(message),
                SecretEnc = ByteString.CopyFrom(secretEnc)
            });
            res.Payload.Add(new RequestToApprovalResponse.Types.RequestToApproval()
            {
                Status = RequestToApprovalResponse.Types.RequestToApproval.Types.RequestStatus.Open,
                TransferSigningRequestId = requestId + "-2",
                TransactionDetailEnc = ByteString.CopyFrom(message),
                SecretEnc = ByteString.CopyFrom(secretEnc)
            });

            _requests[requestId] = json;
            _requests[requestId + "-1"] = json;
            _requests[requestId + "-2"] = json;

            return Task.FromResult(res);
        }

        private static Dictionary<string, string> _requests = new Dictionary<string, string>();

        public override Task<ResolveRequestToApprovalResponse> ResolveRequestToApproval(ResolveRequestToApprovalRequest request, ServerCallContext context)
        {
            if (!_requests.TryGetValue(request.TransferSigningRequestId, out var transaction))
            {
                return Task.FromResult(new ResolveRequestToApprovalResponse());
            }

            _requests.Remove(request.TransferSigningRequestId);

            var docObj = new
            {
                TransferSigningRequestId = request.TransferSigningRequestId,
                Resolution = request.Resolution,
                ResolutionMessage = request.ResolutionMessage,
                TransactionDetail = transaction
            };
            var doc = JsonSerializer.Serialize(docObj);

            //todo: check signature

            return Task.FromResult(new ResolveRequestToApprovalResponse());
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
