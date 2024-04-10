using System;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace TopicProcessing
{
    public class TopicProcessor
    {
        private readonly ILogger _logger;

        public TopicProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TopicProcessor>();
        }

        [Function(nameof(HelloCities))]
        public static async Task<string> HelloCities([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            string result = "";
            result += await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo") + " ";
            result += await context.CallActivityAsync<string>(nameof(SayHello), "London") + " ";
            result += await context.CallActivityAsync<string>(nameof(SayHello), "Seattle");
            return result;
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string cityName, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(SayHello));
            logger.LogInformation("Saying hello to {name}", cityName);
            return $"Hello, {cityName}!";
        }

        [Function(nameof(Function1))]
        public static async Task Function1([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(Function1));
            try
            {
                logger.LogInformation("C# Timer trigger function executed at: {0}", DateTime.Now);
                logger.LogInformation("Version with MaxRev.Gdal.Core");
                GdalBase.ConfigureAll();
                Gdal.VersionInfo(null);
                logger.LogInformation("Gdal version: " + Gdal.VersionInfo(null));

                string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(HelloCities));
                logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Function1: {ex}");
            }   
            
        }
    }
}
