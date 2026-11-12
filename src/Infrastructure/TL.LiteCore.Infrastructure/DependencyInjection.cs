using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TL.LiteCore.Application.Abstractions;
using TL.LiteCore.Infrastructure.Persistence;

namespace TL.LiteCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? "Data Source=litecore.db";

        services.AddDbContext<LiteDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<LiteDbContext>());

        return services;
    }
}
