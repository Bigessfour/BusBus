using System;
using System.Collections.Generic;
using System.IO;
using BusBus.Models;
using BusBus.UI.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.UI.Common
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    public class CustomFieldsManagerTests
    {
        private string _tempConfigPath;

        [TestInitialize]
        public void SetUp()
        {
            // Create a temporary test custom fields config file
            _tempConfigPath = Path.Combine(Path.GetTempPath(), "CustomFields.json");

            // Create test JSON data
            string testJson = @"{
                ""Driver"": {
                    ""preferredRoute"": {
                        ""Name"": ""preferredRoute"",
                        ""Label"": ""Preferred Route"",
                        ""Type"": ""select"",
                        ""Required"": true,
                        ""DefaultValue"": ""Downtown"",
                        ""Options"": [""Downtown"", ""Uptown"", ""Suburban""]
                    },
                    ""shirtSize"": {
                        ""Name"": ""shirtSize"",
                        ""Label"": ""Shirt Size"",
                        ""Type"": ""select"",
                        ""Required"": false,
                        ""DefaultValue"": ""M"",
                        ""Options"": [""S"", ""M"", ""L"", ""XL"", ""XXL""]
                    }
                },
                ""Vehicle"": {
                    ""maintenanceSchedule"": {
                        ""Name"": ""maintenanceSchedule"",
                        ""Label"": ""Maintenance Schedule"",
                        ""Type"": ""select"",
                        ""Required"": true,
                        ""DefaultValue"": ""Monthly"",
                        ""Options"": [""Weekly"", ""Bi-weekly"", ""Monthly"", ""Quarterly""]
                    }
                }
            }";

            File.WriteAllText(_tempConfigPath, testJson);

            // Set private field for config path via reflection
            var configPathField = typeof(CustomFieldsManager).GetField("ConfigPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            configPathField?.SetValue(null, _tempConfigPath);

            // Reset cached fields
            var fieldsField = typeof(CustomFieldsManager).GetField("_customFields",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            fieldsField?.SetValue(null, null);
        }

        [TestCleanup]
        public void TearDown()
        {
            // Clean up test file
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
        }

        [TestMethod]
        // Description: Test loading custom fields from configuration
        public void LoadCustomFields_WithValidConfig_ShouldLoadCorrectly()
        {
            // Act
            var customFields = CustomFieldsManager.LoadCustomFields();

            // Assert
            customFields.Should().NotBeNull();
            customFields.Should().ContainKey("Driver");
            customFields.Should().ContainKey("Vehicle");

            // Check Driver fields
            customFields["Driver"].Should().ContainKey("preferredRoute");
            customFields["Driver"]["preferredRoute"].Label.Should().Be("Preferred Route");
            customFields["Driver"]["preferredRoute"].Type.Should().Be("select");
            customFields["Driver"]["preferredRoute"].Required.Should().BeTrue();
            customFields["Driver"]["preferredRoute"].Options.Should().Contain("Downtown");

            // Check Vehicle fields
            customFields["Vehicle"].Should().ContainKey("maintenanceSchedule");
            customFields["Vehicle"]["maintenanceSchedule"].Label.Should().Be("Maintenance Schedule");
            customFields["Vehicle"]["maintenanceSchedule"].Options.Should().HaveCount(4);
        }

        [TestMethod]
        // Description: Test getting custom fields for a specific entity type
        public void GetCustomFields_ForEntityType_ShouldReturnCorrectFields()
        {
            // Act
            var driverFields = CustomFieldsManager.GetCustomFields("Driver");
            var vehicleFields = CustomFieldsManager.GetCustomFields("Vehicle");
            var nonExistentFields = CustomFieldsManager.GetCustomFields("NonExistent");

            // Assert
            driverFields.Should().NotBeNull();
            driverFields.Should().HaveCount(2);
            driverFields.Should().ContainKey("preferredRoute");
            driverFields.Should().ContainKey("shirtSize");

            vehicleFields.Should().NotBeNull();
            vehicleFields.Should().HaveCount(1);
            vehicleFields.Should().ContainKey("maintenanceSchedule");

            nonExistentFields.Should().NotBeNull();
            nonExistentFields.Should().BeEmpty();
        }

        [TestMethod]
        // Description: Test reloading custom fields after configuration changes
        public void ReloadCustomFields_AfterConfigChange_ShouldReflectChanges()
        {
            // Arrange - Initial load
            var initialFields = CustomFieldsManager.GetCustomFields("Driver");
            initialFields.Should().HaveCount(2);

            // Act - Change config and reload
            string updatedJson = @"{
                ""Driver"": {
                    ""preferredRoute"": {
                        ""Name"": ""preferredRoute"",
                        ""Label"": ""Preferred Route"",
                        ""Type"": ""select"",
                        ""Required"": true,
                        ""DefaultValue"": ""Downtown"",
                        ""Options"": [""Downtown"", ""Uptown"", ""Suburban""]
                    },
                    ""shirtSize"": {
                        ""Name"": ""shirtSize"",
                        ""Label"": ""Shirt Size"",
                        ""Type"": ""select"",
                        ""Required"": false,
                        ""DefaultValue"": ""M"",
                        ""Options"": [""S"", ""M"", ""L"", ""XL"", ""XXL""]
                    },
                    ""newField"": {
                        ""Name"": ""newField"",
                        ""Label"": ""New Test Field"",
                        ""Type"": ""text"",
                        ""Required"": false,
                        ""DefaultValue"": """"
                    }
                }
            }";

            File.WriteAllText(_tempConfigPath, updatedJson);

            // Force reload
            CustomFieldsManager.ReloadCustomFields();

            // Assert
            var updatedFields = CustomFieldsManager.GetCustomFields("Driver");
            updatedFields.Should().HaveCount(3);
            updatedFields.Should().ContainKey("newField");
            updatedFields["newField"].Label.Should().Be("New Test Field");
        }

        [TestMethod]
        // Description: Test handling of missing configuration file
        public void LoadCustomFields_WithMissingConfig_ShouldReturnEmptyDictionary()
        {
            // Arrange - Delete config file
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }

            // Force reload
            CustomFieldsManager.ReloadCustomFields();

            // Act
            var fields = CustomFieldsManager.LoadCustomFields();

            // Assert
            fields.Should().NotBeNull();
            fields.Should().BeEmpty();
        }
    }
}
