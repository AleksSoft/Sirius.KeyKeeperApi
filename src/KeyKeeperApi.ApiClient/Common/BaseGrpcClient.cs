using System;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Swisschain.KeyKeeperApi.ApiClient.Common
{
    public class BaseGrpcClient : IDisposable
    {
        private readonly Channel _channel;

        protected CallInvoker CallInvoker { get; }
        
        protected BaseGrpcClient(string serverGrpcUrl, string apiKey)
        {
            var lowerCaseUrl = serverGrpcUrl.ToLowerInvariant();

            if (!lowerCaseUrl.StartsWith("http"))
            {
                throw new InvalidOperationException("Specify protocol explicitly ('http://<your-url>' or 'https://<your-url>')");
            }

            var isHttps = lowerCaseUrl.StartsWith("https://");

            // It works only when there is no protocol in the URL :-\
            var correctedUrl = serverGrpcUrl
                .Replace("https://", string.Empty)
                .Replace("http://", string.Empty);

            if (!isHttps)
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            var credentials = isHttps ? new SslCredentials() : ChannelCredentials.Insecure;

            _channel = new Channel(correctedUrl, credentials);
            
            CallInvoker = _channel.Intercept(metadata =>
            {
                metadata.Add("Authorization", $"Bearer {apiKey}");
                return metadata;
            });
        }

        public void Dispose()
        {
            _channel.ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}
