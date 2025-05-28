using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusBus.AI;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.AI
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    public class GrokServiceTests
    {
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private GrokService _grokService;

        [TestInitialize]
        public void SetUp()
        {
            // Setup mock HTTP handler
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Create a test instance of GrokService with the mocked HttpClient
            _grokService = CreateGrokServiceWithMockedHttpClient(_httpClient);
        }

        [TestCleanup]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        [TestMethod]
        // Description: Test maintenance pattern analysis with Grok API
        public async Task AnalyzeMaintenancePatternAsync_ShouldCallGrokWithCorrectPrompt()
        {
            // Arrange
            var maintenanceData = "Bus 101: Oil change due, 5000 miles since last service";
            var expectedResponse = "AI analysis of maintenance data...";

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _grokService.AnalyzeMaintenancePatternAsync(maintenanceData);

            // Assert
            result.Should().Be(expectedResponse);

            // Verify the correct API request was made with appropriate prompt
            VerifyHttpRequestMade(request => request.Contains("maintenance") && request.Contains(maintenanceData));
        }

        [TestMethod]
        // Description: Test route optimization with Grok API
        public async Task OptimizeRouteAsync_ShouldCallGrokWithCorrectPrompt()
        {
            // Arrange
            var routeData = "Route 42: Downtown to Suburb";
            var ridership = "Morning: 85%, Evening: 60%";
            var expectedResponse = "Route optimization recommendations...";

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _grokService.OptimizeRouteAsync(routeData, ridership);

            // Assert
            result.Should().Be(expectedResponse);

            // Verify the correct API request was made with appropriate prompt
            VerifyHttpRequestMade(request =>
                request.Contains("optimize") &&
                request.Contains(routeData) &&
                request.Contains(ridership));
        }

        [TestMethod]
        // Description: Test driver insights with Grok API
        public async Task GenerateDriverInsightsAsync_ShouldCallGrokWithCorrectPrompt()
        {
            // Arrange
            var driverData = "Driver John Smith: 5 years experience, 98% on-time";
            var expectedResponse = "Driver performance analysis...";

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _grokService.GenerateDriverInsightsAsync(driverData);

            // Assert
            result.Should().Be(expectedResponse);

            // Verify the correct API request was made with appropriate prompt
            VerifyHttpRequestMade(request =>
                request.Contains("driver") &&
                request.Contains(driverData));
        }

        [TestMethod]
        // Description: Test handling of API errors
        public async Task CallGrokAsync_WithApiError_ShouldHandleGracefully()
        {
            // Arrange
            var maintenanceData = "Error test data";

            // Setup error response
            var errorResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{'error': 'Invalid request'}", Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(errorResponse);

            // Act & Assert
            Func<Task> act = async () => await _grokService.AnalyzeMaintenancePatternAsync(maintenanceData);
            await act.Should().ThrowAsync<HttpRequestException>()
                .WithMessage("*response code does not indicate success*");
        }

        [TestMethod]
        // Description: Test Grok API timeout handling
        public async Task CallGrokAsync_WithTimeout_ShouldHandleGracefully()
        {
            // Arrange
            var driverData = "Timeout test data";

            // Setup timeout
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            // Act & Assert
            Func<Task> act = async () => await _grokService.GenerateDriverInsightsAsync(driverData);
            await act.Should().ThrowAsync<TaskCanceledException>()
                .WithMessage("*timed out*");
        }

        [TestMethod]
        // Description: Test handling of malformed JSON responses
        public async Task CallGrokAsync_WithMalformedResponse_ShouldThrowJsonException()
        {
            // Arrange
            var routeData = "Malformed JSON test";
            var ridership = "Morning: 75%, Evening: 50%";

            // Setup malformed JSON response
            var malformedResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{'invalid_json_structure': true,", Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(malformedResponse);

            // Act & Assert
            Func<Task> act = async () => await _grokService.OptimizeRouteAsync(routeData, ridership);
            await act.Should().ThrowAsync<JsonException>();
        }

        [TestMethod]
        // Description: Test handling of empty response
        public async Task CallGrokAsync_WithEmptyResponse_ShouldThrowException()
        {
            // Arrange
            var maintenanceData = "Empty response test";

            // Setup empty response
            var emptyResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(emptyResponse);

            // Act & Assert
            Func<Task> act = async () => await _grokService.AnalyzeMaintenancePatternAsync(maintenanceData);
            await act.Should().ThrowAsync<NullReferenceException>()
                .WithMessage("*Object reference not set to an instance of an object*");
        }

        [TestMethod]
        // Description: Test that Grok-3 model is correctly specified in API requests
        public async Task CallGrokAsync_ShouldSpecifyGrok3Model()
        {
            // Arrange
            var maintenanceData = "Test for model specification";
            var expectedResponse = "API response...";

            SetupMockHttpResponse(expectedResponse);

            // Act
            await _grokService.AnalyzeMaintenancePatternAsync(maintenanceData);

            // Assert - Verify that the correct model is specified
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        ContainsModelSpecification(req, "grok-3")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        // Description: Test that API configuration parameters are correctly set
        public async Task CallGrokAsync_ShouldSetCorrectApiParameters()
        {
            // Arrange
            var driverData = "Test for API parameters";
            var expectedResponse = "API response with parameters...";

            SetupMockHttpResponse(expectedResponse);

            // Act
            await _grokService.GenerateDriverInsightsAsync(driverData);

            // Assert - Verify API parameters (max_tokens and temperature)
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        HasCorrectApiParameters(req)),
                    ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        // Description: Test that the correct X.AI API endpoint is used
        public async Task CallGrokAsync_ShouldUseCorrectApiEndpoint()
        {
            // Arrange
            var routeData = "Route 101: Downtown to Airport";
            var ridership = "Peak: 90%, Off-peak: 45%";
            var expectedResponse = "Endpoint validation response...";
            var expectedEndpoint = "https://api.x.ai/v1/chat/completions";

            SetupMockHttpResponse(expectedResponse);

            // Act
            await _grokService.OptimizeRouteAsync(routeData, ridership);

            // Assert - Verify the correct API endpoint is used
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString() == expectedEndpoint),
                    ItExpr.IsAny<CancellationToken>());
        }

        private static bool ContainsModelSpecification(HttpRequestMessage request, string modelName)
        {
            if (request.Content == null)
                return false;

            var content = request.Content.ReadAsStringAsync().Result;
            return content.Contains($"\"model\":\"{modelName}\"") || content.Contains($"\"model\": \"{modelName}\"");
        }

        private static bool HasCorrectApiParameters(HttpRequestMessage request)
        {
            if (request.Content == null)
                return false;

            var content = request.Content.ReadAsStringAsync().Result;

            // Check for expected parameters (max_tokens and temperature)
            return content.Contains("\"max_tokens\":") &&
                   content.Contains("\"temperature\":");
        }

        #region Helper Methods

        private static GrokService CreateGrokServiceWithMockedHttpClient(HttpClient httpClient)
        {
            // Use reflection to create GrokService and replace its HttpClient
            var grokService = new GrokService();

            // Set the mocked HttpClient
            var httpClientField = typeof(GrokService).GetField("httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(grokService, httpClient);

            return grokService;
        }

        private void SetupMockHttpResponse(string responseContent)
        {
            // Create mock Grok API response
            var mockResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = responseContent
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(mockResponse);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);
        }

        private void VerifyHttpRequestMade(Func<string, bool> requestContentValidator)
        {
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString() == "https://api.x.ai/v1/chat/completions" &&
                        ContentMatchesValidator(req, requestContentValidator)),
                    ItExpr.IsAny<CancellationToken>());
        }

        private static bool ContentMatchesValidator(HttpRequestMessage request, Func<string, bool> validator)
        {
            if (request.Content == null)
                return false;

            var content = request.Content.ReadAsStringAsync().Result;
            return validator(content);
        }

        #endregion
    }
}
