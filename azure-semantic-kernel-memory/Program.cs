using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

var config = new ConfigurationBuilder()
    //.AddJsonFile("applications.json")
    .AddUserSecrets<Program>()
    .Build();


var kernelBuilder = BuildMemoryConfig(config);

var memory = kernelBuilder.Build<MemoryServerless>();

var filepath = $"C:\\Users\\shreyas.udupa\\source\\repos\\azure-semantic-kernel-quickstart\\azure-semantic-kernel-memory\\datafabric-agent-guide.docx";
await memory.ImportDocumentAsync(filepath, documentId: "doc-001");

Console.WriteLine("Waiting for memory injestion to complete..");
while (!await memory.IsDocumentReadyAsync("doc-001"))
{
    await Task.Delay(TimeSpan.FromMilliseconds(1500));
}

Console.WriteLine("Ask any question related to this document");
var question = Console.ReadLine();

var answer = await memory.AskAsync(question);

Console.WriteLine($"Question: {question}\n\n Answer: {answer.Result}\n\n ");

foreach (var source in answer.RelevantSources)
{
    Console.WriteLine($"{source.SourceName} - {source.Link} [{source.Partitions.First().LastUpdate:D}]");
}


static IKernelMemoryBuilder BuildMemoryConfig(IConfigurationRoot config)
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