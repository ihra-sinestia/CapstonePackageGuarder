using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using CapstonePackageGuarder;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Initializing Capstone Package Guarder (CPG)...");

        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        string? geminiApiKey = config["GeminiApiKey"];
        string? qdrantUrl = config["QdrantUrl"];
        string? qdrantApiKey = config["QdrantApiKey"];

        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Console.WriteLine("ERROR: Please configure GeminiApiKey in User Secrets or appsettings.json.");
            Console.WriteLine("Example: dotnet user-secrets set \"GeminiApiKey\" \"your_key\"");
            return;
        }

        try
        {
            var textEmbeddingService = new GeminiEmbeddingService(geminiApiKey);
            
            // For simplicity and immediate running (5-hour limit), we use VolatileMemoryStore.
            // To enable Qdrant Cloud: install Qdrant.Client and use QdrantVectorStore.
            IMemoryStore store = new VolatileMemoryStore();
            
            var memory = new MemoryBuilder()
                .WithTextEmbeddingGeneration(textEmbeddingService)
                .WithMemoryStore(store)
                .Build();

            var ingestor = new DataIngestor(memory);
            await ingestor.IngestDataAsync("Data/vulnerabilities_kb.md");

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", geminiApiKey);
            
            kernelBuilder.Plugins.AddFromObject(new ProjectAnalyzerPlugin(memory), "AnalyzerPlugin");
            
            var kernel = kernelBuilder.Build();

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var executionSettings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chatHistory = new ChatHistory("You are CPG (Capstone Package Guarder), an expert C# Agent for security. " +
                "You inspect project packages using AnalyzerPlugin. If the user gives you a directory, use your find_projects tool to find all .csproj files first. " +
                "Then you MUST use get_project_packages to read them. The tool will AUTOMATICALLY check the RAG memory for you and output [SECURITY WARNING] if vulnerable! " +
                "If get_project_packages outputs a security warning for a package, tell the user the vulnerability and ask if you should update it. If they say 'yes', use your UpdateProjectPackage tool. Don't auto-update without asking! " +
                "IMPORTANT: Respond in the exact same language the user uses to talk to you.");

            Console.WriteLine("\n[CPG Agent is Ready!]");
            Console.WriteLine("You can test it by saying 'Find and check all projects in ../ '");

            while (true)
            {
                try
                {
                    Console.Write("\nYou: ");
                    string? input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input)) break;

                    chatHistory.AddUserMessage(input);

                    bool success = false;
                    int retries = 0;
                    
                    while (!success && retries < 3)
                    {
                        try
                        {
                            var response = await chatCompletionService.GetChatMessageContentAsync(
                                chatHistory,
                                executionSettings,
                                kernel);

                            Console.WriteLine($"\nCPG: {response.Content}");
                            chatHistory.AddMessage(response.Role, response.Content ?? "");
                            success = true;
                        }
                        catch (Exception innerEx) when (innerEx.Message.Contains("429") || innerEx.Message.Contains("Too Many Requests"))
                        {
                            retries++;
                            Console.WriteLine($"\n[⏳ Rate Limit Hit (429 API Burst). Waiting 20 seconds to reset quota... (Attempt {retries}/3)]");
                            await Task.Delay(20000); // 20 seconds
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Error]: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical Error: {ex.Message}");
        }
    }
}
