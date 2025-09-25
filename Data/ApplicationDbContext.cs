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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.WaterMeterReading)
                .WithOne(m => m.Invoice)
                .HasForeignKey<MeterReading>(m => m.InvoiceId);
        }
    }
}
