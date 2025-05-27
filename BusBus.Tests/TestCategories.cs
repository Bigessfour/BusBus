namespace BusBus.Tests
{
    /// <summary>
    /// Test categories to organize tests by type and characteristics.
    /// Use these with the [Category] attribute in NUnit tests.
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
        /// Tests that require SQL Server Express (used to filter these tests out on CI if needed)
        /// </summary>
        public const string RequiresSqlServer = "RequiresSqlServer";

        /// <summary>
        /// Tests for UI components and user interface logic
        /// </summary>
        public const string UI = "UI";

        /// <summary>
        /// Tests specifically for database operations and Entity Framework
        /// </summary>
        public const string Database = "Database";

        /// <summary>
        /// Tests for service layer classes
        /// </summary>
        public const string Service = "Service";

        /// <summary>
        /// Tests for model classes and data transfer objects
        /// </summary>
        public const string Model = "Model";

        /// <summary>
        /// Smoke tests that verify basic functionality
        /// </summary>
        public const string Smoke = "Smoke";

        /// <summary>
        /// Long-running tests (typically excluded from regular test runs)
        /// </summary>
        public const string LongRunning = "LongRunning";
    }
}
