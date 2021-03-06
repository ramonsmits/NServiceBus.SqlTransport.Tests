﻿using System;

namespace NServiceBus.SqlTransport.Tests.Shared
{
    public class Configuration
    {
        public const string SenderEndpointName = "SqlTransport-Test-Sender";

        public const string ReceiverEndpointName = "SqlTransport-Test-Receiver";

        public static string ConnectionString = Environment.GetEnvironmentVariable("SqlTestsConnectionString");
        
        public static string AppInsightKey = Environment.GetEnvironmentVariable("SqlTestAppInsightKey");
    }
}
