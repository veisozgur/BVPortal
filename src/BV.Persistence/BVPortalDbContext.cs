using BV.Domain.Authentication;
using BV.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence;

public sealed class BVPortalDbContext(DbContextOptions<BVPortalDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BVPortalDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
