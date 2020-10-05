using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace KeyKeeperApi.Grpc.tools
{
    public class AsymmetricEncryptionService
    {
        private const long PublicExponent = 3;
        private const int Strength = 1024;
        private const int Certainty = 25;

        private readonly SecureRandom _secureRandom = new SecureRandom();

        public string EncryptToBase64(byte[] data, string publicKey)
        {
            var cipheredData = Encrypt(data, publicKey);

            return Convert.ToBase64String(cipheredData);
        }

        public byte[] Encrypt(byte[] data, string publicKey)
        {
            AsymmetricKeyParameter publicKeyParameters;

            using (var reader = new StringReader(publicKey))
            {
                var pemReader = new PemReader(reader);
                publicKeyParameters = (AsymmetricKeyParameter)pemReader.ReadObject();
            }

            var cipher = new Pkcs1Encoding(new RsaEngine());

            cipher.Init(true, publicKeyParameters);
            Console.WriteLine(cipher.AlgorithmName);

            var cipheredData = cipher.ProcessBlock(data, 0, data.Length);

            return cipheredData;
        }

        public byte[] Decrypt(string value, string privateKey)
        {
            var data = Convert.FromBase64String(value);

            return Decrypt(data, privateKey);
        }

        public byte[] Decrypt(byte[] data, string privateKey)
        {
            using (var reader = new StringReader(privateKey))
            {
                // https://stackoverflow.com/a/60423034
                var pemReader = new Org.BouncyCastle.Utilities.IO.Pem.PemReader(reader);
                var pem = pemReader.ReadPemObject();

                AsymmetricKeyParameter pk = PrivateKeyFactory.CreateKey(pem.Content);
                
                var cipher1 = new Pkcs1Encoding(new RsaEngine());
                cipher1.Init(false, (ICipherParameters)pk);

                var decipheredData = cipher1.ProcessBlock(data, 0, data.Length);

                return decipheredData;
            }
        }

        public bool VerifySignature(byte[] data, byte[] signature, string publicKey)
        {
            AsymmetricKeyParameter publicKeyParameters;

            using (var reader = new StringReader(publicKey))
            {
                var pemReader = new PemReader(reader);
                publicKeyParameters = (AsymmetricKeyParameter)pemReader.ReadObject();
            }

            var signer = SignerUtilities.GetSigner("SHA256WITHRSA");
            signer.Init(false, publicKeyParameters);
            signer.BlockUpdate(data, 0, data.Length);
            var verifyResult = signer.VerifySignature(signature);
            return verifyResult;
        }

        public byte[] GenerateSignature(byte[] data, string privateKey)
        {
            using (var reader = new StringReader(privateKey))
            {
                // https://stackoverflow.com/a/60423034
                var pemReader = new Org.BouncyCastle.Utilities.IO.Pem.PemReader(reader);
                var pem = pemReader.ReadPemObject();

                AsymmetricKeyParameter pk = PrivateKeyFactory.CreateKey(pem.Content);

                var signer = SignerUtilities.GetSigner("SHA256WITHRSA");
                signer.Init(true, pk);
                signer.BlockUpdate(data, 0, data.Length);

                var signature = signer.GenerateSignature();
                return signature;
            }
        }

        public Tuple<string, string> GenerateKeyPairPem()
        {
            var keyPairGenerator = new RsaKeyPairGenerator();

            var param = new RsaKeyGenerationParameters(
                Org.BouncyCastle.Math.BigInteger.ValueOf(PublicExponent),
                _secureRandom,
                Strength,
                Certainty);

            keyPairGenerator.Init(param);

            var keyPair = keyPairGenerator.GenerateKeyPair();

            var privateKey = KeyToString(keyPair.Private);

            var publicKey = KeyToString(keyPair.Public);

            return new Tuple<string, string>(privateKey, publicKey);
        }

        private static string KeyToString(AsymmetricKeyParameter keyParameter)
        {
            using var stringWriter = new StringWriter();
            var pemWriter = new PemWriter(stringWriter);
            pemWriter.WriteObject(keyParameter);
            return stringWriter.ToString();
        }
    }
}
