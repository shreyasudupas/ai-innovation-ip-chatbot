using kernel_memory_with_semantic_kernel.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Reflection;
using System.Text;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var config = new ConfigurationBuilder()
    //.AddJsonFile("applications.json")
    .AddUserSecrets<Program>()
    .Build();

var kernelMemoryBuilder = BuildKernelMemoryConfig(config);
var kernelBuilder = BuildKernel(config);

var kernel = kernelBuilder.Build();
var memory = kernelMemoryBuilder.Build<MemoryServerless>();
//var memory = new MemoryWebClient("http://127.0.0.1:9001");

await AddFileToMemoryForInjestion(memory);

//await ChatUsingPrompt(memory, kernel);

//await ChatUsingFunctionInvoke(memory, kernel);

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
await ChatUsingSemanticKernel_KernelMemory_Chat(memory,chatCompletionService);

static IKernelBuilder BuildKernel(IConfigurationRoot configuration)
{
    var builder = Kernel.CreateBuilder();

    builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

    builder.AddAzureOpenAIChatCompletion(
        deploymentName: configuration["AZURE_OPENAI_DEPLOYMENT_NAME"],
        endpoint: configuration["AZURE_OPENAI_ENDPOINT"],
        apiKey: configuration["AZURE_OPENAI_API_KEY"]
        );
    //;
    return builder;
}

static IKernelMemoryBuilder BuildKernelMemoryConfig(IConfigurationRoot config)
{
    return new KernelMemoryBuilder()
    .WithAzureOpenAITextEmbeddingGeneration(new()
    {
        APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        Endpoint = config["AZURE_OPENAI_ENDPOINT"],
        APIKey = config["AZURE_OPENAI_API_KEY"],
        Deployment = "text-embedding-ada-002" // text-embedding-ada-002
    })
    .WithAzureOpenAITextGeneration(new()
    {
        APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        Endpoint = config["AZURE_OPENAI_ENDPOINT"],
        Deployment = config["AZURE_OPENAI_DEPLOYMENT_NAME"],
        APIKey = config["AZURE_OPENAI_API_KEY"]
    })
    //.WithAzureAISearchMemoryDb(new()
    //{
    //    Auth = AzureAISearchConfig.AuthTypes.APIKey,
    //    Endpoint = config["AZURE_OPEN_API_SEARCH_ENDPOINT"],
    //    APIKey = config["AZURE_ADMIN_SEARCH_KEY"]
    //})
    .WithSimpleVectorDb();
}

static async Task AddFileToMemoryForInjestion(IKernelMemory memory)
{
    //Add Files for injestion
    //var filepath = Path.Combine(Directory.GetCurrentDirectory(), "datafabric-agent-guide.docx");

    var documentsToAdd = new Document("datafabric-docs")
        .AddFile("datafabric-agent-guide.docx")
        .AddFile("retry-mechanism-for-events-datafabric.docx");

    //await memory.ImportDocumentAsync(filepath, documentId: "doc-001");
    await memory.ImportDocumentAsync(documentsToAdd);

    Console.WriteLine("Waiting for memory injestion to complete..");
    while (!await memory.IsDocumentReadyAsync("datafabric-docs"))
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
    }
}

static async Task ChatUsingPrompt(MemoryServerless memory,Kernel kernel)
{
    var plugin = new MemoryPlugin(memory, waitForIngestionToComplete: true);
    kernel.ImportPluginFromObject(plugin, "memory");


    //OpenAIPromptExecutionSettings settings = new()
    //{
    //    Temperature = 0
    //};

    var chatHistory = new ChatHistory();
    var history = new StringBuilder();
    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

    Console.WriteLine("\nAsk the Question related to the file uploaded. To exit type bye!,\n\n");
    while (true)
    {
        Console.Write(" User: ");
        var message = Console.ReadLine();

        if(string.Equals(message,"bye",StringComparison.InvariantCultureIgnoreCase))
        {
            break;
        }

        //var prompt = $@"
        //    {{memory.ask}} {message} 
        //    If Kernel Memory doesn't know the answer, say 'I don't know'.

        //     Question to Kernel Memory: {message}
        //    ";

        var prompt = $@"
        You are a chat assistant providing product information based on available documents. Your goal is to summarize answers as concisely as possible.
        Question to kernel memory: {message}
        AI response: {{memory.ask}}

        For follow-up questions, refer to the conversation history using the context below:
        Context for follow-up questions: {history.ToString()}

        If the question cannot be answered fully or if the user is dissatisfied with the response, reply with:
        Fallback for incomplete answers: ""I don't have that information. Please create a ticket in the ServiceNow portal.""

        For unrelated or general questions, respond with:
        Response for unrelated queries: ""I'm sorry, I'm not aware of this. Please ask something related to the product.""
        ";

        chatHistory.Add(new()
        {
            Role = AuthorRole.System,
            Content = "You are a helpful assistant"
        });
        
        chatHistory.Add(new ChatMessageContent
        {
            Role = AuthorRole.User,
            Items = [
                new FunctionCallContent(
                    functionName: "ask_user_history",
                    pluginName: "memory",
                    id: "001",
                    arguments: new() { { "question", message } }
                    )
                ]
        });
        //var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory,
        //    settings,
        //    kernel);
        var result = await memory.AskAsync(prompt);

        Console.WriteLine($"\n AI Bot: {result.Result}\n\n");
        history.Append($"Question to kernel memory: {message}\n AI Bot: {result.Result}\n");

        //Console.WriteLine($"AI Bot: {result}\n");

        chatHistory.Add(new ChatMessageContent
        {
            Role = AuthorRole.Assistant,
            Items = [
                new FunctionResultContent(
                    functionName: "ask",
                    pluginName: "memory",
                    callId: "001",
                    result: result
                   )
            ]
        });
    }

    var chathistoryCount = chatHistory.Count;

    Console.WriteLine("\n---------------Chat History are----------------\n\n");
    //Display the history
    for(var i = 0; i< chathistoryCount; i++)
    {
        PropertyInfo argumentPropertyInfo = chatHistory[i].Items[0].GetType().GetProperty("Arguments");
        PropertyInfo resultPropertyInfo = chatHistory[i].Items[0].GetType().GetProperty("Result");

        if (argumentPropertyInfo != null)
        {
            var arg = (KernelArguments)argumentPropertyInfo.GetValue(chatHistory[i].Items[0]);

            Console.WriteLine($"Role: {chatHistory[i].Role} - Question: {arg["question"]}");
        } 
        else
        {
            var resultValue = resultPropertyInfo.GetValue(chatHistory[i].Items[0]);

            Console.WriteLine($"Role: {chatHistory[i].Role} - Answer: {resultValue}");
        }

        //Console.WriteLine($"Role: {chatHistory[i].Role} - Question: {chatHistory[i].Items[0]}");
    }
}

static async Task ChatUsingFunctionInvoke(MemoryServerless memory, Kernel kernel)
{
    var memoryPlugin = new ExtendedKernelMemory(memory, kernel);
    kernel.ImportPluginFromObject(memoryPlugin, nameof(ExtendedKernelMemory));

    while (true)
    {
        Console.WriteLine("\nAsk the Question related to the file uploaded. User> ");
        var message = Console.ReadLine();

        var result = await kernel.InvokeAsync(nameof(ExtendedKernelMemory), "ask", new KernelArguments
        {
            { "question", message }
        });

        Console.WriteLine($"Answer: {result.GetValue<string>()}");

        //get the memory info for kernel Data dictionary that is defined in the ExtendedKernel Memory class
        MemoryAnswer memoryAnswer = (MemoryAnswer)kernel.Data["AskMemoryKey"];

        foreach (var source in memoryAnswer?.RelevantSources)
        {
            Console.WriteLine($"File name: {source.SourceName} - {source.DocumentId} - {source.Partitions.First().LastUpdate:D}");
        }
    }
}

static async Task ChatUsingSemanticKernel_KernelMemory_Chat(IKernelMemory kernelMemory,IChatCompletionService chatService)
{
    //chat setup
    var systemPrompt = """
                           You are a helpful assistant replying to user questions using information from your memory.
                           Reply very briefly and concisely, get to the point immediately. Don't provide long explanations unless necessary.
                           Sometimes you don't have relevant memories so you reply saying you don't know, don't have the information.
                           The topic of the conversation is Data Fabric application.
                           """;

    var chatHistory = new ChatHistory(systemPrompt);

    // Start the chat
    var assistantMessage = "Hello, how can I help?";
    Console.WriteLine($"Copilot> {assistantMessage}\n");
    chatHistory.AddAssistantMessage(assistantMessage);

    // Infinite chat loop
    var reply = new StringBuilder();

    while(true)
    {
        // Get user message (retry if the user enters an empty string)
        Console.Write("You> ");
        var userMessage = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(userMessage)) { continue; }
        else { chatHistory.AddUserMessage(userMessage); }

        // Use KM to generate an answer. Fewer tokens, but one extra LLM request.
        MemoryAnswer memoryAnswer = await kernelMemory.AskAsync(userMessage);
        var answer = memoryAnswer.Result;

        // Inject the memory recall in the initial system message
        chatHistory[0].Content = $"{systemPrompt}\n\nLong term memory:\n{answer}";

        // Generate the next chat message, stream the response
        Console.Write("\nCopilot> ");
        reply.Clear();

        await foreach (StreamingChatMessageContent stream in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
        {
            Console.Write(stream.Content);
            reply.Append(stream.Content);
        }

        chatHistory.AddAssistantMessage(reply.ToString());
        Console.WriteLine("\n");
    }
}