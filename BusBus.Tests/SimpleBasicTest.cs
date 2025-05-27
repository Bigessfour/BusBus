using NUnit.Framework;

namespace BusBus.Tests
{
    [TestFixture]
    public class SimpleBasicTest
    {
        [Test]
        public void BasicMathTest()
        {
            // Most basic test possible
            var result = 2 + 2;
            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void StringTest()
        {
            var text = "Hello World";
            Assert.That(text, Is.Not.Empty);
            Assert.That(text, Is.EqualTo("Hello World"));
        }
    }
}
