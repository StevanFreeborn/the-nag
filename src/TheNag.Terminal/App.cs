using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using TheNag.Terminal.Evaluation;
using TheNag.Terminal.Examples.ControlMapping;

namespace TheNag.Terminal;

internal sealed class App(
  Optimizer optimizer,
  IConfiguration configuration
) : IHostedService
{
  private readonly Optimizer _optimizer = optimizer;
  private readonly IConfiguration _configuration = configuration;

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    var scenario = _configuration.GetValue<string>("scenario") ?? nameof(ControlMappingScenario);
    var optimizedPrompt = scenario switch
    {
      nameof(ControlMappingScenario) => await _optimizer.RunAsync(ControlMappingScenario.New()),
      _ => throw new InvalidOperationException($"Unknown scenario: {scenario}")
    };

    Console.WriteLine("\n=== Optimization Complete ===");
    Console.WriteLine($"{optimizedPrompt}");
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}