using Microsoft.EntityFrameworkCore;
using PropertyInvoiceScanner.Core.Models;

namespace PropertyInvoiceScanner.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProcessedEmail> ProcessedEmails => Set<ProcessedEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEmail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OutlookEntryId).IsUnique();
        });
    }
}
