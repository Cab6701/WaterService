using Microsoft.EntityFrameworkCore;
using WaterService.Models;

namespace WaterService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> option)
            : base(option)
        {
        }

        public DbSet<Customer> Customers { get; set; } = default!;
        public DbSet<MeterReading> MeterReadings { get; set; } = default!;
        public DbSet<Invoice> Invoices { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<TierPrice> TierPrices { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.WaterMeterReading)
                .WithOne(m => m.Invoice)
                .HasForeignKey<Invoice>(i => i.MeterReadingId);

            // Invoice áp dụng một bản ghi TierPrice cụ thể để giữ nguyên giá lịch sử
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.AppliedTierPrice)
                .WithMany()
                .HasForeignKey(i => i.AppliedTierPriceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MeterReading>()
            .HasOne(mr => mr.Customer)
            .WithMany(c => c.MeterReadings)
            .HasForeignKey(mr => mr.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
