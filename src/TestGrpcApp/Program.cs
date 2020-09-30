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

            //TestEncriptionSync();

            //TestSignature();
        }

        private static void TestSignature()
        {
            var readerPrivate = new StreamReader("private-key-test.txt");
            var readerPublic = new StreamReader("public-key-test.txt");

            var privateStr = readerPrivate.ReadToEnd();
            var publicStr = readerPublic.ReadToEnd();


            var data64 = "eyJUcmFuc2ZlckRldGFpbCI6eyJCbG9ja2NoYWluUHJvdG9jb2xJZCI6InByb3RvY29sLWlkIiwiT3BlcmF0aW9uSWQiOiJjNWY4OWE4NTI3MTE0N2M5YTBhYjFiYTZkY2FhZTVmYyIsIkJsb2NrY2hhaW5JZCI6IkV0aGVyZXVtIiwiTmV0d29ya1R5cGUiOiJ0ZXN0bmV0IiwiQXNzZXQiOnsiU3ltYm9sIjoiRVRIIiwiQXNzZXRBZGRyZXNzIjoiIiwiQXNzZXRJZCI6IiJ9LCJTb3VyY2UiOnsiQWRkcmVzcyI6ImZlNDM3ZWRiMTMyZDQ3YmE4ODdjMmM1ZWFjMzY5ZDRmIiwiQWRkcmVzc0dyb3VwIjoiQnJva2VyIEFjY291bnQgIzEiLCJOYW1lIjoiIiwiVGFnIjoiIiwiVGFnVHlwZSI6IiJ9LCJEZXN0aW5hdGlvbiI6eyJBZGRyZXNzIjoiMjU2NDI2NzJjZDdhNDQxMTk3M2ZkZTkxMjNiYTk1OWEiLCJBZGRyZXNzR3JvdXAiOiIiLCJOYW1lIjoiIiwiVGFnIjoiIiwiVGFnVHlwZSI6IiJ9LCJBbW91bnQiOiIxIiwiRmVlTGltaXQiOiIwLjAwMSIsIkNsaWVudENvbnRleHQiOnsiV2l0aGRyYXdhbFJlZmVyZW5jZUlkIjoiYWNjb3VudC03NDkyMTciLCJBY2NvdW50UmVmZXJlbmNlSWQiOiIxMjkxMSIsIlRpbWVzdGFtcCI6IjI4LjA5LjIwIDE0OjEzOjEwIiwiVXNlcklkIjoiIiwiQXBpS2V5SWQiOiJBcGkga2V5ICMzIiwiSVAiOiIxNzIuMTY0LjIwLjIifX0sIlJlc29sdXRpb24iOiJhcHByb3ZlIiwiUmVzb2x1dGlvbk1lc3NhZ2UiOiJhc2RmYXNkZiJ9";
            var data = Convert.FromBase64String(data64);


            var sign = Convert.FromBase64String("Kf5iSGE44iSX+UMp7lz3bYaYHE5Rpwav/wztVJHQ2E6pjNYt/muXumL7I/dyzBpWkJIGsvnrkOGMB2LiRf+M30CCWKtlZ8U0upSGz1kvlwEDP8dbpo74wcByKQy9h73dJtCuVeFbxZ/PixVUUg3IfNKfM7uExhSIIlNa4AU38Hg=");

            var algo = new AsymmetricEncryptionService();

            var verfy = algo.VerifySignature(data, sign,  publicStr);

            Console.WriteLine(verfy);

            Console.ReadLine();

        }

        private static void TestEncriptionSync()
        {
            var algo = new SymmetricEncryptionService();

            var buf = Encoding.UTF8.GetBytes("Hello world!");

            var key = algo.GenerateKey();
            var nonce = algo.GenerateNonce();
            
            var enc = algo.Encrypt(buf, key, nonce);

            Console.WriteLine("-------");
            Console.WriteLine($"key: {Convert.ToBase64String(key)}");
            Console.WriteLine($"nonce: {Convert.ToBase64String(nonce)}");
            Console.WriteLine("-------");
            Console.WriteLine(Convert.ToBase64String(enc));

            var res = algo.Decrypt(enc, key, nonce);
            Console.WriteLine("-------");
            Console.WriteLine(Encoding.UTF8.GetString(res));


            Console.WriteLine();
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=");
            Console.WriteLine();
            var d1 = "wqi+neXmkFks+eUpnfsV8bygfNnVxSZSQ7tQaQk7i1AfXJxCTwyh8/HibqVd8AhN/B6GLfgUzN5MyHuLjpQR8Dl3lW6w9MownocjTUKdzF9C0QtVBD1DHNOjHyicnMmLPne9mhDhmbJul5N7DHXGd+SHySkvn1MpvnSSJbP1/HT0DiVID2U9kQkuVdhbUvj2+TweGw4fP0OE4oQAPtazyB2SKn+fNiXf0SmcS9c5ngRazsKi+WDFIXOJUK3B26LigLlZadjBSW0sdjY0ExRIMC6+I0Dc0iBaEzPjG12iwKDKeqmT/RLvsoZCWx3x1aHYIs8zOhLz9GmH3ZktboXKuSA3nrjOig+6oMj8lF62MmNZRUxEHAilu6wj75Q7kE0zFWJCTMaiXS/nSw/58k2yDhgXA1ksbHQxJfhkqmNlM3hLiIAT50tt94UL6tOcWq8iK/ymb7xhyhv9VhobEkQbhhdmcJxm8mBDZXjsUEYaO0ntYLhAbBmkUwwlhnNfFEtVn9FK9PgpBzgZuYTHX1DZZ4MATvNqxUdR4sbPupHFPXGU9xZFz+K+U2SETbKYkR0fmfJKsVNd5QcoF9yrtDOdvUVhZaY6d68j6D69K5m6J9ePcWlL9xYyZnqwMkYhTV13vDcGA8eYF5IIgLCp2tLwwY4Q7I4POIMmhmgapVSk1PY/kA85Fd8KGUjAgcfdMkJrpqJ5194HuMP7rqFld8aOUqSBhLVs53V0TIpxtvdSCvfBq3Lum1+IrKiI4y+KGr4mPm9/Or2JsWLP6FQtAIo+BGp5qdmxyNs1n9a9P9MIQr6Nv8OryKM0flnEnb9DM9j/Lt93aoPzDQO4YpfImI2pfZUcI19IyNV+cfK/RyKFY0v8nXJiCCGRLS/H28ZfVpKtMqnRfQiVzvDzJPLeKiIF3fHfjdV9+u6R+ShnHEThjbXwns3Zw/38XQYsmhuSG4NB2Q9dULzZfZDA";
            
            key = Convert.FromBase64String("K7UOsz6j/sz/Sn/JJEQlJdyWzRUbnb9RlpG8umdXHDs=");
            nonce = Convert.FromBase64String("W5pOlo5BUNcc6lqpwynW8Q==");

            var d3 = Convert.FromBase64String(d1);
            res = algo.Decrypt(d3, key, nonce);
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
