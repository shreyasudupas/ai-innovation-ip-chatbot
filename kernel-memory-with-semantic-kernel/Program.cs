using kernel_memory_with_semantic_kernel.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Reflection;

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

await ChatUsingPrompt(memory, kernel);

//await ChatUsingFunctionInvoke(memory, kernel);

static IKernelBuilder BuildKernel(IConfigurationRoot configuration)
{
    var builder = Kernel.CreateBuilder();

    builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

    //builder.AddAzureOpenAIChatCompletion(
    //    deploymentName: configuration["AZURE_OPENAI_DEPLOYMENT_NAME"],
    //    endpoint: configuration["AZURE_OPENAI_ENDPOINT"],
    //    apiKey: configuration["AZURE_OPENAI_API_KEY"]
    //    );
    ;
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
    //    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    //};

    var chatHistory = new ChatHistory();
    //var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

    while (true)
    {
        Console.Write("\nAsk the Question related to the file uploaded. To exit type bye!,\n\n User> ");
        var message = Console.ReadLine();

        if(string.Equals(message,"bye",StringComparison.InvariantCultureIgnoreCase))
        {
            break;
        }

        var prompt = $@"
            {{memory.ask}} {message} 
            If Kernel Memory doesn't know the answer, say 'I don't know'.

             Question to Kernel Memory: {message}
            ";


        chatHistory.Add(new ChatMessageContent
        {
            Role = AuthorRole.User,
            Items = [
                new FunctionCallContent(
                    functionName: "ask",
                    pluginName: "memory",
                    id: "001",
                    arguments: new() { { "question", message } }
                    )
                ]
        });
        //var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);
        var result = await memory.AskAsync(prompt);

        Console.WriteLine($"\n AI Assistant: {result.Result}\n\n");
        
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