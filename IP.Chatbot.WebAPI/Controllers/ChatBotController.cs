using IP.Chatbot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace IP.Chatbot.WebAPI.Controllers
{
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    [ApiController]
    [Route("[controller]/api/[action]")]
    public class ChatBotController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly ChatHistory _chatHistory;
        private readonly ILogger<ChatBotController> _logger;
        private readonly MemoryWebClient _memoryWebClient;

        public ChatBotController(ILogger<ChatBotController> logger,
            Kernel kernel,
            ChatHistory chatHistory,
            MemoryWebClient memoryWebClient)
        {
            _logger = logger;
            _kernel = kernel;
            _chatHistory = chatHistory;
            _memoryWebClient = memoryWebClient;
        }



        [HttpGet(Name = "GetAnswer")]
        public async Task<ChatMessage> GetAnswer(string question,string authorName)
        {
            var result = new ChatMessage();

            var prompt = $@"
            {{memory.ask}} {question} 
            If Kernel Memory doesn't know the answer, say 'I don't know'.

             Question to Kernel Memory: {question}
            ";


            _chatHistory.Add(new ChatMessageContent
            {
                Role = AuthorRole.User,
                AuthorName = authorName,
                Items = [
                    new FunctionCallContent(
                    functionName: "ask",
                    pluginName: "memory",
                    id: "001",
                    arguments: new() { { "question", question } }
                    )
                    ]
            });

            var kernelMemoryResult = await _memoryWebClient.AskAsync(prompt);

            //Console.WriteLine($"\n AI Assistant: {result.Result}\n\n");

            _chatHistory.Add(new ChatMessageContent
            {
                Role = AuthorRole.Assistant,
                AuthorName = authorName,
                Items = [
                    new FunctionResultContent(
                    functionName: "ask",
                    pluginName: "memory",
                    callId: "001",
                    result: kernelMemoryResult
                   )
                ]
            });

            result.UserType = "ChatBot";
            result.Content = kernelMemoryResult.Result;
            
            foreach(var source in kernelMemoryResult.RelevantSources)
            {
                result.RelavantSources.Add(new()
                {
                    DocumentId = source.DocumentId,
                    SourceName = source.SourceName,
                    LastUpdatedDate = source.Partitions.First().LastUpdate.ToString()
                });
            }

            return result;
        }
    }
}
