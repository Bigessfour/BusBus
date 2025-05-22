using BusBus.UI;
using Xunit;

namespace BusBus.Tests.UI
{
    public class UiConstantsTests
    {
        [Fact]
        public void FormWidth_ReturnsDefault()
        {
            Assert.True(UiConstants.FormWidth > 0);
        }
        [Fact]
        public void CardPanelHeight_ReturnsDefault()
        {
            Assert.True(UiConstants.CardPanelHeight > 0);
        }
    }
}
