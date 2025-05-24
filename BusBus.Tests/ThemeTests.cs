using NUnit.Framework;
using System;
using System.Reflection;
using System.Windows.Forms;
using BusBus.UI;

namespace BusBus.Tests
{
    [TestFixture]
    public class ThemeTests
    {
        [Test]
        public void ApplyThemeToControl_ChangesControlColors()
        {
            // Arrange
            var form = new Form();
            var button = new Button();
            form.Controls.Add(button);

            // Use reflection to invoke private static method
            var themeManagerType = typeof(ThemeManager);
            var method = themeManagerType.GetMethod("ApplyThemeToControl", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "ApplyThemeToControl method not found");

            // Act
            method!.Invoke(null, new object[] { form });

            // Assert - check that the form and button have theme colors
            Assert.That(form.BackColor, Is.EqualTo(ThemeManager.CurrentTheme.MainBackground));
            Assert.That(button.BackColor, Is.EqualTo(ThemeManager.CurrentTheme.ButtonBackground));
        }

        [Test]
        public void ThemeRegistry_CanRegisterAndSwitchToNewTheme()
        {
            // Arrange
            var currentTheme = ThemeManager.CurrentTheme.Name;
            var customThemeName = "TestTheme";

            try
            {
                // Act - Register a custom theme
                ThemeManager.RegisterTheme(customThemeName, () => new TestTheme());
                ThemeManager.SwitchTheme(customThemeName);

                // Assert
                Assert.That(ThemeManager.CurrentTheme.Name, Is.EqualTo(customThemeName));
                Assert.That(ThemeManager.GetAvailableThemes(), Does.Contain(customThemeName));
            }
            finally
            {
                // Clean up - switch back to original theme
                ThemeManager.SwitchTheme(currentTheme);
            }
        }

        [Test]
        public void Theme_Implements_IDisposable()
        {
            // Arrange
            var theme = new LightTheme();

            // Act & Assert
            Assert.That(theme, Is.InstanceOf<IDisposable>());

            // Safely dispose
            theme.Dispose();
        }

        [Test]
        public void RefreshControl_AppliesThemeToSpecificControl()
        {
            // Arrange
            var panel = new Panel();
            panel.Tag = "HeadlinePanel";

            // Act
            ThemeManager.RefreshControl(panel);

            // Assert
            Assert.That(panel.BackColor, Is.EqualTo(ThemeManager.CurrentTheme.HeadlineBackground));
        }

        // Helper test theme class
        private class TestTheme : Theme
        {
            public override string Name => "TestTheme";
            public override System.Drawing.Color MainBackground => System.Drawing.Color.Purple;
            public override System.Drawing.Color SidePanelBackground => System.Drawing.Color.Violet;
            public override System.Drawing.Color HeadlineBackground => System.Drawing.Color.Indigo;
            public override System.Drawing.Color CardBackground => System.Drawing.Color.Lavender;
            public override System.Drawing.Color GridBackground => System.Drawing.Color.Plum;
            public override System.Drawing.Color ButtonBackground => System.Drawing.Color.MediumPurple;
            public override System.Drawing.Color ButtonHoverBackground => System.Drawing.Color.DarkViolet;
            public override System.Drawing.Color CardText => System.Drawing.Color.Black;
            public override System.Drawing.Color HeadlineText => System.Drawing.Color.White;
            public override System.Drawing.Color TextBoxBackground => System.Drawing.Color.LavenderBlush;
        }
    }
}
