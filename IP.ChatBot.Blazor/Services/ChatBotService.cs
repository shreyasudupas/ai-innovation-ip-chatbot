using IP.Chatbot.Models;

namespace IP.ChatBot.Blazor.Services;

public class ChatBotService : IChatBotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public ChatBotService(IHttpClientFactory httpClientFactory,
        ILogger<ChatBotService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ChatMessage> GetMessageFromAIModel(string user, string question)
    {
        var httpClient = _httpClientFactory.CreateClient("ChatbotService");

        try
        {
            var result = await httpClient.GetFromJsonAsync<ChatMessage>($"api/GetAnswer?question={question}&authorName={user}");
            return result;

        }
        catch(Exception ex)
        {
            _logger.LogError("Error occured in GetMessageFromAIModel {0}", ex);
            return null;
        }
    }
}
