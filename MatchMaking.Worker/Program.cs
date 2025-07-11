using MatchMaking.Worker;

if (args is ["healthcheck"])
{
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
return 0;