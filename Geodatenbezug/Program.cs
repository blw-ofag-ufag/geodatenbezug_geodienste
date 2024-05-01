using System.Net.Http.Headers;
using System.Text;
using Geodatenbezug;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder => { }, options =>
    {
        options.EnableUserCodeException = true;
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient("GeodiensteApi", httpClient =>
        {
            var geodiensteUser = Environment.GetEnvironmentVariable("AuthUser") ?? throw new InvalidOperationException("AuthUser environment variable must be set.");
            var geodienstePw = Environment.GetEnvironmentVariable("AuthPw") ?? throw new InvalidOperationException("AuthPw environment variable must be set.");
            var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{geodiensteUser}:{geodienstePw}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        });
        services.AddTransient<Processor>();
        services.AddTransient<IMailService>();
        services.AddTransient<IGeodiensteApi, GeodiensteApi>();
        services.AddTransient<IAzureStorage, AzureStorage>();
        services = services.Configure<LoggerFilterOptions>(options =>
        {
            var logFilterToRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (logFilterToRemove is not null)
            {
                options.Rules.Remove(logFilterToRemove);
            }
        });
    })
    .Build();

host.Run();
