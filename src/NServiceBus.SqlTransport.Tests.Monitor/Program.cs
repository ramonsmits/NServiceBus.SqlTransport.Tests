﻿using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NServiceBus.SqlTransport.Tests.Shared;

namespace NServiceBus.SqlTransport.Tests.Monitor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = Configuration.AppInsightKey;

            var telemetryClient = new TelemetryClient(configuration);
            telemetryClient.TrackTrace("Monitor started");

            Console.WriteLine("Cleaning wait_time stats ...");

            await ClearWaitTimeStats();

            Console.WriteLine("Monitor started");

            while (true)
            {
                var queueLengthMetric = await GetQueueLengthMetric(Configuration.ReceiverEndpointName);

                telemetryClient.TrackMetric(queueLengthMetric);

                var pageLatchMetric = await GetPageLatchStats();

                telemetryClient.TrackMetric(pageLatchMetric);

                telemetryClient.Flush();

                Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] Metrics pushed to AppInsights");

                await Task.Delay(TimeSpan.FromSeconds(1));

            }
        }

        static async Task ClearWaitTimeStats()
        {
            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("DBCC SQLPERF ('sys.dm_os_wait_stats', CLEAR); ", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        static async Task<MetricTelemetry> GetQueueLengthMetric(string endpointName)
        {
            var query =
                $@"SELECT isnull(cast(max([RowVersion]) - min([RowVersion]) + 1 AS int), 0) FROM [{endpointName}] WITH (nolock)";

            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();

                    return new MetricTelemetry
                    {
                        Name = "queue length",
                        Sum = (int) result,
                        Count = 1
                    };
                }
            }
        }

        static async Task<MetricTelemetry> GetPageLatchStats()
        {
            var query = $@"select max_wait_time_ms from qpi.wait_stats where wait_type = 'PAGELATCH_EX'";

            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();

                    return new MetricTelemetry
                    {
                        Name = "PAGELATCH_EX - max_wait_time_ms",
                        Sum = result == null ? 0 :  decimal.ToDouble((decimal)result),
                        Count = 1
                    };
                }
            }
        }
    }
}
