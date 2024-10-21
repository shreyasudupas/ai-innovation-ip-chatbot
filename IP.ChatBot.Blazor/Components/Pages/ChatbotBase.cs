using IP.Chatbot.Models;
using IP.ChatBot.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IP.ChatBot.Blazor.Components.Pages
{
    public class ChatbotBase : ComponentBase
    {
        [Inject]
        public IChatBotService _chatbotService { get; set; }

        [Inject]
        public IJSRuntime JS {  get; set; }

        protected List<ChatMessage> chatMessages;

        protected string UserMessage { get; set; } = string.Empty;

        [Inject]
        public LoginUser LoginUser { get; set; }

        protected bool loadingChatMessage = false;

        protected override void OnInitialized()
        {
            chatMessages = new();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("scrollToBottom", "chat-container");
        }

        protected async Task SendUserMessage()
        {
            var question = UserMessage;
            UserMessage = string.Empty;
            loadingChatMessage = true;

            chatMessages.Add(new()
            {
                UserType = "User",
                Content = question,
                CreatedDate = DateTime.Now.ToString("HH:mm tt")
            });

            if (!string.IsNullOrEmpty(question) && !(string.IsNullOrEmpty(LoginUser.Username)))
            {
                var answer = await _chatbotService.GetMessageFromAIModel(LoginUser.Username, question);

                if (answer != null)
                {
                    chatMessages.Add(new()
                    {
                        UserType = answer.UserType,
                        Content = answer.Content,
                        RelavantSources = answer.RelavantSources,
                        CreatedDate = DateTime.Now.ToString("HH:mm tt")
                    });
                }
            }
            loadingChatMessage = false;
        }
    }
}
