using BusBus.UI;
using System.Drawing;
using Xunit;

namespace BusBus.Tests.UI
{
    public class ThemeTests
    {
        [Fact]
        public void Theme_LightTheme_AssignsExpectedColors()
        {
            var theme = new Theme("Light");
            Assert.Equal(Color.FromArgb(240, 240, 240), theme.MainBackground);
            Assert.Equal(Color.Black, theme.HeadlineText);
        }

        [Fact]
        public void Theme_DarkTheme_AssignsExpectedColors()
        {
            var theme = new Theme("Dark");
            Assert.Equal(Color.FromArgb(30, 30, 30), theme.MainBackground);
            Assert.Equal(Color.White, theme.HeadlineText);
        }
    }
}
