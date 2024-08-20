using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
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
        })
        .ConfigureLogging(logging =>
        {
            logging.AddFilter("Microsoft.*", LogLevel.Trace);
        })
        .Build();

        await host.RunAsync();
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