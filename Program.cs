using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults(workerApplication =>
        {

            workerApplication.UseMiddleware<MyCustomMiddleware>();
        })
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
            services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
        })
        .ConfigureLogging(logging =>
        {
            logging.AddFilter("Microsoft.*", LogLevel.Trace);
        })
        .Build();

        await host.RunAsync();
    }
}

public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is MetricTelemetry metricTelemetry)
        {
            // Use reflection to inspect and potentially modify MetricTelemetry
            AlterMetricTelemetry(metricTelemetry);
        }
        else if (telemetry is PerformanceCounterTelemetry performanceCounterTelemetry)
        {
            // Use reflection to inspect and potentially modify PerformanceCounterTelemetry
            AlterPerformanceCounterTelemetry(performanceCounterTelemetry);
        }
    }

    private void AlterMetricTelemetry(MetricTelemetry metricTelemetry)
    {
        // Use reflection to get metric details (e.g., CPU usage, thread count)
        var metricNameProperty = metricTelemetry.GetType().GetProperty("Name");
        if (metricNameProperty != null)
        {
            string metricName = metricNameProperty.GetValue(metricTelemetry) as string;
            if (metricName == "Processor Time" || metricName == "Thread Count")
            {
                Console.WriteLine($"Metric: {metricName}, Value: {metricTelemetry.Sum}");
            }
        }

        // Add or modify custom properties if needed
        metricTelemetry.Properties["CustomMetricProperty"] = "CustomValue";
    }

    private void AlterPerformanceCounterTelemetry(PerformanceCounterTelemetry performanceCounterTelemetry)
    {
        // Use reflection to get performance counter details
        var counterNameProperty = performanceCounterTelemetry.GetType().GetProperty("CounterName");
        if (counterNameProperty != null)
        {
            string counterName = counterNameProperty.GetValue(performanceCounterTelemetry) as string;
            if (counterName == @"\Processor(_Total)\% Processor Time" || counterName == @"\Thread Count")
            {
                Console.WriteLine($"Counter: {counterName}, Value: {performanceCounterTelemetry.Value}");
            }
        }

        // Add or modify custom properties if needed
        performanceCounterTelemetry.Properties["CustomCounterProperty"] = "CustomValue";
    }
}