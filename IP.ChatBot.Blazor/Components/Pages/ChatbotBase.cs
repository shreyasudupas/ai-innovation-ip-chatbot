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
            chatMessages.Add(new()
            {
                UserType = "User",
                Content = UserMessage,
                CreatedDate = DateTime.Now.ToString("HH:mm tt")
            });

            if (!string.IsNullOrEmpty(UserMessage) && !(string.IsNullOrEmpty(LoginUser.Username)))
            {
                var answer = await _chatbotService.GetMessageFromAIModel("shreyas", UserMessage);

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
            UserMessage = string.Empty;
        }

    }
}
