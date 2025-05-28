using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BusBus.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BusBus.Tests.UI
{
    [TestClass]
    [TestCategory("UI")]
    public class DashboardLoadTest
    {
        private TestServiceProvider _serviceProvider;
        private Mock<ILogger<DashboardView>> _loggerMock;
        private List<string> _logMessages;
        private Stopwatch _performanceTimer;

        [TestInitialize]
        public void Setup()
        {
            _logMessages = new List<string>();
            _performanceTimer = new Stopwatch();
            _loggerMock = new Mock<ILogger<DashboardView>>();
            
            // Set up logging to capture messages
            _loggerMock.Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback<LogLevel, EventId, object, Exception, Func<object, Exception, string>>(
                    (level, id, state, ex, formatter) =>
                    {
                        _logMessages.Add($"{level}: {formatter(state, ex)}");
                    });
            
            // Create service provider for testing
            _serviceProvider = new TestServiceProvider();
            _serviceProvider.AddService(typeof(ILogger<DashboardView>), _loggerMock.Object);
            _serviceProvider.AddService(typeof(ILoggerFactory), new TestLoggerFactory(_loggerMock.Object));
        }

        [TestMethod]
        public async Task DashboardLoad_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            const int timeoutMs = 3000; // Dashboard should initialize within 3 seconds
            _performanceTimer.Start();

            // Act
            var dashboard = new DashboardView(_serviceProvider);
            var cts = new CancellationTokenSource(timeoutMs);
            
            try
            {
                // Simulate activation of the dashboard
                await dashboard.ActivateForTest(cts.Token);
                
                // Assert
                var elapsed = _performanceTimer.ElapsedMilliseconds;
                Console.WriteLine($"Dashboard loaded in {elapsed} ms");
                Assert.IsTrue(elapsed < timeoutMs, $"Dashboard loaded too slowly: {elapsed}ms");
                
                // Log messages for debugging
                Console.WriteLine($"Captured {_logMessages.Count} log messages:");
                foreach (var message in _logMessages)
                {
                    Console.WriteLine($"LOG: {message}");
                }
            }
            catch (OperationCanceledException)
            {
                Assert.Fail($"Dashboard load timed out after {timeoutMs}ms");
            }
            finally
            {
                _performanceTimer.Stop();
            }
        }

        [TestMethod]
        public void DashboardLoad_ShouldTrackComponentInitialization()
        {
            // Arrange & Act
            var dashboard = new DashboardView(_serviceProvider);
            
            // Assert
            var initializationLogs = _logMessages.FindAll(msg => msg.Contains("initialization"));
            Assert.IsTrue(initializationLogs.Count > 0, "No initialization tracking logs found");
            
            Console.WriteLine("Component initialization logs:");
            foreach (var log in initializationLogs)
            {
                Console.WriteLine($"INIT: {log}");
            }
        }
    }

    // Helper testing classes
    public static class DashboardViewExtensions
    {
        public static Task ActivateForTest(this DashboardView view, CancellationToken cancellationToken)
        {
            // Use reflection to invoke the protected OnActivateAsync method
            var method = typeof(DashboardView).GetMethod("OnActivateAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (Task)method.Invoke(view, new object[] { cancellationToken });
        }
    }

    public class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void AddService(Type type, object implementation)
        {
            _services[type] = implementation;
        }

        public object GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out var service);
            return service;
        }
    }

    public class TestLoggerFactory : ILoggerFactory
    {
        private readonly ILogger _logger;

        public TestLoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose() { }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // Do nothing
        }
    }
}
