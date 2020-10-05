using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.Consts;
using KeyKeeperApi.Grpc.tools;
using KeyKeeperApi.MyNoSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyNoSqlServer.Abstractions;
using Swisschain.Sdk.Server.Authorization;
using Swisschain.Sirius.ValidatorApi;

namespace KeyKeeperApi.Grpc
{
    public class InvitesService : Invites.InvitesBase
    {
        private readonly AuthConfig _authConfig;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validatorLinkReader;
        private readonly IMyNoSqlServerDataWriter<ValidatorLinkEntity> _validatorLinkWriter;
        private readonly ILogger<InvitesService> _logger;

        public InvitesService(AuthConfig authConfig, IMyNoSqlServerDataReader<ValidatorLinkEntity> validatorLinkReader,
            IMyNoSqlServerDataWriter<ValidatorLinkEntity> validatorLinkWriter,
            ILogger<InvitesService> logger)
        {
            _authConfig = authConfig;
            _validatorLinkReader = validatorLinkReader;
            _validatorLinkWriter = validatorLinkWriter;
            _logger = logger;
        }

        public override async Task<AcceptResponse> Accept(AcceptRequest request, ServerCallContext context)
        {
            var validatorId = request.ValidatorId;

            var validatorLinkEntity = _validatorLinkReader.Get().FirstOrDefault(v => v.InvitationToken == request.InviteId);

            if (validatorLinkEntity == null)
            {
                _logger.LogInformation("Cannot accept invitation: 'Invitation token is not correct'. InviteId='{InviteId}'; ValidatorId='{ValidatorId}'", request.InviteId, request.ValidatorId);

                return new AcceptResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.WrongInvitation,
                        Message = "Invitation token is not correct"
                    }
                };
            }

            if (validatorLinkEntity.IsAccepted)
            {
                _logger.LogInformation("Cannot accept invitation: 'Invitation token already accepted'. InviteId='{InviteId}'; ValidatorId='{ValidatorId}'", request.InviteId, request.ValidatorId);

                return new AcceptResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.WrongInvitation,
                        Message = "Invitation token already accepted"
                    }
                };
            }

            if (string.IsNullOrEmpty(request.PublicKeyPem))
            {
                _logger.LogInformation("Cannot accept invitation: 'PublicKeyPem cannot be empty'. InviteId='{InviteId}'; ValidatorId='{ValidatorId}'", request.InviteId, request.ValidatorId);

                return new AcceptResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.WrongInvitation,
                        Message = "PublicKeyPem cannot be empty"
                    }
                };
            }

            var token = GenerateJwtToken(validatorId,
                request.PublicKeyPem,
                validatorLinkEntity.ApiKeyId,
                validatorLinkEntity.TenantId);

            var resp = new AcceptResponse
            {
                ApiKey = token,
                Name = validatorLinkEntity.Name,
                Position = validatorLinkEntity.Position,
                Description = validatorLinkEntity.Description
            };

            validatorLinkEntity.ValidatorId = request.ValidatorId;
            validatorLinkEntity.PublicKeyPem = request.PublicKeyPem;
            validatorLinkEntity.IsAccepted = true;
            validatorLinkEntity.DeviceInfo = request.DeviceInfo;
            await _validatorLinkWriter.InsertOrReplaceAsync(validatorLinkEntity);

            _logger.LogInformation("Invitation accepted. InviteId='{InviteId}'; ValidatorId='{ValidatorId}'", request.InviteId, request.ValidatorId);

            return resp;
        }

        private Random _rnd = new Random();

        [Authorize]
        public override Task<PingResponse> GetPing(PingRequest request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);
            var publicKey = context.GetHttpContext().User.GetClaimOrDefault(Claims.PublicKeyPem);

            if (string.IsNullOrEmpty(publicKey))
            {
                return Task.FromResult(new PingResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.InternalServerError,
                        Message = "Please add your ValidatorId with public key into settings for mock API"
                    }
                });
            }

            var asynccrypto = new AsymmetricEncryptionService();
            var messageEnc = asynccrypto.Encrypt(Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("s")), publicKey);

            var response = new PingResponse();
            if (_rnd.Next(100) > 50)
            {
                response.MessageEnc = Convert.ToBase64String(messageEnc);
                response.SignatureMessage = "not-implemented-please-skip";
            }
            else
            {
                response.MessageEnc = string.Empty;
                response.SignatureMessage = string.Empty;
            }

            //todo: make a signature for message here

            _logger.LogInformation("GetPing response. ValidatorId='{ValidatorId}'; HasMessage={HasMessage}", validatorId, !string.IsNullOrEmpty(response.MessageEnc));

            return Task.FromResult(response);
        }

        private string GenerateJwtToken(string validatorId, string publicKeyPem, string apiKeyId, string tenantId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authConfig.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", validatorId) }),
                Expires = DateTime.UtcNow.AddYears(1),
                Audience = _authConfig.Audience,
                Claims = new Dictionary<string, object>(),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            tokenDescriptor.Claims[Claims.KeyKeeperId] = validatorId;
            tokenDescriptor.Claims[Claims.PublicKeyPem] = publicKeyPem;
            tokenDescriptor.Claims[Claims.ApiKeyId] = apiKeyId;
            tokenDescriptor.Claims["tenant-id"] = tenantId;
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    
}
