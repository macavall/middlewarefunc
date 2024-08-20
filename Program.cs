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
            // Register our custom middlewares with the worker

            //workerApplication.UseMiddleware<ExceptionHandlingMiddleware>();

            workerApplication.UseMiddleware<MyCustomMiddleware>();

            //workerApplication.UseWhen<StampHttpHeaderMiddleware>((context) =>
            //{
            //    // We want to use this middleware only for http trigger invocations.
            //    return context.FunctionDefinition.InputBindings.Values
            //                  .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
            //});
        })
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
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