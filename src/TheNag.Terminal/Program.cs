using System.IO.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TheNag.Terminal;
using TheNag.Terminal.Evaluation;

await Host.CreateDefaultBuilder(args)
  .ConfigureAppConfiguration(
    static c => c.SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appsettings.json")
      .AddEnvironmentVariables()
  )
  .ConfigureLogging(static c => c.ClearProviders())
  .ConfigureServices(static (ctx, services) =>
  {
    var geminiSectionName = "GeminiApiKey";
    var geminiApiKey = ctx.Configuration.GetRequiredSection(geminiSectionName).Value
      ?? throw new InvalidOperationException($"{geminiSectionName} is required");

    services.AddHttpClient(Options.DefaultName)
      .AddStandardResilienceHandler(o =>
      {
        o.AttemptTimeout.Timeout = TimeSpan.FromMinutes(1);
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
      });

    services.AddSingleton(sp =>
    {
      var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
      return new GeminiService(httpClientFactory, geminiApiKey);
    });

    services.AddSingleton<IFileSystem>(new FileSystem());
    services.AddSingleton<Optimizer>();
    services.AddHostedService<App>();
  })
  .Build()
  .StartAsync();