using Scalar.AspNetCore;
using TL.HealthCheck;
using TL.LiteCore.Application;
using TL.LiteCore.Infrastructure;
using TL.LiteCore.Infrastructure.Persistence;
using TL.MiddlewareLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLightweightHealthChecks();

var app = builder.Build();

app.UseExceptionHandling();
app.UseRequestTiming();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TL.LiteCore API");
        options.WithTheme(ScalarTheme.Moon);
    });
}

app.MapLightweightHealthChecks();

app.MapGet("/", () => Results.Ok(new
{
    status = "Healthy",
    system = "TL.LiteCore",
    type = "Synchronous Minimal API Template",
    timestampUtc = DateTime.UtcNow
}))
.WithName("RootHealth")
.WithTags("Health");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LiteDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();

public partial class Program;
