using IP.Chatbot.Models;
using IP.Chatbot.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

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
        private readonly IChatCompletionService _chatCompletionService;

        public ChatBotController(ILogger<ChatBotController> logger,
            Kernel kernel,
            ChatHistory chatHistory,
            MemoryWebClient memoryWebClient,
            IChatCompletionService chatCompletionService)
        {
            _logger = logger;
            _kernel = kernel;
            _chatHistory = chatHistory;
            _memoryWebClient = memoryWebClient;
            _chatCompletionService = chatCompletionService;
        }



        [HttpGet(Name = "GetAnswer")]
        public async Task<ChatMessage> GetAnswer(string question,string authorName)
        {
            var result = new ChatMessage();
            //return await GetChatFromKernelMemory(question, authorName, result);
            return await GetChatFromKernelMemoryWithSimanticKernalChat(question, authorName, result);
        }

        private async Task<ChatMessage> GetChatFromKernelMemoryWithSimanticKernalChat(string question, string authorName, ChatMessage result)
        {
            var reply = new StringBuilder();

            _chatHistory.AddUserMessage(question);

            // Use KM to generate an answer. Fewer tokens, but one extra LLM request.
            MemoryAnswer memoryAnswer = await _memoryWebClient.AskAsync(question);
            var answer = memoryAnswer.Result;

            // Inject the memory recall in the initial system message
            _chatHistory[0].Content = $"{CommonString.SystemPrompt}\n\nLong term memory:\n{answer}";

            // Generate the next chat message, stream the response
            await foreach (StreamingChatMessageContent stream in _chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory))
            {
                reply.Append(stream.Content);
            }

            _chatHistory.AddAssistantMessage(reply.ToString());
            result.UserType = "ChatBot";
            result.Content = reply.ToString();

            return result;
        }

        [HttpGet(Name ="GetChatHistory")]
        public async Task<ChatHistory> GetAllChatHistory()
        {
            return _chatHistory;
        }

        //[Obsolete]
        //private async Task<ChatMessage> GetChatFromKernelMemory(string question, string authorName, ChatMessage result)
        //{
        //    var prompt = $@"
        //    {{memory.ask}} {question} 
        //    If Kernel Memory doesn't know the answer, say 'I don't know'.

        //     Question to Kernel Memory: {question}
        //    ";


        //    //_chatHistory.Add(new ChatMessageContent
        //    //{
        //    //    Role = AuthorRole.User,
        //    //    AuthorName = authorName,
        //    //    Items = [
        //    //        new FunctionCallContent(
        //    //        functionName: "ask",
        //    //        pluginName: "memory",
        //    //        id: "001",
        //    //        arguments: new() { { "question", question } }
        //    //        )
        //    //        ]
        //    //});

        //    var kernelMemoryResult = await _memoryWebClient.AskAsync(prompt);

        //    //Console.WriteLine($"\n AI Assistant: {result.Result}\n\n");

        //    //_chatHistory.Add(new ChatMessageContent
        //    //{
        //    //    Role = AuthorRole.Assistant,
        //    //    AuthorName = authorName,
        //    //    Items = [
        //    //        new FunctionResultContent(
        //    //        functionName: "ask",
        //    //        pluginName: "memory",
        //    //        callId: "001",
        //    //        result: kernelMemoryResult
        //    //       )
        //    //    ]
        //    //});

        //    result.UserType = "ChatBot";
        //    result.Content = kernelMemoryResult.Result;

        //    foreach (var source in kernelMemoryResult.RelevantSources)
        //    {
        //        result.RelavantSources.Add(new()
        //        {
        //            DocumentId = source.DocumentId,
        //            SourceName = source.SourceName,
        //            LastUpdatedDate = source.Partitions.First().LastUpdate.ToString()
        //        });
        //    }

        //    return result;
        //}
    }
}
