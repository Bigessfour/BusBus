using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests;

[TestClass]
public class Tests
{
    [TestInitialize]
    public void Setup()
    {
    }

    [TestMethod]
    public void Test1()
    {
        // Simple NUnit test
        var actual = 1 + 1;
        const int expected = 2;
        Assert.AreEqual(expected, actual, "Basic math should work");
        Console.WriteLine("Test executed successfully");
    }
}
