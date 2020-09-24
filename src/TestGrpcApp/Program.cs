using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using KeyKeeperApi.Grpc.tools;
using Swisschain.Sirius.GuardianValidatorApi;
using Swisschain.Sirius.ValidatorApi;

namespace TestGrpcApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestUntuthoriseRequest();
            //GeneratePublicKey();

            TestCreateAprovalRequest();
        }

        private static void TestCreateAprovalRequest()
        {
            Console.WriteLine("Press enter");
            Console.ReadLine();
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJPbmxpbmUgSldUIEJ1aWxkZXIiLCJpYXQiOjE2MDA5NTY1NTksImV4cCI6MTYzMjQ5MjU1NywiYXVkIjoic2lyaXVzLnN3aXNzY2hhaW4uaW8iLCJzdWIiOiIiLCJhcGkta2V5LWlkIjoia2V5LTEiLCJ2YXVsdC1pZCI6InZhdWx0LTEiLCJ0ZW5hbnQtaWQiOiJkZW1vIn0.asiDHrZyo6ha5Z8ga4rMLGvwkplEKYc6Tdz_T-brF9E");
                return Task.CompletedTask;
            });

            var channel = GrpcChannel.ForAddress("http://localhost:5001");
            

            var vaultApi = new Validators.ValidatorsClient(channel);
            var vlidatorApi = new Transfers.TransfersClient(channel);

            var request = new CreateApprovalRequestRequest()
            {
                TransferSigningRequestId = "req-3",
                RequestId = Guid.NewGuid().ToString("N"),
                ValidatorRequests =
                {
                    new CreateApprovalRequestRequest.Types.ValidatorRequest()
                    {
                        ValidaditorId = "alex",
                        MessageEnc = ByteString.CopyFrom(new byte[] {1, 2, 3, 4, 5}),
                        SecretEnc = ByteString.CopyFrom(new byte[] {6, 7, 8})
                    }
                }
            };

            vaultApi.CreateApprovalRequest(request, new CallOptions(credentials: credentials));


        }

        private static void GeneratePublicKey()
        {
            var service = new AsymmetricEncryptionService();
            var pair = service.GenerateKeyPairPem();
            Console.WriteLine(pair.Item2);
        }

        private static void TestUntuthoriseRequest()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress("http://localhost:5001");

            var api = new Transfers.TransfersClient(channel);


            try
            {
                var resp = api.GetApprovalRequests(new GetApprovalRequestsRequests());


                Console.WriteLine(resp.Payload.ToString());
            }
            catch (RpcException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
