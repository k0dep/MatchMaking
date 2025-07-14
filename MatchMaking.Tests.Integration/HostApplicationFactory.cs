using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace MatchMaking.IntegrationTests;

public class HostApplicationFactory : WebApplicationFactory<MatchMaking.Worker.Program>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _bootstrapServers;

    public HostApplicationFactory(IConnectionMultiplexer redis, string bootstrapServers)
    {
        _redis = redis;
        _bootstrapServers = bootstrapServers;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder = builder.Configure(_ => { });

        builder.ConfigureServices(services =>
        {
            // Replace Redis multiplexer
            services.AddSingleton(_redis);
        });

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Redis:ConnectionString"] = "unused-because-injected", // required by Program.cs
                    ["Kafka:BootstrapServers"] = _bootstrapServers,
                    ["Kafka:GroupId"] = $"it-group-{Guid.NewGuid():N}"
                });
        });
    }

    public Task StartHostAsync()
    {
        var host = Services.GetRequiredService<IHost>();
        return host.StartAsync();
    }
    
    public async Task StopHostAsync()
    {
        var host = Services.GetRequiredService<IHost>();
        await host.StopAsync();
        await host.WaitForShutdownAsync();
    }
}