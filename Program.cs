using Google.Protobuf.WellKnownTypes;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var list = PerformanceCounterCategory.GetCategories();
        var list2 = new List<string>();

        

        foreach (var category in list)
        {
            list2.Add(category.CategoryName);

            Console.WriteLine(category.CategoryName);
        }



        // List all performance counter categories
        foreach (var category in PerformanceCounterCategory.GetCategories())
        {
            Console.WriteLine($"Category: {category.CategoryName}");

            // List all counters within this category
            if (category.CategoryName == "Process") // Filter by specific category if needed
            {
                foreach (var counter in category.GetCounters("_Total")) // Example instance
                {
                    Console.WriteLine($"    Counter: {counter.CounterName}");
                }
            }
        }

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

        // Create TelemetryClient instance
        var telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);

        // Track a custom metric
        //var result = (telemetryClient.GetMetricManager()).Metrics; // ("Custom CPU Usage").TrackValue(25.0);

        // Track a custom metric for thread count
        //var result12 = 
            
        telemetryClient.GetMetric("Processor Time").TrackValue(100);

        await host.RunAsync();
    }
}

public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        //AlterMetricTelemetry(telemetry);

        //AlterMetricTelemetry(metricTelemetry);

        if (telemetry is MetricTelemetry metricTelemetry)
        {
            // Use reflection to inspect and potentially modify MetricTelemetry
            AlterMetricTelemetry(metricTelemetry);
        }
        //else if (telemetry is MetricTelemetry metricTelemetry1)
        //{
        //    // Use reflection to inspect and potentially modify PerformanceCounterTelemetry
        //    AlterPerformanceCounterTelemetry(metricTelemetry1);
        //}
    }

    private void AlterMetricTelemetry(MetricTelemetry metricTelemetry)
    {
        // Use reflection to get metric details (e.g., CPU usage, thread count)
        var metricNameProperty = metricTelemetry.GetType().GetProperty("Name");
        if (metricNameProperty != null)
        {
            Console.WriteLine(metricTelemetry.GetType());

            string metricName = metricNameProperty.GetValue(metricTelemetry) as string;
            Console.WriteLine($"Metric: {metricName}, Name: {metricTelemetry.Name}, Sum: {metricTelemetry.Sum}, Max: {metricTelemetry.Max}, Min: {metricTelemetry.Min}, Count: {metricTelemetry.Count}");

            //string metricName = metricNameProperty.GetValue(metricTelemetry) as string;
            //if (metricName == "Processor Time" || metricName == "Thread Count")
            //{
            //    Console.WriteLine($"Metric: {metricName}, Value: {metricTelemetry.Sum}");
            //}
        }

        // Add or modify custom properties if needed
        //metricTelemetry.Properties["CustomMetricProperty"] = "CustomValue";
    }

    //private void AlterPerformanceCounterTelemetry(MetricTelemetry performanceCounterTelemetry)
    //{
    //    // Use reflection to get performance counter details
    //    var counterNameProperty = performanceCounterTelemetry.GetType().GetProperty("CounterName");
    //    if (counterNameProperty != null)
    //    {
    //        string counterName = counterNameProperty.GetValue(performanceCounterTelemetry) as string;
    //        if (counterName == @"\Processor(_Total)\% Processor Time" || counterName == @"\Thread Count")
    //        {
    //            Console.WriteLine($"Counter: {counterName}, Value: {performanceCounterTelemetry.Value}");
    //        }
    //    }

    //    // Add or modify custom properties if needed
    //    performanceCounterTelemetry.Properties["CustomCounterProperty"] = "CustomValue";
    //}
}

internal sealed class MyCustomMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Logging setup
        var logger = context.GetLogger<MyCustomMiddleware>();
        logger.LogInformation("Starting Logging from MyCustomMiddleware!!!");

        // Attempt to get the HTTP request data
        var reqData = await context.GetHttpRequestDataAsync();
        var body = await new StreamReader(reqData.Body).ReadToEndAsync();

        Console.WriteLine($"Request Body: {body}");

        reqData.Body.Position = 0;
    }
}