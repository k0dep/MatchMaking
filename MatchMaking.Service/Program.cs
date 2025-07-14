using System.Text.Json;
using StackExchange.Redis;
using FluentValidation;
using MatchMaking.Service.Entities;
using MatchMaking.Service.HealthChecks;
using MatchMaking.Service.Validators;
using MatchMaking.Service.Workers;
using MediatR;
using OneOf.Types;
using MatchMaking.Service.Extensions;

if (args is ["healthcheck", not null])
{
    var client = new HttpClient();
    var response = await client.GetAsync($"{args[1]}/health");
    Console.WriteLine($"StatusCode: {response.StatusCode}");
    Console.WriteLine($"Body: {await response.Content.ReadAsStringAsync()}");
    return response.IsSuccessStatusCode ? 0 : 1;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

builder.Services.AddHostedService<MatchCompleteConsumer>();
builder.Services.AddConsumersAndProducers(builder.Configuration);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddTransient<IValidator<MatchInfoRequest>, MatchInfoRequestValidator>();
builder.Services.AddTransient<IValidator<MatchSearchRequest>, MatchSearchRequestValidator>();

builder.Services.AddApplicationRateLimiter();

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" })
    .AddCheck<KafkaHealthCheck>("kafka", tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseRateLimiter();

app.MapPost("/match-search", async (
        MatchSearchRequest request,
        IMediator mediator,
        CancellationToken ct) =>
    {
        var response = await mediator.Send(request, ct);

        return response.Match(
            (Success _) => Results.Created(),
            (ValidationError validationError) => Results.BadRequest(validationError.Messages));
    })
    .RequireRateLimiting("match-search");

app.MapGet("/match-info", async (
        string userId,
        IMediator mediator,
        CancellationToken ct) =>
    {
        var response = await mediator.Send(new MatchInfoRequest(userId), ct);

        return response.Match(
            (MatchInfo info) => Results.Ok(info),
            (NotFound _) => Results.NotFound(),
            (ValidationError validationError) => Results.BadRequest(validationError.Messages));
    });

app.MapHealthChecks("/health");

await app.RunAsync();
return 0;

public partial class Program;