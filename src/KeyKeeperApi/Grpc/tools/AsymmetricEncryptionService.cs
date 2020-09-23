using System;
using System.IO;
using Org.BouncyCastle.Crypto;
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

            var cipher = new RsaEngine();
            cipher.Init(true, publicKeyParameters);

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
            AsymmetricCipherKeyPair keyPair;

            using (var reader = new StringReader(privateKey))
            {
                var pemReader = new PemReader(reader);
                keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
            }

            var cipher = new RsaEngine();
            cipher.Init(false, (RsaKeyParameters)keyPair.Private);

            var decipheredData = cipher.ProcessBlock(data, 0, data.Length);

            return decipheredData;
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
