using System;
using System.IO;
using System.Text;
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

            //TestCreateAprovalRequest();

            TestEncription();
        }

        private static void TestEncription()
        {
            var algo = new AsymmetricEncryptionService();

            var readerPrivate = new StreamReader("private-key-test.txt");
            var readerPublic = new StreamReader("public-key-test.txt");

            var privateStr = readerPrivate.ReadToEnd();
            var publicStr = readerPublic.ReadToEnd();

            var buf = Encoding.UTF8.GetBytes("Hello world!");

            var enc = algo.Encrypt(buf, publicStr);

            var str = Convert.ToBase64String(enc);
            Console.WriteLine();
            Console.WriteLine(str);
            Console.WriteLine();


            var res = algo.Decrypt(enc, privateStr);

            var txt = Encoding.UTF8.GetString(res);
            Console.WriteLine(txt);

            Console.ReadLine();
        }

        private static void TestCreateAprovalRequest()
        {
            Console.WriteLine("Press enter");
            Console.ReadLine();
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var credentials1 = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJPbmxpbmUgSldUIEJ1aWxkZXIiLCJpYXQiOjE2MDA5NTY1NTksImV4cCI6MTYzMjQ5MjU1NywiYXVkIjoic2lyaXVzLnN3aXNzY2hhaW4uaW8iLCJzdWIiOiIiLCJhcGkta2V5LWlkIjoia2V5LTEiLCJ2YXVsdC1pZCI6InZhdWx0LTEiLCJ0ZW5hbnQtaWQiOiJkZW1vIn0.asiDHrZyo6ha5Z8ga4rMLGvwkplEKYc6Tdz_T-brF9E");
                return Task.CompletedTask;
            });

            var credentials2 = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6ImFsZXgiLCJrZXkta2VlcGVyLWlkIjoiYWxleCIsIm5iZiI6MTYwMDk1NjI3MSwiZXhwIjoxNjMyNDkyMjcxLCJpYXQiOjE2MDA5NTYyNzEsImF1ZCI6InNpcml1cy5zd2lzc2NoYWluLmlvIn0.JKuYGQvXqvtlLM0Yd-3IcT1IN4fb6MFNyj2UH5zPFV8");
                return Task.CompletedTask;
            });

            var channel1 = GrpcChannel.ForAddress("https://sirius-validator-dev.swisschain.info:443", new GrpcChannelOptions()
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials1)
            });

            var channel2 = GrpcChannel.ForAddress("https://sirius-validator-dev.swisschain.info:443", new GrpcChannelOptions()
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials2)
            });


            var vaultApi = new Validators.ValidatorsClient(channel1);
            var vlidatorApi = new Transfers.TransfersClient(channel2);

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

            vaultApi.CreateApprovalRequest(request);


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
