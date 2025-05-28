#pragma warning disable CS8618 // Non-nullable field/property must contain a non-null value when exiting constructor
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602 // Dereference of a possibly null reference
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusBus.AI
{
    public class GrokService
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;

        public GrokService()
        {
            httpClient = new HttpClient();
            apiKey = Environment.GetEnvironmentVariable("GROK_API_KEY");
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> AnalyzeMaintenancePatternAsync(string maintenanceData)
        {
            var prompt = $@"Analyze this bus maintenance data and provide insights:
{maintenanceData}

Please provide:
1. Maintenance pattern analysis
2. Predictive maintenance recommendations
3. Cost optimization suggestions
4. Risk assessment";

            return await CallGrokAsync(prompt);
        }

        public async Task<string> OptimizeRouteAsync(string routeData, string ridership)
        {
            var prompt = $@"Optimize this bus route based on ridership data:
Route: {routeData}
Ridership: {ridership}

Provide:
1. Route efficiency analysis
2. Schedule optimization recommendations
3. Stop placement suggestions
4. Expected improvements";

            return await CallGrokAsync(prompt);
        }

        public async Task<string> GenerateDriverInsightsAsync(string driverData)
        {
            var prompt = $@"Analyze driver performance data:
{driverData}

Provide:
1. Performance trends
2. Training recommendations
3. Safety insights
4. Recognition opportunities";

            return await CallGrokAsync(prompt);
        }

        private async Task<string> CallGrokAsync(string prompt)
        {
            var request = new
            {
                model = "grok-3",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.x.ai/v1/chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GrokResponse>(responseJson);
            return result.choices[0].message.content;
        }
    }

    public class GrokResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string content { get; set; }
    }
}
