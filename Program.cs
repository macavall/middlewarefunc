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
        if (telemetry is RequestTelemetry requestTelemetry)
        {
            // Example: Modify request telemetry
            AlterTelemetry(requestTelemetry);
        }
        else if (telemetry is DependencyTelemetry dependencyTelemetry)
        {
            // Example: Modify dependency telemetry (outgoing REST requests)
            AlterTelemetry(dependencyTelemetry);
        }
    }

    private void AlterTelemetry(object telemetry)
    {
        // Use reflection to alter the telemetry properties
        var type = telemetry.GetType();

        // Modify properties using reflection, for example:
        var urlProperty = type.GetProperty("Url");
        if (urlProperty != null)
        {
            var urlValue = urlProperty.GetValue(telemetry) as Uri;
            if (urlValue != null)
            {
                // Modify the URL or other properties
                urlProperty.SetValue(telemetry, new Uri(urlValue.ToString().Replace("http:", "https:")));
            }
        }

        // Modify other properties as needed
        var responseCodeProperty = type.GetProperty("ResponseCode");
        if (responseCodeProperty != null)
        {
            var responseCodeValue = responseCodeProperty.GetValue(telemetry) as string;
            if (!string.IsNullOrEmpty(responseCodeValue) && responseCodeValue == "200")
            {
                responseCodeProperty.SetValue(telemetry, "201");
            }
        }

        // Add custom properties
        var customDimensionsProperty = type.GetProperty("Properties");
        if (customDimensionsProperty != null)
        {
            var properties = customDimensionsProperty.GetValue(telemetry) as IDictionary<string, string>;
            if (properties != null)
            {
                properties["CustomProperty"] = "CustomValue";
            }
        }
    }
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

//public static void Main()
//{
//    var host = new HostBuilder()
//        .ConfigureFunctionsWorkerDefaults()
//        .ConfigureAppConfiguration(configuration =>
//        {
//            var config = configuration.SetBasePath(Directory.GetCurrentDirectory())
//                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

//            var builtConfig = config.Build();
//        })
//        .ConfigureServices(services =>
//        {
//            services.AddSingleton<IMemoryStore>(new VolatileMemoryStore());

//            // return JSON with expected lowercase naming
//            services.Configure<JsonSerializerOptions>(options =>
//            {
//                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
//            });
//        })
//        .Build();

//    host.Run();
//}