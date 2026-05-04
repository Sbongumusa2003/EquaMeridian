using Microsoft.EntityFrameworkCore;

namespace EquaMeridian.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordReset> PasswordReset => Set<PasswordReset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Listing>()
            .HasOne(l => l.Supplier)
            .WithMany(u => u.Listings)
            .HasForeignKey(l => l.SupplierID);

        modelBuilder.Entity<Listing>()
            .Property(l => l.DailyRateZAR)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Listing>()
            .Property(l => l.WeeklyRateZAR)
            .HasColumnType("decimal(18,2)");

        // Remove duplicate — only ONE HasKey call needed
        modelBuilder.Entity<AuditLog>()
            .HasKey(a => a.AuditID);

        // Configure PasswordReset → User relationship
        modelBuilder.Entity<PasswordReset>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}