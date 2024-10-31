using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var isServerlessMemory = true;

var builder = WebApplication.CreateBuilder(args);

IHostEnvironment env = builder.Environment;

// Add services to the container.
var config = builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
    .AddUserSecrets<Program>()
    .Build();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IKernelMemory memory;
if(!isServerlessMemory)
{
    //Kernel Memory Registraton
    memory = new MemoryWebClient("http://127.0.0.1:9001");
    builder.Services.AddTransient((serviceProvider) => memory);
}
else
{
    var kernelBuilder = new KernelMemoryBuilder()
        .Configure(builder => builder.Services.AddLogging(l =>
        {
            l.SetMinimumLevel(LogLevel.Critical);
            l.AddConsole();
        }))
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
        });

    memory = kernelBuilder.Build();
    builder.Services.AddTransient(sp => memory);
}

await AddFileToMemoryForInjestion(memory);

//kernel registration
builder.Services.AddSingleton<ChatHistory>();

builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: config["AZURE_OPENAI_DEPLOYMENT_NAME"],
            endpoint: config["AZURE_OPENAI_ENDPOINT"],
            apiKey: config["AZURE_OPENAI_API_KEY"]);

// Finally, create the Kernel service with the service provider and plugin collection
builder.Services.AddTransient((serviceProvider) => {
    Kernel kernel = new(serviceProvider);
    kernel.ImportPluginFromObject(new MemoryPlugin(memory, waitForIngestionToComplete: true), "memory");

    return kernel;
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task AddFileToMemoryForInjestion(IKernelMemory memory)
{
    var documentsToAdd = new Document("datafabric-docs")
        .AddFile("Data\\datafabric-agent-guide.docx")
        .AddFile("Data\\retry-mechanism-for-events-datafabric.docx")
        .AddFile("Data\\Epicor Global Work Policy.pdf");

    await memory.ImportDocumentAsync(documentsToAdd);

    Console.WriteLine("Waiting for memory injestion .....");
    while (!await memory.IsDocumentReadyAsync("datafabric-docs"))
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
    }
    Console.WriteLine("memory injestion is completed");
}
