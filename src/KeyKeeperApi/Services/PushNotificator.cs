﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KeyKeeperApi.Common.Configuration;
using KeyKeeperApi.MyNoSql;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;

namespace KeyKeeperApi.Services
{
    public class PushNotificator : IPushNotificator, IStartable, IDisposable
    {
        private readonly FireBaseMessagingConfig _config;
        private readonly IMyNoSqlServerDataReader<ValidatorLinkEntity> _validatorLinkReader;
        private readonly ILogger<PushNotificator> _logger;
        private readonly bool _isActive;

        public PushNotificator(
            FireBaseMessagingConfig config,
            IMyNoSqlServerDataReader<ValidatorLinkEntity> validatorLinkReader,
            ILogger<PushNotificator> logger)
        {
            _config = config;
            _validatorLinkReader = validatorLinkReader;
            _logger = logger;
            _isActive = !string.IsNullOrEmpty(_config.FireBasePrivateKeyJson) && _config.FireBasePrivateKeyJson != "${Sirius-KeyKeeperApi-FireBasePrivateKeyJson}";
        }

        public async Task SendPushNotifications(ApprovalRequestMyNoSqlEntity approvalRequest)
        {
            var traceLog = string.Empty;
            try
            {
                if (!_isActive)
                    return;

                var validators = _validatorLinkReader.Get()
                    .Where(v => v.TenantId == approvalRequest.TenantId)
                    .Where(v => v.ValidatorId == approvalRequest.ValidatorId)
                    .Where(v => v.IsAccepted)
                    .Where(v => !v.IsBlocked)
                    .Where(v => !string.IsNullOrEmpty(v.DeviceInfo))
                    .Where(v => !string.IsNullOrEmpty(v.PushNotificationFcmToken));

                var hashset = new HashSet<string>();
                var tokens = new List<string>();

                foreach (var validator in validators)
                {
                    if (!hashset.Contains(validator.DeviceInfo))
                    {
                        tokens.Add(validator.PushNotificationFcmToken);
                        hashset.Add(validator.DeviceInfo);
                    }
                }

                if (!tokens.Any())
                {
                    _logger.LogInformation(
                        "Push notification for TransferSigningRequestId={TransferSigningRequestId} is sent to {ValidatorId}. SuccessCount: {SuccessCount}. FailureCount: {FailureCount}.",
                        approvalRequest.TransferSigningRequestId,
                        approvalRequest.ValidatorId,
                        0,
                        0);
                    return;
                }

                var message = new MulticastMessage()
                {
                    Notification = new Notification()
                    {
                        Title = "New Approval Request",
                        Body =
                            "You receive a new approval request. Please check the transfer details in the application."
                    },
                    Tokens = tokens
                };

                traceLog = JsonConvert.SerializeObject(message);

                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);

                _logger.LogInformation(
                    "Push notification for TransferSigningRequestId={TransferSigningRequestId} is sent to {ValidatorId}. SuccessCount: {SuccessCount}. FailureCount: {FailureCount}.",
                    approvalRequest.TransferSigningRequestId,
                    approvalRequest.ValidatorId,
                    response.SuccessCount,
                    response.FailureCount);

                if (response.FailureCount > 0)
                {
                    var tokensList = JsonConvert.SerializeObject(tokens);

                    foreach (var result in response.Responses.Where(r => r.IsSuccess == false))
                    {
                        _logger.LogWarning(
                            "Fail push notification. TransferSigningRequestId: {TransferSigningRequestId}; ValidatorId: {ValidatorId}; MessageId: {MessageId}; MessagingErrorCode: {MessagingErrorCode}; Message: {Message}; Tokens: {TokensList}",
                            approvalRequest.TransferSigningRequestId,
                            approvalRequest.ValidatorId,
                            result.MessageId,
                            result.Exception.MessagingErrorCode,
                            result.Exception.Message,
                            tokensList);
                    }

                    
                }

            }
            catch(Exception ex)
            {
                Console.WriteLine(traceLog);
                _logger.LogError(ex, "Cannot send push notification to validator. TransferSigningRequestId={TransferSigningRequestId}; ValidatorId={ValidatorId}", approvalRequest.TransferSigningRequestId, approvalRequest.ValidatorId);
            }
        }

        public void Start()
        {
            if (!_isActive)
            {
                _logger.LogInformation("DISABLE FireBase application. Cannot send push notifications.");
                return;
            }
            ;
            var defaultApp = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(_config.FireBasePrivateKeyJson),
            });
            _logger.LogInformation(
                "FireBase application is ready to send push notifications. AppName: {Name}; ProjectId: {FcmProjectId}",
                defaultApp.Name, defaultApp.Options?.ProjectId);
        }

        public void Dispose()
        {
            
        }
    }
}
