// Suppress CS8602: Possible dereference of a null reference. Safe in test code with controlled setup.
#pragma warning disable CS8602
// Suppress CS8618: Non-nullable field is uninitialized. This is safe for test code with [SetUp] initialization.
#pragma warning disable CS8618
using NUnit.Framework;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using BusBus.UI;

namespace BusBus.Tests
{
    [TestFixture]
    public class ThemeManagerIntegrationTests
    {
        private string _originalThemeName;

        [SetUp]
        public void Setup()
        {
            // Remember original theme to restore it later
            _originalThemeName = ThemeManager.CurrentTheme.Name;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original theme after each test
            ThemeManager.SwitchTheme(_originalThemeName);
        }

        [Test]
        public void ThemeManager_SwitchTheme_ChangesCurrentTheme()
        {
            // Arrange
            string initialTheme = ThemeManager.CurrentTheme.Name;
            string targetTheme = initialTheme == "Dark" ? "Light" : "Dark";

            // Act
            ThemeManager.SwitchTheme(targetTheme);

            // Assert
            Assert.That(ThemeManager.CurrentTheme.Name, Is.EqualTo(targetTheme));
            Assert.That(ThemeManager.CurrentTheme.Name, Is.Not.EqualTo(initialTheme));
        }

        [Test]
        public void ThemeManager_SetTheme_ChangesCurrentTheme()
        {
            // Arrange
            string initialTheme = ThemeManager.CurrentTheme.Name;
            string targetTheme = initialTheme == "Dark" ? "Light" : "Dark";

            // Act
            ThemeManager.SetTheme(targetTheme);

            // Assert
            Assert.That(ThemeManager.CurrentTheme.Name, Is.EqualTo(targetTheme));
            Assert.That(ThemeManager.CurrentTheme.Name, Is.Not.EqualTo(initialTheme));
        }

        [Test]
        public void ThemeManager_SwitchingThemes_AppliesCorrectColors()
        {
            // Arrange
            var form = new Form();
            var panel = new Panel();
            form.Controls.Add(panel);

            // Act - Switch to Light theme
            ThemeManager.SwitchTheme("Light");
            ThemeManager.RefreshTheme(form);
            Color lightBackColor = form.BackColor;

            // Switch to Dark theme
            ThemeManager.SwitchTheme("Dark");
            ThemeManager.RefreshTheme(form);
            Color darkBackColor = form.BackColor;

            // Assert
            Assert.That(lightBackColor, Is.EqualTo(ThemeColors.LightMainBackground));
            Assert.That(darkBackColor, Is.EqualTo(ThemeColors.DarkMainBackground));
            Assert.That(lightBackColor, Is.Not.EqualTo(darkBackColor), "Light and dark theme colors should be different");
        }

        [Test]
        public void ThemeManager_RegisterAndSwitchToCustomTheme_Works()
        {
            // Arrange
            const string customThemeName = "CustomTestTheme";

            // Register custom theme
            ThemeManager.RegisterTheme(customThemeName, () => new CustomTestTheme());

            // Act
            ThemeManager.SwitchTheme(customThemeName);

            // Assert
            Assert.That(ThemeManager.CurrentTheme.Name, Is.EqualTo(customThemeName));
            Assert.That(ThemeManager.CurrentTheme.MainBackground, Is.EqualTo(Color.Purple));
        }

        [Test]
        public void ThemeManager_ThemeChanged_EventIsFired()
        {
            // Arrange
            string initialTheme = ThemeManager.CurrentTheme.Name;
            string targetTheme = initialTheme == "Dark" ? "Light" : "Dark";
            bool eventFired = false;

            void EventHandler(object? sender, EventArgs e)
            {
                eventFired = true;
            }

            // Subscribe to event
            ThemeManager.ThemeChanged += EventHandler;

            try
            {
                // Act
                ThemeManager.SwitchTheme(targetTheme);

                // Assert
                Assert.That(eventFired, Is.True, "ThemeChanged event should be fired when switching themes");
            }
            finally
            {
                // Unsubscribe to prevent test interference
                ThemeManager.ThemeChanged -= EventHandler;
            }
        }

        [Test]
        public void ThemeManager_SwitchToInvalidTheme_ThrowsArgumentException()
        {
            // Arrange
            const string nonExistentTheme = "NonExistentTheme";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => ThemeManager.SwitchTheme(nonExistentTheme));
            Assert.That(ex.Message, Does.Contain(nonExistentTheme));
        }

        // Custom theme for testing
        private class CustomTestTheme : Theme
        {
            public override string Name => "CustomTestTheme";
            public override Color MainBackground => Color.Purple;
            public override Color SidePanelBackground => Color.Violet;
            public override Color HeadlineBackground => Color.Indigo;
            public override Color CardBackground => Color.Lavender;
            public override Color GridBackground => Color.Plum;
            public override Color ButtonBackground => Color.MediumPurple;
            public override Color ButtonHoverBackground => Color.DarkViolet;
            public override Color CardText => Color.Black;
            public override Color HeadlineText => Color.White;
            public override Color TextBoxBackground => Color.LavenderBlush;
        }
    }
}
