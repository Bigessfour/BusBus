using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests
{
    [TestClass]
    public class SimpleBasicTest
    {
        [TestMethod]
        public void BasicMathTest()
        {
            // Most basic test possible
            var result = 2 + 2;
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void StringTest()
        {
            var text = "Hello World";
            Assert.IsTrue(!string.IsNullOrEmpty(text));
            Assert.AreEqual("Hello World", text);
        }
    }
}
