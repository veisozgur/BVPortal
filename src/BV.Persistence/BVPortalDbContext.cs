using BV.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence;

public sealed class BVPortalDbContext(DbContextOptions<BVPortalDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BVPortalDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
