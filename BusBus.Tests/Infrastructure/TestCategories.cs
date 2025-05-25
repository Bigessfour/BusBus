using System;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Define test categories to organize tests by type and characteristics.
    /// These can be used with the [Category] attribute in NUnit tests.
    /// </summary>
    public static class TestCategories
    {
        /// <summary>
        /// Fast tests that don't require external resources like databases or network connections
        /// </summary>
        public const string Unit = "Unit";
        
        /// <summary>
        /// Tests that validate integration with real external systems (databases, APIs, etc.)
        /// </summary>
        public const string Integration = "Integration";
        
        /// <summary>
        /// Tests that validate end-to-end functionality and multiple components working together
        /// </summary>
        public const string EndToEnd = "EndToEnd";
        
        /// <summary>
        /// Performance or load tests that measure system behavior under load
        /// </summary>
        public const string Performance = "Performance";
        
        /// <summary>
        /// Tests that require SQL Server container (used to filter these tests out on CI if needed)
        /// </summary>
        public const string RequiresSqlServer = "RequiresSqlServer";
        
        /// <summary>
        /// Smoke tests that verify basic functionality works as expected
        /// </summary>
        public const string Smoke = "Smoke";
        
        /// <summary>
        /// Tests that are known to be long-running and may timeout on CI systems
        /// </summary>
        public const string LongRunning = "LongRunning";
        
        /// <summary>
        /// Tests that are flaky or intermittently failing and need to be fixed
        /// </summary>
        public const string Flaky = "Flaky";
    }
}
