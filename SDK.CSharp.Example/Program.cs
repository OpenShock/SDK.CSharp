using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SDK.CSharp.Example;
using Serilog;


var hostBuilder = Host.CreateApplicationBuilder();

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

Log.Logger = loggerConfiguration.CreateLogger();

hostBuilder.Logging.ClearProviders();
hostBuilder.Logging.AddSerilog();

var exampleType = typeof(IExample);
typeof(Program).Assembly.GetTypes().Where(x => x.IsClass && x.IsAssignableTo(exampleType)).ToList().ForEach(x =>
{
    hostBuilder.Services.TryAddEnumerable(new ServiceDescriptor(exampleType, x, ServiceLifetime.Singleton));
});

var config = hostBuilder.Configuration.Get<ExampleConfig>()!;
hostBuilder.Services.AddSingleton<ExampleConfig>(config);
Console.WriteLine(JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

var host = hostBuilder.Build();

var console = new Thread(() =>
{
    Console.WriteLine("OpenShock Example Program...");
    
    var examples = host.Services.GetServices<IExample>().ToImmutableArray();
    
    Console.WriteLine($"Available Examples:");

    for (int i = 0; i < examples.Length; i++)
    {
        var example = examples[i];
        Console.WriteLine($"[{i}] {example.GetType().Name}");
    }

    while (true)
    {
        var input = Console.ReadLine();
        if (!int.TryParse(input, out var choice))
        {
            Console.WriteLine("Invalid choice");
            continue;
        }

        var example = examples[choice];

        if (examples == null) Console.WriteLine("Invalid choice, example doesnt exist");
        
        Console.WriteLine($"Starting example {example.GetType().Name}");
        
        example.Start().Wait();
    }

})
{
    IsBackground = true
};

console.Start();

await host.RunAsync();