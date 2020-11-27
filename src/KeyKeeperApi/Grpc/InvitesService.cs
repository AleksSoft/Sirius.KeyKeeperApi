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
using Version = Swisschain.Sirius.ValidatorApi.Version;

namespace KeyKeeperApi.Grpc
{
    public class InvitesService : Invites.InvitesBase
    {
        private readonly AuthConfig _authConfig;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validatorLinkReader;
        private readonly IMyNoSqlServerDataWriter<ValidatorLinkEntity> _validatorLinkWriter;
        private readonly IMyNoSqlServerDataReader<PingMessageMyNoSqlEntity> _pingMessageReader;
        private readonly IMyNoSqlServerDataWriter<PingMessageMyNoSqlEntity> _pingMessageWriter;
        private readonly ILogger<InvitesService> _logger;

        public InvitesService(AuthConfig authConfig, 
            IMyNoSqlServerDataReader<ValidatorLinkEntity> validatorLinkReader,
            IMyNoSqlServerDataWriter<ValidatorLinkEntity> validatorLinkWriter,
            IMyNoSqlServerDataReader<PingMessageMyNoSqlEntity> pingMessageReader,
            IMyNoSqlServerDataWriter<PingMessageMyNoSqlEntity> pingMessageWriter,
            ILogger<InvitesService> logger)
        {
            _authConfig = authConfig;
            _validatorLinkReader = validatorLinkReader;
            _validatorLinkWriter = validatorLinkWriter;
            _pingMessageReader = pingMessageReader;
            _pingMessageWriter = pingMessageWriter;
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
            validatorLinkEntity.PushNotificationFcmToken = request.PushNotificationFCMToken;
            await _validatorLinkWriter.InsertOrReplaceAsync(validatorLinkEntity);

            _logger.LogInformation("Invitation accepted. InviteId='{InviteId}'; ValidatorId='{ValidatorId}'; PushNotificationFcmToken='{PushNotificationFcmToken}'", request.InviteId, request.ValidatorId, request.PushNotificationFCMToken);

            return resp;
        }

        private Random _rnd = new Random();

        [Authorize]
        public override async Task<PingResponse> GetPing(PingRequest request, ServerCallContext context)
        {
            var validatorId = context.GetHttpContext().User.GetClaimOrDefault(Claims.KeyKeeperId);
            var publicKey = context.GetHttpContext().User.GetClaimOrDefault(Claims.PublicKeyPem);

            if (string.IsNullOrEmpty(publicKey))
            {
                return new PingResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.InternalServerError,
                        Message = "Incorrect Bearer Token."
                    }
                };
            }

            var message = _pingMessageReader.Get(PingMessageMyNoSqlEntity.GeneratePartitionKey(validatorId),
                PingMessageMyNoSqlEntity.GenerateRowKey());

            var response = new PingResponse();

            if (message == null)
            {
                response.MessageEnc = string.Empty;
                response.SignatureMessage = string.Empty;
            }
            else
            {
                var asynccrypto = new AsymmetricEncryptionService();
                var messageEnc = asynccrypto.Encrypt(Encoding.UTF8.GetBytes(message.Message), publicKey);

                response.MessageEnc = Convert.ToBase64String(messageEnc);
                response.SignatureMessage = "not-implemented-please-skip";

                await _pingMessageWriter.DeleteAsync(message.PartitionKey, message.RowKey);
            }

            _logger.LogInformation("GetPing response. ValidatorId='{ValidatorId}'; HasMessage={HasMessage}", validatorId, !string.IsNullOrEmpty(response.MessageEnc));

            return response;
        }

        [Authorize]
        public override async Task<RemoveVaultConnectionResponse> RemoveVaultConnection(RemoveVaultConnectionRequest request, ServerCallContext context)
        {
            var tenantId = context.GetHttpContext().User.GetClaimOrDefault(Claims.TenantId);
            var apiKeyId = context.GetHttpContext().User.GetClaimOrDefault(Claims.ApiKeyId);

            if (string.IsNullOrEmpty(apiKeyId))
            {
                return new RemoveVaultConnectionResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.InternalServerError,
                        Message = "Incorrect Bearer Token. "
                    }
                };
            }

            var validatorLinkEntity = _validatorLinkReader.Get(
                ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(apiKeyId));

            if (validatorLinkEntity != null)
            {
                await _validatorLinkWriter.DeleteAsync(
                    ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                    ValidatorLinkEntity.GenerateRowKey(apiKeyId));
            }

            return new RemoveVaultConnectionResponse();
        }

        [Authorize]
        public override async Task<RefreshPushNotificationFCMTokenResponse> RefreshPushNotificationFCMToken(RefreshPushNotificationFCMTokenRequest request, ServerCallContext context)
        {
            var tenantId = context.GetHttpContext().User.GetClaimOrDefault(Claims.TenantId);
            var apiKeyId = context.GetHttpContext().User.GetClaimOrDefault(Claims.ApiKeyId);

            if (string.IsNullOrEmpty(apiKeyId))
            {
                return new RefreshPushNotificationFCMTokenResponse()
                {
                    Error = new ValidatorApiError()
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.InternalServerError,
                        Message = "Incorrect Bearer Token. "
                    }
                };
            }

            var validatorLinkEntity = _validatorLinkReader.Get(
                ValidatorLinkEntity.GeneratePartitionKey(tenantId),
                ValidatorLinkEntity.GenerateRowKey(apiKeyId));

            if (validatorLinkEntity != null)
            {
                validatorLinkEntity.PushNotificationFcmToken = request.PushNotificationFCMToken;
                await _validatorLinkWriter.InsertOrReplaceAsync(validatorLinkEntity);

                _logger.LogInformation("Refresh PushNotificationFcmToken. ValidatorId='{ValidatorId}'; PushNotificationFcmToken='{PushNotificationFcmToken}'", validatorLinkEntity.ValidatorId, request.PushNotificationFCMToken);
            }
            else
            {
                return new RefreshPushNotificationFCMTokenResponse
                {
                    Error = new ValidatorApiError
                    {
                        Code = ValidatorApiError.Types.ErrorCodes.ExpiredApiKey,
                        Message = "API key is expired or deleted"
                    }
                };
            }

            return new RefreshPushNotificationFCMTokenResponse();
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


    public class VersionService : Version.VersionBase
    {
        public override Task<GetCurrentVersionResponce> GetCurrentVersion(GetCurrentVersionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetCurrentVersionResponce()
            {
                Major = 1,
                Minor = 0
            });
        }
    }


}
