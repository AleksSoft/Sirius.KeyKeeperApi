using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace KeyKeeperApi.Grpc.tools
{
    public class SymmetricEncryptionService 
    {
        private const int KeyBitSize = 256;
        private const int MacBitSize = 128;
        private const int NonceBitSize = 128;

        private readonly SecureRandom _random;

        public SymmetricEncryptionService()
        {
            _random = new SecureRandom();
        }

        /// <summary>
        /// Encrypt data use key and random nonce. Return encrypted content and nonce (IV) 
        /// </summary>
        public byte[] Encrypt(byte[] data, byte[] key, byte[] nonce)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data can not be null.");

            if (key == null)
                throw new ArgumentNullException(nameof(key), "Key can not be null");

            if (data.Length == 0)
                throw new ArgumentException("Data can not be empty string.", nameof(data));

            if (key == null || key.Length != KeyBitSize / 8)
                throw new ArgumentException($"Key needs to be {KeyBitSize} bit!", nameof(key));
           
            var cipher = new GcmBlockCipher(new AesEngine());
            Console.WriteLine(cipher.AlgorithmName);
            //var cipher = new AesEngine();

            var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce);

            cipher.Init(true, parameters);

            var cipherData = new byte[cipher.GetOutputSize(data.Length)];

            var len = cipher.ProcessBytes(data,
                0,
                data.Length,
                cipherData,
                0);

            cipher.DoFinal(cipherData, len);

            return cipherData;
        }

        public byte[] Decrypt(byte[] data, byte[] key, byte[] ivNonce)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data can not be null.");

            if (key == null)
                throw new ArgumentNullException(nameof(key), "Key can not be null");

            if (data.Length == 0)
                throw new ArgumentException("Data can not be empty string.", nameof(data));

            if (key == null || key.Length != KeyBitSize / 8)
                throw new ArgumentException($"Key should be {KeyBitSize} bit.", nameof(key));

            var nonce = ivNonce;

            var cipher = new GcmBlockCipher(new AesEngine());

            var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce);
            cipher.Init(false, parameters);

            var cipherData = data;

            var decryptedData = new byte[cipher.GetOutputSize(cipherData.Length)];

            try
            {
                var len = cipher.ProcessBytes(cipherData,
                    0,
                    cipherData.Length,
                    decryptedData,
                    0);
                cipher.DoFinal(decryptedData, len);
            }
            catch (InvalidCipherTextException ex)
            {
                Console.WriteLine("InvalidCipherTextException:");
                Console.WriteLine(ex);
                return null;
            }

            return decryptedData;
             
        }

        public byte[] GenerateKey()
        {
            var key = new byte[KeyBitSize / 8];
            _random.NextBytes(key);
            key[^1] &= 0x7F;
            return key;
        }

        public byte[] GenerateNonce()
        {
            var nonce = new byte[NonceBitSize / 8];

            _random.NextBytes(nonce, 0, nonce.Length);

            return nonce;
        }
    }
}
