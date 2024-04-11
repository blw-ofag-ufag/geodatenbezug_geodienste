﻿using Geodatenbezug;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient();
        services.AddSingleton<Processing>();
        services.AddSingleton<IGeodiensteApi, GeodiensteApi>();
    })
    .Build();

host.Run();