using IP.Chatbot.Models;

namespace IP.ChatBot.Blazor.Services;

public interface IChatBotService
{
    Task<ChatMessage> GetMessageFromAIModel(string user, string question);
}
