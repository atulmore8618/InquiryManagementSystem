using InquiryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Inquiry> Inquiries { get; set; }

        public DbSet<MarketRate> MarketRates { get; set; }

        public DbSet<EmailLog> EmailLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MarketRate>()
                .Property(x => x.Rate)
                .HasPrecision(18, 2);
        }
    }
}