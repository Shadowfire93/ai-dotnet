using Common.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using System.Threading.Tasks;

namespace MCPAPI.Services
{
    public class ChatService : IChatService
    {
        public async IAsyncEnumerable<StreamingChatMessageContent> ProcessPrompt(IList<Query> queries)
        {
            var endpoint = new Uri("http://localhost:11434/v1");
            var modelId = "qwen3";

            // Connect to both Weather and DuckDuckGo MCP servers
            var weatherClient = await McpClientFactory.CreateAsync(new SseClientTransport(new()
            {
                Name = "Weather",
                Endpoint = new System.Uri("https://localhost:44392/sse")
            }));

            // Retrieve tools from both servers
            var weatherTools = await weatherClient.ListToolsAsync().ConfigureAwait(false);

            var builder = Kernel.CreateBuilder();
            builder.Services
                .AddLogging(c => c.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace))
                .AddOpenAIChatCompletion(modelId: modelId, apiKey: null, endpoint: endpoint);

            Kernel kernel = builder.Build();
            //kernel.Plugins.AddFromFunctions("Weather", weatherTools.Where(t => t.Name == "Weather").Select(t => t.AsKernelFunction()));

            // Enable automatic function calling
            OpenAIPromptExecutionSettings executionSettings = new()
            {
                Temperature = 1,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };

            var ai =  kernel.GetRequiredService<IChatCompletionService>();

            string grounding = "You are an AI assistant that helps people find out about the weather in Adelaide South Australia";
            ChatHistory chat = new(grounding);

            for (int i = 0; i < queries.Count; i++)
            {
                if (queries[i].Origin == "user")
                {
                    chat.AddUserMessage(queries[i].Message);
                }
                else
                {
                    chat.AddAssistantMessage(queries[i].Message);
                }
            }
            //(StreamingChatMessageContent)
            await foreach (var message in ai.GetStreamingChatMessageContentsAsync(chat))
            {
                yield return message;
            }

            //yield return ai.GetStreamingChatMessageContentsAsync(chat);
        }
    }
}
