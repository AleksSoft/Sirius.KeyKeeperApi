using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using KeyKeeperApi.Grpc.tools;
using Newtonsoft.Json;
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

            TestEncription3();

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
            var publicStrJson = readerPublic.ReadToEnd();

            var publicStr = JsonConvert.DeserializeObject<KeyJson>(publicStrJson).Key;

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

        private static void TestEncription2()
        {
            var algo = new AsymmetricEncryptionService();

            var readerPrivate = new StreamReader("private-key-test.txt");
            var readerPublic = new StreamReader("public-key-test.txt");

            var privateStr = readerPrivate.ReadToEnd();
            var publicStrJson = readerPublic.ReadToEnd();

            var publicStr = JsonConvert.DeserializeObject<KeyJson>(publicStrJson).Key;

            var service = new SymmetricEncryptionService();
            var buf = service.GenerateKey();

            var enc = algo.Encrypt(buf, publicStr);

            var str = Convert.ToBase64String(enc);
            Console.WriteLine(Convert.ToBase64String(buf));
            Console.WriteLine();
            Console.WriteLine(str);
            Console.WriteLine();

            enc = Convert.FromBase64String("ov0P6xO2AXxLJe7DgMmfjXjCPC488wAElzbTVy+N7/6Q4g0ld6iRnuXtI2FN06ym/loHSHH7sy9375xPoHy3bDp1jCcs1FrClnynlWUO5c2Xq5M1mQBYlcg3u5OG4wgttHtXa9/cftm1B9hl/Nh9ItSwKI/br61dlT+gebZzOO0=");

            algo = new AsymmetricEncryptionService();
            var res = algo.Decrypt(enc, privateStr);
            Console.WriteLine(Convert.ToBase64String(res));


            Console.ReadLine();
        }

        private static void TestEncription3()
        {
            var secret = Convert.FromBase64String("mqxcq/9r6borrdB2qOw1LX0ayKV/L3VzqcxvV0uNeBs=");
            var nonce = Convert.FromBase64String("Qd5MOgX5LK2qlTret20hLQ==");
            var enc = Convert.FromBase64String("FoWu3qjUSPwo6/GlFesy+34hQf6eQrGUfhCigqJH1EsQVS406uhukyYZ0qkfF5upuNTQ52HPn3EbQhCTVOB5rkPlhfCHr0AmGDhLvKTg+ltWwMN+TTQALF+OoR2Lep4s7yDNaVO3rBZyr39l9fqJtqSV412YwMnRuv8hsszA94C7MUoKmgT2vpxU7+phoKPPvg155FcVx+YaSowT8/wXj9Mh29svgmfNm6z0w/WVguoRps+D5ovS4s2/ZI4lRXi7+qZ0VRPJ1Bd8p7dGvKxRRSJS284aiBXYkhb6BIDgnbIFWsbb6r82/AFMN0ckEa+221HYLVyQK6XBaLBchHkLkzI3j3rtKnw3+CtjKeuROF+W5Q73Om2C5h1KWO0xa22mUdfbYuPQvg8dg4KtpHKkr/xvM1tP5oYnl1xayVx/NXHcR3RC68iT12fptdJRpg/zfOGxXuTyfmxC7oPxB3ViihDWIWdaJyUl3TP32eeNe1Js0UPXtd7qk9jDZlF6gSQYKPvEcQDhczMQjYlpHYEiQImedweC7+AlwVro6ZcRF3beyjK9F8Uvj47nuJ9ooDA5bW3XS6Y022/E1qfJxrtYC8auVyTCOaYBcv5OL/bPpqAXgOU1/IbqrmdSCkNi7vOK4DJIfEdwrtVvuBid0yNr1yJVqSigjSBj0UjjFrpCb6/BpbNgfJMi4qbns224PC5iXF0402p6iaRUI3T/8bKfWgkcENbMOO1/9Q3+q7FuKBaaRSJyEOX+uq/WQtPhtYppqffDT4mb2hbyqeFAZmgXXV5vxJU5BGpYGrBXXh2PHePusCF707sjRh72JQzys3cJmXbBc8j7trzu0mVTgFqHMU5QxJqT7JsL1yuzW/26Pl9B1ImflUXUSDcqJE9fqGDfA6q64B0qmK2NjvAg4OQB32Ym1cinETRZetxneSLBSegt385E0cA2zuecS7b3ONNoM02M3WcYzyaLYb1mJXWz6g==");


            var service = new SymmetricEncryptionService();
            var data = service.Decrypt(enc, secret, nonce);

            //data = Convert.FromBase64String("eyJhbW91bnQiOiIzLjk4MTIiLCJhc3NldCI6eyJhc3NldEFkZHJlc3MiOiIweDNBOUJDNDIwYTQyRDQzODZEMUE4NENDNTYwZTczMjQ3NzlEODY3MzQiLCJhc3NldElkIjoiMTAwMDAxIiwic3ltYm9sIjoiRVRIIn0sImJsb2NrY2hhaW5JZCI6ImV0aGVyZXVtLXJvcHN0ZW4iLCJibG9ja2NoYWluUHJvdG9jb2xJZCI6ImV0aGVyZXVtIiwiY2xpZW50Q29udGV4dCI6eyJ1c2VySWQiOiIxMDAwMDQ1IiwiYXBpS2V5SWQiOiI0MDAwNzYyIiwiYWNjb3VudFJlZmVyZW5jZUlkIjoiTXIuIFdoaXRlIiwid2l0aGRyYXdhbFJlZmVyZW5jZUlkIjoiTXIuIFJlZCIsImlwIjoiMTAuMC4yNS4xNzkiLCJ0aW1lc3RhbXAiOiIyMDIwLTA5LTMwVDE0OjA3OjA3Ljc3Mzg3NzQwMFoifSwiZGVzdGluYXRpb24iOnsiYWRkcmVzcyI6IjB4MUE5QkM0MjBhNDJENDM4NkQxQTg0Q0M1NjBlNzMyNDc3OUQ4NjczNCIsIm5hbWUiOiJObyBuYW1lIiwiZ3JvdXAiOiIxMDAwNDU3IiwidGFnIjoidGhpcyBpcyBhIHRleHQgdGFnIHZhbHVlIiwidGFnVHlwZSI6InRleHQifSwiZmVlTGltaXQiOiIwLjY1NyIsIm5ldHdvcmtUeXBlIjoidGVzdCIsIm9wZXJhdGlvbklkIjoiRDJCNUI3RTUtMTVDRi00NEM4LThDM0UtMzYxQjQyMURFNjcxIiwic291cmNlIjp7ImFkZHJlc3MiOiIweDRBOUJDNDIwYTQyRDQzODZEMUE4NENDNTYwZTczMjQ3NzlEODY3MzQiLCJuYW1lIjpudWxsLCJncm91cCI6IjEwMDA0NTgifX0=");

            var json = Encoding.UTF8.GetString(data);
            
            Console.WriteLine(json);
            Console.WriteLine();
            Console.WriteLine(Convert.ToBase64String(data));
            Console.WriteLine();


            service = new SymmetricEncryptionService();
            var enc2 = service.Encrypt(data, secret, nonce);

            Console.WriteLine(Convert.ToBase64String(enc2)== "29U6B+E/y3QK+M2ZHHd1D3V2v5R3E9O6zuj4o6gToeTC/G/wTLqSSf6jYWSpnNgcvvGZCg+F1bgWxlHuaj9OHRaSzBuhugSDIb5sicKPjPboaXVRxQWh9ZaK14vcv1JtQG2uA/vWUSEMgPCi78KL8albDXNQQq/H25bIjCyHxCaQ1Oxb6XvCkJFop/6C6qn3mHE9m7lfJRJvGgaz+F/YTa6vgqWf7ejfsjaPzd+U17sV+gQJO7NoG5N3cDTT7K98fNMKIgVNSFjkN/gOrjuakkzums/+oflrMVPTkUezBSDASjOYT7LiQv1tJBZzTtNYRUllCXriQAyXXr9SAsmPxpYHdug0imqNjW+00N2u7d1tAPl7KHSXoH40hdCssatXynCv4A8ercarSrqRBbV9crI7Y4nXOuP6BmfMd2i28R2Y0HffSpt1w5sEC95vZjNJHzd10uMCXuvKcJcTvfplA+BC8B8CUCnLfu/GV+sq7Vo671m0wS2ECPrPMfCBniJAedV4G7lnYNYjfyM9sudVIhUWut6rPV4K/PTYDVHTSMLGh9PRZzljuLO83bwoIYOVTi6zQ1kcGBAwVCzsEVtLkxy1IAXo97r3A3c/n0IScwECwXzzSFbX76x9WHbBB5zP/mmq4kZVJyi7es7cTvLAApL9ph8ZdWR/UHaTiiFsPf5pZq1L+6DTVXxCeHFw9qKjaKqZhZVdcuvR1MDq9ag1lbQkNJINVHgW0N3bIARCOyZ0MkIo8VlcbHDgBMbquvzjZslB08FhGdmETeuQ7giWXbedrTtYQgDXUVbX0c8l37C6d/j2MrtH8ln44PFV+CjBhexILtmBkztkseoeayMbU6xm6dJVmdBodS8KyGGFnYiLACGGPnoyl78EWm7N7dxMdRHS4S++qbPsNFPVNCKsqf7T8zSgk9onRtJkwBM2zB4P9HnFNK1yCbRZ4bTHrhLIqXFI8yhRHiml3q6Y7NCIog==");

            Console.WriteLine("=-=-=-=-=-=-=-");
            Console.WriteLine(Convert.ToBase64String(enc2));




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

    public class KeyJson
    {
        public string Key { get; set; }
    }
}
