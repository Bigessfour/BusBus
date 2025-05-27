using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Config;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace BusBus.Tests.DataAccess
{
    [TestFixture]
    [Category(TestCategories.Database)]
    [Category(TestCategories.Unit)]
    public class AppDbContextTests : TestBase
    {
        private AppDbContext _dbContext;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _dbContext = ServiceProvider.GetRequiredService<AppDbContext>();
        }

        [Test]
        [Description("Test that AppDbContext correctly configures the model")]
        public void AppDbContext_ModelConfiguration_ShouldBeCorrect()
        {
            // Act
            var model = _dbContext.Model;

            // Assert
            // Check entity types
            var entityTypes = model.GetEntityTypes().Select(t => t.ClrType).ToList();
            entityTypes.Should().Contain(typeof(Route));
            entityTypes.Should().Contain(typeof(Driver));
            entityTypes.Should().Contain(typeof(Vehicle));
            entityTypes.Should().Contain(typeof(CustomField));

            // Check Route entity configuration
            var routeEntity = model.FindEntityType(typeof(Route));
            routeEntity.Should().NotBeNull();

            var routeProperties = routeEntity.GetProperties().ToList();
            routeProperties.Should().Contain(p => p.Name == "Id");
            routeProperties.Should().Contain(p => p.Name == "Name");
            routeProperties.Should().Contain(p => p.Name == "AMStartingMileage");

            // Check navigation properties
            var routeNavigations = routeEntity.GetNavigations().ToList();
            routeNavigations.Should().Contain(n => n.Name == "Driver");
            routeNavigations.Should().Contain(n => n.Name == "Vehicle");

            // Check Driver entity configuration
            var driverEntity = model.FindEntityType(typeof(Driver));
            driverEntity.Should().NotBeNull();

            var driverProperties = driverEntity.GetProperties().ToList();
            driverProperties.Should().Contain(p => p.Name == "Id");
            driverProperties.Should().Contain(p => p.Name == "FirstName");
            driverProperties.Should().Contain(p => p.Name == "LastName");

            // Verify PersonalDetails is properly ignored
            var personalDetailsProperty = driverEntity.FindProperty("PersonalDetails");
            personalDetailsProperty.Should().BeNull("PersonalDetails should be ignored in the model");

            // Check EmergencyContact is properly ignored
            var emergencyContactProperty = driverEntity.FindProperty("EmergencyContact");
            emergencyContactProperty.Should().BeNull("EmergencyContact should be ignored in the model");

            // But the JSON-backed columns should exist
            driverProperties.Should().Contain(p => p.Name == "PersonalDetailsJson");
            driverProperties.Should().Contain(p => p.Name == "EmergencyContactJson");
        }

        [Test]
        [Description("Test that seeded data is properly loaded")]
        public async Task AppDbContext_SeedData_ShouldBeLoaded()
        {
            // Arrange
            await _dbContext.Database.EnsureCreatedAsync();

            // Act
            var drivers = await _dbContext.Drivers.ToListAsync();
            var vehicles = await _dbContext.Vehicles.ToListAsync();

            // Assert
            drivers.Should().NotBeEmpty("Seeded drivers should be loaded");
            vehicles.Should().NotBeEmpty("Seeded vehicles should be loaded");

            // Check specific seeded driver data
            var johnSmith = drivers.FirstOrDefault(d => d.FirstName == "John" && d.LastName == "Smith");
            johnSmith.Should().NotBeNull();
            johnSmith.LicenseNumber.Should().Be("DL123456");

            // Check specific seeded vehicle data
            var vehicle101 = vehicles.FirstOrDefault(v => v.Number == "101");
            vehicle101.Should().NotBeNull();
            vehicle101.Capacity.Should().Be(72);
            vehicle101.IsActive.Should().BeTrue();
        }

        [Test]
        [Description("Test JSON serialization and deserialization for Driver properties")]
        public async Task AppDbContext_JsonProperties_ShouldSerializeAndDeserializeCorrectly()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "JSON",
                LastName = "Test",
                LicenseNumber = "JSON-TEST",
                PersonalDetails = new PersonalDetails
                {
                    HairColor = "Black",
                    EyeColor = "Green",
                    Height = 175,
                    BloodType = "AB+",
                    Allergies = new System.Collections.Generic.List<string> { "Nuts", "Pollen" },
                    CustomFields = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "Preference", "Window Seat" },
                        { "Score", 95 }
                    }
                },
                EmergencyContact = new EmergencyContact
                {
                    Name = "Emergency Person",
                    Phone = "555-EMER",
                    Relationship = "Sibling",
                    Address = "123 Emergency Ln"
                }
            };

            // Act - Save to database
            _dbContext.Drivers.Add(driver);
            await _dbContext.SaveChangesAsync();

            // Clear the context to ensure we're getting a fresh entity
            _dbContext.ChangeTracker.Clear();

            // Act - Retrieve from database
            var retrievedDriver = await _dbContext.Drivers.FindAsync(driver.Id);

            // Assert
            retrievedDriver.Should().NotBeNull();

            // Check PersonalDetails deserialization
            retrievedDriver.PersonalDetails.Should().NotBeNull();
            retrievedDriver.PersonalDetails.HairColor.Should().Be("Black");
            retrievedDriver.PersonalDetails.EyeColor.Should().Be("Green");
            retrievedDriver.PersonalDetails.Height.Should().Be(175);
            retrievedDriver.PersonalDetails.Allergies.Should().Contain("Nuts");
            retrievedDriver.PersonalDetails.CustomFields.Should().ContainKey("Preference");
            retrievedDriver.PersonalDetails.CustomFields["Preference"].Should().Be("Window Seat");

            // Check EmergencyContact deserialization
            retrievedDriver.EmergencyContact.Should().NotBeNull();
            retrievedDriver.EmergencyContact.Name.Should().Be("Emergency Person");
            retrievedDriver.EmergencyContact.Phone.Should().Be("555-EMER");
            retrievedDriver.EmergencyContact.Relationship.Should().Be("Sibling");
        }

        [Test]
        [Description("Test that database schema matches SQL Information configuration")]
        public void AppDbContext_ShouldMatchSqlInformationSchema()
        {
            // Arrange
            var configuration = GetTestConfiguration();
            var sqlInfo = configuration.GetSqlInformation();
            var model = _dbContext.Model;

            // Act & Assert - Routes table
            var routeEntity = model.FindEntityType(typeof(Route));
            routeEntity.Should().NotBeNull("Routes table should exist");

            var routePrimaryKey = routeEntity.FindPrimaryKey();
            routePrimaryKey.Should().NotBeNull();
            routePrimaryKey.Properties.Should().HaveCount(1);
            routePrimaryKey.Properties[0].Name.Should().Be("Id");

            // Verify Routes columns exist
            var routeProperties = routeEntity.GetProperties().Select(p => p.Name).ToList();
            routeProperties.Should().Contain("Name");
            routeProperties.Should().Contain("RouteDate");
            routeProperties.Should().Contain("AMStartingMileage");
            routeProperties.Should().Contain("AMEndingMileage");
            routeProperties.Should().Contain("AMRiders");
            routeProperties.Should().Contain("PMStartMileage");
            routeProperties.Should().Contain("PMEndingMileage");
            routeProperties.Should().Contain("PMRiders");
            routeProperties.Should().Contain("DriverId");
            routeProperties.Should().Contain("VehicleId");
            routeProperties.Should().Contain("RowVersion");

            // Act & Assert - Drivers table
            var driverEntity = model.FindEntityType(typeof(Driver));
            driverEntity.Should().NotBeNull("Drivers table should exist");

            var driverPrimaryKey = driverEntity.FindPrimaryKey();
            driverPrimaryKey.Should().NotBeNull();
            driverPrimaryKey.Properties[0].Name.Should().Be("Id");

            var driverProperties = driverEntity.GetProperties().Select(p => p.Name).ToList();
            driverProperties.Should().Contain("FirstName");
            driverProperties.Should().Contain("LastName");
            driverProperties.Should().Contain("Name");
            driverProperties.Should().Contain("LicenseNumber");
            driverProperties.Should().Contain("PhoneNumber");
            driverProperties.Should().Contain("Email");

            // Act & Assert - Vehicles table
            var vehicleEntity = model.FindEntityType(typeof(Vehicle));
            vehicleEntity.Should().NotBeNull("Vehicles table should exist");

            var vehiclePrimaryKey = vehicleEntity.FindPrimaryKey();
            vehiclePrimaryKey.Should().NotBeNull();
            vehiclePrimaryKey.Properties[0].Name.Should().Be("Id");

            var vehicleProperties = vehicleEntity.GetProperties().Select(p => p.Name).ToList();
            vehicleProperties.Should().Contain("Number");
            vehicleProperties.Should().Contain("Name");
            vehicleProperties.Should().Contain("Capacity");
            vehicleProperties.Should().Contain("Model");
            vehicleProperties.Should().Contain("LicensePlate");
            vehicleProperties.Should().Contain("IsActive");

            // Verify relationships
            var routeNavigations = routeEntity.GetNavigations().Select(n => n.Name).ToList();
            routeNavigations.Should().Contain("Driver");
            routeNavigations.Should().Contain("Vehicle");
        }

        [Test]
        [Description("Test that seed data GUIDs from SQL Information are valid")]
        public async Task AppDbContext_SeedDataGuids_ShouldBeValid()
        {
            // Arrange
            var configuration = GetTestConfiguration();
            var sqlInfo = configuration.GetSqlInformation();

            // Act & Assert - Driver seed data
            foreach (var seedDriver in sqlInfo.SeedData.Drivers)
            {
                var driverGuid = Guid.Parse(seedDriver.Id);
                driverGuid.Should().NotBe(Guid.Empty, $"Driver {seedDriver.Name} should have a valid non-empty GUID");

                // Verify the driver exists in the database (if seeded)
                var existingDriver = await _dbContext.Drivers
                    .FirstOrDefaultAsync(d => d.Id == driverGuid);

                if (existingDriver != null)
                {
                    existingDriver.LicenseNumber.Should().Be(seedDriver.LicenseNumber);
                }
            }

            // Act & Assert - Vehicle seed data
            foreach (var seedVehicle in sqlInfo.SeedData.Vehicles)
            {
                var vehicleGuid = Guid.Parse(seedVehicle.Id);
                vehicleGuid.Should().NotBe(Guid.Empty, $"Vehicle {seedVehicle.Number} should have a valid non-empty GUID");

                // Verify vehicle properties match configuration
                seedVehicle.Number.Should().NotBeNullOrEmpty();
                seedVehicle.Model.Should().NotBeNullOrEmpty();
                seedVehicle.LicensePlate.Should().NotBeNullOrEmpty();
                seedVehicle.Capacity.Should().BeGreaterThan(0);
            }
        }

        [Test]
        [Description("Test that database configuration matches SQL Information")]
        public void AppDbContext_DatabaseConfiguration_ShouldMatchSqlInfo()
        {
            // Arrange
            var configuration = GetTestConfiguration();
            var sqlInfo = configuration.GetSqlInformation();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Act & Assert
            sqlInfo.DatabaseConfiguration.Database.Should().Be("BusBusDB");
            sqlInfo.DatabaseConfiguration.Server.Should().Be("localhost\\SQLEXPRESS");
            sqlInfo.DatabaseConfiguration.Authentication.Should().Be("Windows Authentication");
            sqlInfo.DatabaseConfiguration.Provider.Should().Be("SQL Server Express");
            sqlInfo.DatabaseConfiguration.Framework.Should().StartWith("Entity Framework Core");

            // Verify connection string contains expected values
            connectionString.Should().Contain("BusBusDB");
            connectionString.Should().Contain("SQLEXPRESS");
            connectionString.Should().Contain("Trusted_Connection=True");
        }

        private static IConfiguration GetTestConfiguration()
        {
            var projectDir = GetProjectDirectory();
            var configPath = Path.Combine(projectDir, "config", "appsettings.json");

            return new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();
        }

        private static string GetProjectDirectory()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var projectDir = Path.GetDirectoryName(currentDir);
            while (projectDir != null && !File.Exists(Path.Combine(projectDir, "BusBus.csproj")))
            {
                projectDir = Path.GetDirectoryName(projectDir);
            }
            return projectDir ?? currentDir;
        }
    }
}
