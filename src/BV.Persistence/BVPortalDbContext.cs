using BV.Domain.Auditing;
using BV.Domain.Authentication;
using BV.Domain.Catalog;
using BV.Domain.Customers;
using BV.Domain.Notifications;
using BV.Domain.Orders;
using BV.Domain.Quotes;
using BV.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence;

public sealed class BVPortalDbContext(DbContextOptions<BVPortalDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<QuoteRequestItem> QuoteRequestItems => Set<QuoteRequestItem>();
    public DbSet<QuoteResponse> QuoteResponses => Set<QuoteResponse>();
    public DbSet<QuoteResponseItem> QuoteResponseItems => Set<QuoteResponseItem>();
    public DbSet<QuoteNotification> QuoteNotifications => Set<QuoteNotification>();
    public DbSet<QuoteOperationNote> QuoteOperationNotes => Set<QuoteOperationNote>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BVPortalDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
