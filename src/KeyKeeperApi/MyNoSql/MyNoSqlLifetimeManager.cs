using System;
using System.Threading;
using Autofac;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;

namespace KeyKeeperApi.MyNoSql
{
    public class MyNoSqlLifetimeManager : IStartable, IDisposable
    {
        private readonly ILogger<MyNoSqlLifetimeManager> _logger;
        private readonly MyNoSqlTcpClient _client;
        private readonly IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> _approvalRequestReader;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validatorLinkEntityReader;
        private readonly IMyNoSqlServerDataReader<PingMessageMyNoSqlEntity> _pingMessageReader;


        public MyNoSqlLifetimeManager(
            ILogger<MyNoSqlLifetimeManager> logger,
            MyNoSqlTcpClient client,
            IMyNoSqlServerDataReader<ApprovalRequestMyNoSqlEntity> approvalRequestReader,
            IMyNoSqlServerDataReader<ValidatorLinkEntity> validatorLinkEntityReader,
            IMyNoSqlServerDataReader<PingMessageMyNoSqlEntity> pingMessageReader)
        {
            _logger = logger;
            _client = client;
            _approvalRequestReader = approvalRequestReader;
            _validatorLinkEntityReader = validatorLinkEntityReader;
            _pingMessageReader = pingMessageReader;
        }

        public void Start()
        {
            _logger.LogInformation("LifetimeManager starting...");
            _client.Start();

            _logger.LogInformation("LifetimeManager sleep 2 second...");
            Thread.Sleep(2000);

            _logger.LogInformation("approvalRequestReader - count: {Count}", _approvalRequestReader.Count());
            _logger.LogInformation("validatorLinkEntityReader - count: {Count}", _validatorLinkEntityReader.Count());
            _logger.LogInformation("pingMessageReader - count: {Count}", _pingMessageReader.Count());

            _logger.LogInformation("MyNoSqlLifetimeManager started");
        }

        public void Dispose()
        {
            if (_client.Connected)
            {
                _client.Stop();
            }
        }
    }
}
