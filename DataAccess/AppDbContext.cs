using BusBus.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBus.DataAccess
{
    // This interface is added to make AppDbContext easier to mock in tests
    public interface IAppDbContext : IDisposable
    {
        DbSet<Route> Routes { get; }
        DbSet<Driver> Drivers { get; }
        DbSet<Vehicle> Vehicles { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public class AppDbContext : DbContext, IAppDbContext
    {
        public DbSet<Route> Routes { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            modelBuilder.Entity<Route>(entity =>
            {
                entity.Property(e => e.AMStartingMileage).HasColumnType("int");
                entity.Property(e => e.AMEndingMileage).HasColumnType("int");
                entity.Property(e => e.AMRiders).HasColumnType("int");
                entity.Property(e => e.PMStartMileage).HasColumnType("int");
                entity.Property(e => e.PMEndingMileage).HasColumnType("int");
                entity.Property(e => e.PMRiders).HasColumnType("int");
                entity.HasOne(e => e.Driver).WithMany().OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Vehicle).WithMany().OnDelete(DeleteBehavior.SetNull);
            });
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
            });
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BusNumber).IsRequired();
            });
        }
    }
}
