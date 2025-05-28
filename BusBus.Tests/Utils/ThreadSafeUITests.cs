#nullable enable
#pragma warning disable CA1416 // Platform compatibility (WinForms)
#pragma warning disable NUnit1032 // Disposable fields in TearDown
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1861 // Prefer static readonly fields
#pragma warning disable CA1862 // Prefer string comparison overload
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1852 // Type can be sealed
#pragma warning disable CA1859 // Change type for performance
#pragma warning disable CA1825 // Avoid unnecessary zero-length array allocations
#pragma warning disable SYSLIB1045 // Regex compile-time warning

using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusBus.Utils;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using FluentAssertions;

namespace BusBus.Tests.Utils
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    // Platform attribute removed (MSTest incompatible)
    // Apartment attribute removed (MSTest incompatible) // Required for WinForms testing
    public class ThreadSafeUITests
    {
        private Form _testForm = null!;
        private TextBox _testTextBox = null!;
        private Button _testButton = null!;

        [TestInitialize]
        public void SetUp()
        {
            _testForm = new Form();
            _testTextBox = new TextBox();
            _testButton = new Button();

            _testForm.Controls.Add(_testTextBox);
            _testForm.Controls.Add(_testButton);
        }

        [TestCleanup]
        public void TearDown()
        {
            _testTextBox?.Dispose();
            _testButton?.Dispose();
            _testForm?.Dispose();
        }

        [TestMethod]
        // Description: Test ThreadSafeUI Invoke method on UI thread
        public void ThreadSafeUI_InvokeOnUIThread_ShouldExecuteDirectly()
        {
            // Arrange
            string testText = "Test Text Set Directly";

            // Act
            ThreadSafeUI.Invoke(_testTextBox, () =>
            {
                _testTextBox.Text = testText;
            });

            // Assert
            _testTextBox.Text.Should().Be(testText);
        }

        [TestMethod]
        // Description: Test ThreadSafeUI set property extension on UI thread
        public void ThreadSafeUI_SetPropertyOnUIThread_ShouldExecuteDirectly()
        {
            // Arrange
            string testText = "Test Property";

            // Act
            _testTextBox.SetPropertyThreadSafe("Text", testText);

            // Assert
            _testTextBox.Text.Should().Be(testText);
        }

        [TestMethod]
        // Description: Test ThreadSafeUI Invoke method from background thread
        public async Task ThreadSafeUI_InvokeFromBackgroundThread_ShouldUpdateUI()
        {
            // Arrange
            string testText = "Set from background thread";

            // Act - Run on a separate thread
            await Task.Run(() =>
            {
                ThreadSafeUI.Invoke(_testTextBox, () =>
                {
                    _testTextBox.Text = testText;
                });
            });

            // Assert
            _testTextBox.Text.Should().Be(testText);
        }

        [TestMethod]
        // Description: Test ThreadSafeUI UpdateUI method
        public void ThreadSafeUI_UpdateUI_ShouldSetControlProperties()
        {
            // Arrange
            string testText = "Updated UI";
            bool buttonEnabled = false;

            // Act
            ThreadSafeUI.UpdateUI(_testTextBox, () =>
            {
                _testTextBox.Text = testText;
            });
            ThreadSafeUI.UpdateUI(_testButton, () =>
            {
                _testButton.Enabled = buttonEnabled;
            });

            // Assert
            _testTextBox.Text.Should().Be(testText);
            _testButton.Enabled.Should().Be(buttonEnabled);
        }

        [TestMethod]
        // Description: Test ThreadSafeUI updating multiple controls
        public void ThreadSafeUI_UpdateMultipleControls_ShouldUpdateAll()
        {
            // Arrange
            var controls = new Control[] { _testTextBox, _testButton };
            string textBoxText = "Multiple update";
            string buttonText = "Updated Button";

            // Act
            ThreadSafeUI.UpdateControls(controls, control =>
            {
                if (control is TextBox tb) tb.Text = textBoxText;
                if (control is Button btn) btn.Text = buttonText;
            });

            // Assert
            _testTextBox.Text.Should().Be(textBoxText);
            _testButton.Text.Should().Be(buttonText);
        }
    }
}
