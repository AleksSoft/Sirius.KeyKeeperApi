using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KeyKeeperApi.Worker.WebApi.Models.IsAlive
{
    public class IsAliveResponse
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string Env { get; set; }

        public string Host { get; set; }

        public bool IsDebug { get; set; }

        public DateTime StartedAt { get; set; }

        public List<StateIndicator> StateIndicators { get; set; }

        public class StateIndicator
        {
            public string Type { get; set; }

            public string Value { get; set; }
        }
    }
}
