using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using QnABot.Models;

namespace QnABot.Hubs
{
    public class ChatHub : Hub
    {
        private readonly HttpClient _httpClient;
        private readonly AzureQnASettings _settings;

        public ChatHub(IOptions<AzureQnASettings> settings)
        {
            _settings = settings.Value;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
        }

        public async Task SendMessage(string user, string message)
        {
            var response = await GetAnswerFromQnA(message);
            await Clients.All.SendAsync("ReceiveMessage", user, message, response);
        }

        private async Task<string> GetAnswerFromQnA(string question)
        {
            var requestBody = new
            {
                top = 3,
                question = question,
                includeUnstructuredSources = true,
                confidenceScoreThreshold = 0.5,
                answerSpanRequest = new
                {
                    enable = true,
                    topAnswersWithSpan = 1,
                    confidenceScoreThreshold = 0.5
                }
            };

            var response = await _httpClient.PostAsJsonAsync(_settings.Endpoint, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var a = response.Content.ReadAsStringAsync().Result;
                await Console.Out.WriteLineAsync(a);
                var result = await response.Content.ReadFromJsonAsync<QnAResponse>();
                return result?.Answers?.FirstOrDefault()?.Answer ?? "Sorry, I couldn't find an answer to your question.";
            }

            return "Sorry, there was an error processing your question.";
        }
    }

    public class QnAResponse
    {
        public List<QnAAnswer> Answers { get; set; } = new();
    }

    public class QnAAnswer
    {
        public string Answer { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
    }
} 