﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KeyKeeperApi.Worker.WebApi.Models.IsAlive;
using Swisschain.Sdk.Server.Common;

namespace KeyKeeperApi.Worker.WebApi
{
    [ApiController]
    [Route("api/isalive")]
    public class IsAliveController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(IsAliveResponse), StatusCodes.Status200OK)]
        public IsAliveResponse Get()
        {
            var response = new IsAliveResponse
            {
                Name = ApplicationInformation.AppName,
                Version = ApplicationInformation.AppVersion,
                StartedAt = ApplicationInformation.StartedAt,
                Env = ApplicationEnvironment.Environment,
                Host = ApplicationEnvironment.HostName,
                StateIndicators = new List<IsAliveResponse.StateIndicator>()
            };

            return response;
        }
    }
}
