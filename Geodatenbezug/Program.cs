using System.Net.Http.Headers;
using System.Text;
using Geodatenbezug;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
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
        services.AddTransient<Processing>();
        services.AddTransient<IGeodiensteApi, GeodiensteApi>();
    })
    .Build();

host.Run();
