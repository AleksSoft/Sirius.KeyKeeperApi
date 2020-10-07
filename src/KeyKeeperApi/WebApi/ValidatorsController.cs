using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using KeyKeeperApi.Consts;
using KeyKeeperApi.MyNoSql;
using KeyKeeperApi.WebApi.Models.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Swisschain.Sdk.Server.Authorization;

namespace KeyKeeperApi.WebApi
{
    [Authorize]
    [ApiController]
    [Route("api/validators")]
    public class ValidatorsController : ControllerBase
    {
        private readonly IMyNoSqlServerDataWriter<ValidatorLinkEntity> _validationWriter;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validationReader;
        private readonly IMyNoSqlServerDataWriter<PingMessageMyNoSqlEntity> _pingMessageWriter;
        private readonly ILogger<ValidatorsController> _logger;

        public ValidatorsController(IMyNoSqlServerDataWriter<ValidatorLinkEntity> validationWriter,
            IMyNoSqlServerDataReader<ValidatorLinkEntity> validationReader,
            IMyNoSqlServerDataWriter<PingMessageMyNoSqlEntity> pingMessageWriter,
            ILogger<ValidatorsController> logger)
        {
            _validationWriter = validationWriter;
            _validationReader = validationReader;
            _pingMessageWriter = pingMessageWriter;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<CreateInvitation> CreateInvitationAsync([FromBody] CreateInvitationRequest request)
        {
            var (auth, tenantId, adminId, adminEmail) = Authorize();

            if (!auth)
            {
                return new CreateInvitation();
            }

            var invite = ValidatorLinkEntity.Generate(tenantId, Guid.NewGuid().ToString("N"));
            invite.InvitationToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            invite.Name = request.Name;
            invite.Position = request.Position;
            invite.Description = request.Description;
            invite.CreatedAt = DateTime.UtcNow;
            invite.CreatedByAdminId = adminId;
            invite.CreatedByAdminEmail = adminEmail;
            await _validationWriter.InsertOrReplaceAsync(invite);


            _logger.LogInformation("Invitation Created. TenantId: {TenantId}; AdminId: {AdminId}", tenantId, adminId);

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

        [HttpDelete("remove")]
        public async Task RemoveValidatorApiKeyAsync([FromBody] ValidatorRequest request)
        {
            var (auth, tenantId, adminId, adminEmail) = Authorize();

            if (!auth)
            {
                return;
            }

            var entity = _validationReader.Get(ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(request.ApiKeyId));

            if (entity == null)
            {
                return;
            }

            await _validationWriter.DeleteAsync(entity.PartitionKey, entity.RowKey);
            _logger.LogInformation("Removed validator Api Key: {ApiKeyId}; AdminId: {AdminId}; Name: {Name}; Device: {Device}; TenantId: {TenantId}", request.ApiKeyId, adminId, entity.Name, entity.DeviceInfo, tenantId);
        }

        [HttpPost("block")]
        public async Task BlockValidatorAsync([FromBody] ValidatorRequest request)
        {
            var (auth, tenantId, adminId, adminEmail) = Authorize();

            if (!auth)
            {
                return;
            }

            var entity = _validationReader.Get(ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(request.ApiKeyId));

            if (entity == null)
            {
                _logger.LogInformation("ValidatorLinkEntity not found by API key: {ApiKeyId}", request.ApiKeyId);
                HttpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return;
            }

            entity.IsBlocked = true;

            await _validationWriter.InsertOrReplaceAsync(entity);

            _logger.LogInformation("Validator Api Key is Blocked. API key: {ApiKeyId}; ValidatorId: {ValidatorId}; TenantId: {TenantId}; Name: {Name}; AdminId: {AdminId}", request.ApiKeyId, entity.ValidatorId, entity.TenantId, entity.Name, adminId);
        }

        [HttpPost("unblock")]
        public async Task UnBlockValidatorAsync([FromBody] ValidatorRequest request)
        {
            var (auth, tenantId, adminId, adminEmail) = Authorize();

            if (!auth)
            {
                return;
            }

            var entity = _validationReader.Get(ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(request.ApiKeyId));

            if (entity == null)
            {
                _logger.LogInformation("ValidatorLinkEntity not found by API key: {ApiKeyId}", request.ApiKeyId);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            entity.IsBlocked = false;

            await _validationWriter.InsertOrReplaceAsync(entity);

            _logger.LogInformation("Validator Api Key is UnBlocked. API key: {ApiKeyId}; ValidatorId: {ValidatorId}; TenantId: {TenantId}; Name: {Name}; AdminId: {AdminId}", request.ApiKeyId, entity.ValidatorId, entity.TenantId, entity.Name, adminId);
        }
        
        [HttpGet("validator-list")]
        public List<Validator> GetValidators()
        {
            var (auth, tenantId, adminId, adminEmail) = Authorize();

            if (!auth)
            {
                return new List<Validator>();
            }

            var list = _validationReader.Get(ValidatorLinkEntity.GeneratePartitionKey(tenantId));

            var result = list.Select(v => new Validator(
                    v.RowKey,
                    v.ValidatorId,
                    v.PublicKeyPem,
                    v.Name,
                    v.Position,
                    v.Description,
                    v.IsAccepted,
                    v.IsBlocked,
                    v.DeviceInfo,
                    v.CreatedAt, 
                    v.CreatedByAdminId,
                    v.CreatedByAdminEmail))
                .ToList();

            return result;
        }

        [HttpPost("ping")]
        public async Task PingValidatorAsync([FromBody] PingValidatorRequest request)
        {
            var (auth, tenantId, adminId, adminEmail) = Authorize();

            if (!auth)
            {
                return;
            }

            var entity = _validationReader.Get(ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(request.ApiKeyId));

            if (entity == null)
            {
                _logger.LogInformation("Cannot send ping. ValidatorLinkEntity not found by API key: {ApiKeyId}", request.ApiKeyId);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (!entity.IsAccepted)
            {
                _logger.LogInformation("Cannot send ping. ValidatorLinkEntity is not accepted: {ApiKeyId}", request.ApiKeyId);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (string.IsNullOrEmpty(request.Message))
            {
                _logger.LogInformation("Cannot send ping. Message cannot be empty: {ApiKeyId}", request.ApiKeyId);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (Encoding.UTF8.GetBytes(request.Message).Length > 100)
            {
                _logger.LogInformation("Cannot send ping. Message length in bytes more that 100: {ApiKeyId}; Message: {Message}", request.ApiKeyId, request.Message);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var message = PingMessageMyNoSqlEntity.Generate(entity.ValidatorId, request.Message);

            await _pingMessageWriter.InsertOrReplaceAsync(message);

            _logger.LogInformation("Ping message are created. API key: {ApiKeyId}; ValidatorId: {ValidatorId}; TenantId: {TenantId}; Name: {Name}; AdminId: {AdminId}", request.ApiKeyId, entity.ValidatorId, entity.TenantId, entity.Name, adminId);
        }



        private (bool, string, string, string) Authorize()
        {
            var tenantId = User.GetClaimOrDefault(Claims.TenantId);
            var adminId = User.GetClaimOrDefault(Claims.UserId);
            var adminEmail = User.GetClaimOrDefault(Claims.UserEmail);


            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(adminId) || string.IsNullOrEmpty(adminEmail))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                _logger.LogInformation("Unauthorized request. Path: {Path}; TenantId: {TenantId}; AdminId: {AdminId}; AdminEmail: {AdminEmail}", 
                    HttpContext.Request.Path, tenantId, adminId, adminEmail);

                return (false, tenantId, adminId, adminEmail);
            }

            _logger.LogInformation("Request. Path: {Path}; TenantId: {TenantId}; AdminId: {AdminId}; AdminEmail: {AdminEmail}",
                HttpContext.Request.Path, tenantId, adminId, adminEmail);

            return (true, tenantId, adminId, adminEmail);
        }


    }
}
