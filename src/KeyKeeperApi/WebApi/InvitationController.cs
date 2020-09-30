using System;
using System.Threading.Tasks;
using KeyKeeperApi.MyNoSql;
using Microsoft.AspNetCore.Mvc;
using MyNoSqlServer.Abstractions;

namespace KeyKeeperApi.WebApi
{
    [ApiController]
    [Route("api/invitation")]
    public class InvitationController : ControllerBase
    {
        private readonly IMyNoSqlServerDataWriter<ValidatorLinkEntity> _validationWriter;

        public InvitationController(IMyNoSqlServerDataWriter<ValidatorLinkEntity> validationWriter)
        {
            this._validationWriter = validationWriter;
        }

        [HttpPost("create")]
        public async Task<CreateInvitation> CreateInvitationAsync([FromBody] CreateInvitationRequest request)
        {
            var invite = ValidatorLinkEntity.Generate(request.TenantId, Guid.NewGuid().ToString("N"));
            invite.InvitationToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            invite.Name = request.Name;
            invite.Position = request.Position;
            invite.Description = request.Description;
            await _validationWriter.InsertOrReplaceAsync(invite);

            var resp = new CreateInvitation()
            {
                TenantId = invite.TenantId,
                InvitationToken = invite.InvitationToken,
                Name = invite.Name,
                Position = invite.Position,
                Description = invite.Description
            };

            return resp;
        }

        public class CreateInvitationRequest
        {
            public string TenantId { get; set; }

            public string Name { get; set; }

            public string Position { get; set; }

            public string Description { get; set; }
        }

        public class CreateInvitation
        {
            public string TenantId { get; set; }

            public string Name { get; set; }

            public string Position { get; set; }

            public string Description { get; set; }

            public string InvitationToken { get; set; }
        }
    }
}
