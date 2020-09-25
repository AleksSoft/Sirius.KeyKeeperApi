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

            //TestEncription();

            TestEncriptionSync();
        }

        private static void TestEncriptionSync()
        {
            var algo = new SymmetricEncryptionService();

            var buf = Encoding.UTF8.GetBytes("Hello world!");

            var key = algo.GenerateKey();
            
            var (enc, nonce) = algo.Encrypt(buf, key);

            Console.WriteLine("-------");
            Console.WriteLine($"key: {Convert.ToBase64String(key)}");
            Console.WriteLine($"nonce: {Convert.ToBase64String(nonce)}");
            Console.WriteLine("-------");
            Console.WriteLine(Convert.ToBase64String(enc));

            var res = algo.Decrypt(enc, key, nonce);
            Console.WriteLine("-------");
            Console.WriteLine(Encoding.UTF8.GetString(res));
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


            var s64 =
                "Y1hLuvDfQ4Dr71zIBYwcOPIfpzIMo+RWpgaY3A51s4xS1NeHcrWMCfMr4qq8d0mQbotx4g0UXu8Y4yTZSuYMeMW8ezjzGpbzV8aikk1Skc72OnmUuwt8/ns/HVQmMwYumn0VlKGiJMKiFOUHROUBC3D1bAv1L363qnu1Vmiifn8=";
            var d1 = Convert.FromBase64String(s64);

            res = algo.Decrypt(enc, privateStr);

            txt = Convert.ToBase64String(res);
            Console.WriteLine(txt);


            Console.ReadLine();
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
