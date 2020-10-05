using System;

namespace KeyKeeperApi.WebApi.Models.Validators
{
    public class Validator
    {

        public Validator(string id, string validatorId, string publicKey,
            string name,
            string position,
            string description,
            bool isAccepted,
            bool isBlocked,
            string deviceInfo, 
            DateTime createdAt,
            string createdByAdminId,
            string createdByAdminEmail)
        {
            CreatedByAdminId = createdByAdminId;
            CreatedByAdminEmail = createdByAdminEmail;
            DeviceInfo = deviceInfo;
            Id = id;
            ValidatorId = validatorId;
            PublicKey = publicKey;
            Name = name;
            Position = position;
            Description = description;
            IsAccepted = isAccepted;
            IsBlocked = isBlocked;
            CreatedAt = createdAt;
        }

        public string Id { get; set; }

        public string ValidatorId { get; set; }

        public string PublicKey { get; set; }

        public string Name { get; set; }

        public string Position { get; set; }

        public string Description { get; set; }

        public bool IsAccepted { get; set; }

        public bool IsBlocked { get; set; }

        public string DeviceInfo { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedByAdminId { get; set; }

        public string CreatedByAdminEmail { get; set; }
    }
}
