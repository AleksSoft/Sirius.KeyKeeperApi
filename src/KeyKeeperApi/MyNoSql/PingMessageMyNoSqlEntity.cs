using System;
using MyNoSqlServer.Abstractions;

namespace KeyKeeperApi.MyNoSql
{
    public class PingMessageMyNoSqlEntity : IMyNoSqlDbEntity
    {
        public const string TableName = "validator-ping-message";

        public string ValidatorId { get; set; }

        public string Message { get; set; }

        public static PingMessageMyNoSqlEntity Generate(string validatorId, string message)
        {
            var entity = new PingMessageMyNoSqlEntity()
            {
                PartitionKey = GeneratePartitionKey(validatorId),
                RowKey = GenerateRowKey(),
                Message = message,
                ValidatorId = validatorId
            };
            return entity;
        }

        public static string GeneratePartitionKey(string validatorId) => validatorId;
        public static string GenerateRowKey() => "message";


        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
