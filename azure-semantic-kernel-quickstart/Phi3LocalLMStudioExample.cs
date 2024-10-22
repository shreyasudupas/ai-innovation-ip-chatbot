
using Microsoft.SemanticKernel;

namespace azure_semantic_kernel_quickstart
{
    public class PhiLocalLMExample
    {
        public async Task Run()
        {
            var endpoint = "http://localhost:1234/v1/";
            var endpointUri = new Uri(endpoint);
            var modelId = "phi3";


            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    endpoint: endpointUri,
                    apiKey: "lm-studio",
                    modelId: modelId)
                .AddLocalTextEmbeddingGeneration()
                .Build();

            const string prompt = @"
            Exercise bot can have conversation about Fitness related topic.
            It gives instruction how to perform the exercise or provide a general instruction

            User: {{$userInput}}
            ExerciseBot:
            ";

            var chatFunction = kernel.CreateFunctionFromPrompt(prompt);

            Console.Write("Enter your Fitness related Question: ");
            var userInput = Console.ReadLine();

            var history = "";
            var arguments = new KernelArguments
            {
                ["history"] = history
            };

            arguments["userInput"] = userInput;

            var answer = await chatFunction.InvokeAsync(kernel,arguments);

            Console.WriteLine($"Response: {answer.GetValue<string>()}");
            Console.WriteLine();
        }
    }
}
