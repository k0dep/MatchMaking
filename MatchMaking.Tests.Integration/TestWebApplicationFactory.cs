using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MatchMaking.IntegrationTests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _bootstrapServers;

    public TestWebApplicationFactory(IConnectionMultiplexer redis, string bootstrapServers)
    {
        _redis = redis;
        _bootstrapServers = bootstrapServers;
        ClientOptions.BaseAddress = new Uri("http://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
}