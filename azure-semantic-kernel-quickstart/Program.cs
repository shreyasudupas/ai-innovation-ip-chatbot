using Azure.AI.OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;


var config = new ConfigurationBuilder()
    //AddJsonFile("applications.json")
    .AddUserSecrets<Program>()
    .Build();

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: config["AZURE_OPENAI_DEPLOYMENT_NAME"],
        endpoint: config["AZURE_OPENAI_ENDPOINT"],
        apiKey: config["AZURE_OPENAI_API_KEY"])
    .Build();

Console.WriteLine("This is AI Chatbot. Please Ask your Question related to Datafabric App");
var ask = Console.ReadLine();

var function = kernel.CreateFunctionFromPrompt("Question: {{$input}}");

// Chat Completion example
var dataSource = GetAzureSearchDataSource(config);

var promptExecutionSettings = new AzureOpenAIPromptExecutionSettings 
{ 
    AzureChatDataSource = dataSource,
    Temperature = 0,
};

var response = await kernel.InvokeAsync(function, new(promptExecutionSettings) { ["input"] = ask });

Console.WriteLine($"Response: {response.GetValue<string>()}");
Console.WriteLine();


static AzureSearchChatDataSource GetAzureSearchDataSource(IConfigurationRoot config)
{
    return new AzureSearchChatDataSource
    {
        Endpoint = new Uri(config["AZURE_OPEN_API_SEARCH_ENDPOINT"]),
        Authentication = DataSourceAuthentication.FromApiKey(config["AZURE_ADMIN_SEARCH_KEY"]),
        IndexName = config["AZURE_SEARCH_INDEX"]
    };
}
