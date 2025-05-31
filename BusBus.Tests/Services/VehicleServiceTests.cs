#nullable enable
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusBus.Tests.Services
{
    [TestClass]
    public class VehicleServiceTests
    {
        private static IServiceProvider? _serviceProvider;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Setup DI container for tests with in-memory database
            var services = new ServiceCollection();

            // Configure in-memory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("VehicleServiceTests"));

            // Register the service
            services.AddScoped<IVehicleService, VehicleService>();

            _serviceProvider = services.BuildServiceProvider();

            // Seed the database with test data
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedDatabase(dbContext);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static void SeedDatabase(AppDbContext dbContext)
        {
            // Create some test vehicles
            var vehicles = new List<Vehicle>
            {
                new Vehicle
                {
                    VehicleId = 1,
                    VehicleGuid = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Number = "BUS001",
                    Name = "Express Bus 1",
                    Capacity = 45,
                    Model = "City Express",
                    LicensePlate = "BUS-001",
                    Year = 2020,
                    IsActive = true
                },
                new Vehicle
                {
                    VehicleId = 2,
                    VehicleGuid = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Number = "BUS002",
                    Name = "Express Bus 2",
                    Capacity = 50,
                    Model = "Highway Cruiser",
                    LicensePlate = "BUS-002",
                    Year = 2019,
                    IsActive = true
                },
                new Vehicle
                {
                    VehicleId = 3,
                    VehicleGuid = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Number = "BUS003",
                    Name = "Express Bus 3",
                    Capacity = 40,
                    Model = "City Mini",
                    LicensePlate = "BUS-003",
                    Year = 2021,
                    IsActive = false
                }
            };

            dbContext.Vehicles.AddRange(vehicles);
            dbContext.SaveChanges();
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task GetByIdAsync_ExistingId_ReturnsVehicle()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();
            var expectedId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Act
            var vehicle = await vehicleService.GetByIdAsync(expectedId);

            // Assert
            Assert.IsNotNull(vehicle, "Vehicle should not be null");
            Assert.AreEqual(expectedId, vehicle.Id, "Vehicle ID should match expected ID");
            Assert.AreEqual("BUS001", vehicle.Number, "Vehicle number should match expected value");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();
            var nonExistingId = Guid.Parse("00000000-0000-0000-0000-999999999999");

            // Act
            var vehicle = await vehicleService.GetByIdAsync(nonExistingId);

            // Assert
            Assert.IsNull(vehicle, "Vehicle should be null for non-existing ID");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task GetAllVehiclesAsync_ReturnsAllVehicles()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

            // Act
            var vehicles = await vehicleService.GetAllVehiclesAsync();

            // Assert
            Assert.IsNotNull(vehicles, "Vehicles collection should not be null");
            Assert.AreEqual(3, vehicles.Count, "Should return all 3 vehicles");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task GetVehiclesAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

            // Act - get the first page with 2 items
            var vehiclesPage1 = await vehicleService.GetVehiclesAsync(0, 2);

            // Assert
            Assert.IsNotNull(vehiclesPage1, "Vehicles collection should not be null");
            Assert.AreEqual(2, vehiclesPage1.Count, "Should return 2 vehicles for the first page");

            // Act - get the second page with remaining items
            var vehiclesPage2 = await vehicleService.GetVehiclesAsync(2, 2);

            // Assert
            Assert.IsNotNull(vehiclesPage2, "Vehicles collection should not be null");
            Assert.AreEqual(1, vehiclesPage2.Count, "Should return 1 vehicle for the second page");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task CreateAsync_ValidVehicle_PersistsToDatabase()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();
            var newVehicle = new Vehicle
            {
                VehicleGuid = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Number = "BUS004",
                Name = "New Test Bus",
                Capacity = 55,
                Model = "Test Model",
                LicensePlate = "BUS-004",
                Year = 2022,
                IsActive = true
            };

            // Act
            var createdVehicle = await vehicleService.CreateAsync(newVehicle);
            var retrievedVehicle = await vehicleService.GetByIdAsync(newVehicle.Id);

            // Assert
            Assert.IsNotNull(createdVehicle, "Created vehicle should not be null");
            Assert.IsNotNull(retrievedVehicle, "Retrieved vehicle should not be null");
            Assert.AreEqual(newVehicle.Id, retrievedVehicle!.Id, "Vehicle ID should match");
            Assert.AreEqual(newVehicle.Number, retrievedVehicle.Number, "Vehicle number should match");
            Assert.AreEqual(newVehicle.Capacity, retrievedVehicle.Capacity, "Vehicle capacity should match");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task UpdateAsync_ExistingVehicle_UpdatesInDatabase()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();
            var vehicleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var vehicle = await vehicleService.GetByIdAsync(vehicleId);
            Assert.IsNotNull(vehicle, "Test vehicle should exist");

            // Modify the vehicle
            vehicle!.Name = "Updated Bus Name";
            vehicle.Capacity = 60;
            vehicle.Model = "Updated Model";

            // Act
            await vehicleService.UpdateAsync(vehicle);
            var updatedVehicle = await vehicleService.GetByIdAsync(vehicleId);

            // Assert
            Assert.IsNotNull(updatedVehicle, "Updated vehicle should not be null");
            Assert.AreEqual("Updated Bus Name", updatedVehicle!.Name, "Vehicle name should be updated");
            Assert.AreEqual(60, updatedVehicle.Capacity, "Vehicle capacity should be updated");
            Assert.AreEqual("Updated Model", updatedVehicle.Model, "Vehicle model should be updated");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public async Task DeleteAsync_ExistingVehicle_RemovesFromDatabase()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();
            var vehicleId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var vehicle = await vehicleService.GetByIdAsync(vehicleId);
            Assert.IsNotNull(vehicle, "Test vehicle should exist before deletion");

            // Act
            await vehicleService.DeleteAsync(vehicleId);
            var deletedVehicle = await vehicleService.GetByIdAsync(vehicleId);

            // Assert
            Assert.IsNull(deletedVehicle, "Vehicle should be null after deletion");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public void ValidateEntity_ValidVehicle_ReturnsTrue()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();
            var validVehicle = new Vehicle
            {
                Number = "BUS005",
                Capacity = 50
            };

            // Act
            var result = vehicleService.ValidateEntity(validVehicle);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should pass for valid vehicle");
            Assert.AreEqual(string.Empty, result.ErrorMessage, "Error message should be empty");
        }

        [TestMethod]
        [TestCategory("Services")]
        [TestCategory("Vehicle")]
        [Timeout(15000)]
        public void ValidateEntity_InvalidVehicle_ReturnsFalse()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

            // Vehicle with no number
            var invalidVehicle1 = new Vehicle
            {
                Number = "",
                Capacity = 50
            };

            // Vehicle with invalid capacity
            var invalidVehicle2 = new Vehicle
            {
                Number = "BUS006",
                Capacity = 0
            };

            // Act
            var result1 = vehicleService.ValidateEntity(invalidVehicle1);
            var result2 = vehicleService.ValidateEntity(invalidVehicle2);

            // Assert
            Assert.IsFalse(result1.IsValid, "Validation should fail for vehicle with no number");
            Assert.AreEqual("Vehicle number is required.", result1.ErrorMessage);

            Assert.IsFalse(result2.IsValid, "Validation should fail for vehicle with invalid capacity");
            Assert.AreEqual("Capacity must be greater than 0.", result2.ErrorMessage);
        }
    }
}
