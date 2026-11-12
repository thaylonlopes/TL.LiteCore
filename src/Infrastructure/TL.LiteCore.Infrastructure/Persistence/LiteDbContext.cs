using Microsoft.EntityFrameworkCore;
using TL.LiteCore.Application.Abstractions;

namespace TL.LiteCore.Infrastructure.Persistence;

public sealed class LiteDbContext(DbContextOptions<LiteDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
