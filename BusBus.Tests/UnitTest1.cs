using System;
using NUnit.Framework;

namespace BusBus.Tests;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        // Simple NUnit test
        var actual = 1 + 1;
        const int expected = 2;
        Assert.That(actual, Is.EqualTo(expected), "Basic math should work");
        Console.WriteLine("Test executed successfully");
    }
}
