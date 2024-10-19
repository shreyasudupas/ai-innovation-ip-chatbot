using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

IHostEnvironment env = builder.Environment;

// Add services to the container.
var config = builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Kernel Memeory Registraton
var memory = new MemoryWebClient("http://127.0.0.1:9001");
builder.Services.AddTransient((serviceProvider) => memory);

await AddFileToMemoryForInjestion(memory);

//kernel registration
builder.Services.AddSingleton<ChatHistory>();

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
        .AddFile("Data\\retry-mechanism-for-events-datafabric.docx");

    await memory.ImportDocumentAsync(documentsToAdd);

    Console.WriteLine("Waiting for memory injestion .....");
    while (!await memory.IsDocumentReadyAsync("datafabric-docs"))
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
    }
    Console.WriteLine("memory injestion is completed");
}
