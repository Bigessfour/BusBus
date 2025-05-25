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
        }        protected override void OnModelCreating(ModelBuilder modelBuilder)
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
            });            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Number).IsRequired();
            });

            // Seed initial data for drivers
            modelBuilder.Entity<Driver>().HasData(
                new Driver { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), FirstName = "John", LastName = "Smith", LicenseNumber = "DL123456" },
                new Driver { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), FirstName = "Mary", LastName = "Johnson", LicenseNumber = "DL234567" },
                new Driver { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), FirstName = "Robert", LastName = "Brown", LicenseNumber = "DL345678" },
                new Driver { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), FirstName = "Lisa", LastName = "Davis", LicenseNumber = "DL456789" },
                new Driver { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), FirstName = "Michael", LastName = "Wilson", LicenseNumber = "DL567890" }
            );            // Seed initial data for vehicles
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Number = "101", Capacity = 72, Model = "Blue Bird All American FE", LicensePlate = "BUS-101", IsActive = true },
                new Vehicle { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Number = "102", Capacity = 66, Model = "Thomas C2 Jouley", LicensePlate = "BUS-102", IsActive = true },
                new Vehicle { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Number = "103", Capacity = 78, Model = "IC Bus CE Series", LicensePlate = "BUS-103", IsActive = true },
                new Vehicle { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Number = "104", Capacity = 72, Model = "Blue Bird Vision", LicensePlate = "BUS-104", IsActive = false },
                new Vehicle { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Number = "105", Capacity = 90, Model = "Thomas HDX", LicensePlate = "BUS-105", IsActive = true }
            );
        }
    }
}
