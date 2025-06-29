using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Models;
using RAGDemo.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using StackExchange.Redis;
using Microsoft.SemanticKernel.ChatCompletion;

namespace RAGDemo.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _embeddingModel = "bge-large";
        private readonly RedisVectorStore _vectorStore;

        public ChatService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://ollama:11434/") };
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var db = redis.GetDatabase();
            _vectorStore = new RedisVectorStore(db, "policy-documents");
        }

        private async Task<float[]> GetEmbeddingAsync(string text, string model)
        {
            // Call Ollama API for embedding
            var requestBody = new { model = model, prompt = text }; // Adjust as needed for Ollama API
            var response = await _httpClient.PostAsync("/api/embeddings", new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var embedding = doc.RootElement.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
            return embedding;
        }

        public async Task<List<PolicyDocumentEmbedding>> GetRelevantParagraphsAsync(string prompt, string embeddingsModel, int topK = 3)
        {
            var embedding = await GetEmbeddingAsync(prompt, embeddingsModel);
            var paragraphs = await _vectorStore.SearchAsync(embedding, topK);
            return paragraphs;
        }

        public async Task<string> GetChatResponseAsync(string prompt, List<string> history, string model, string groundingPrompt, string embeddingsModel)
        {
            var relevantParagraphs = await GetRelevantParagraphsAsync(prompt, embeddingsModel);
            if (relevantParagraphs == null || relevantParagraphs.Count == 0)
            {
                return "Sorry, I have no information relevant to your request.";
            }
            var context = string.Join("\n", relevantParagraphs.Select(p => $"[{p.Title}] {p.Content}"));

            // Build chat history using Semantic Kernel's ChatHistory
            var chatHistory = new ChatHistory();
            if (!string.IsNullOrWhiteSpace(groundingPrompt))
            {
                chatHistory.AddSystemMessage(groundingPrompt);
            }
            if (!string.IsNullOrWhiteSpace(context))
            {
                chatHistory.AddSystemMessage($"Context:\n{context}");
            }
            // Expect history as alternating user/llm turns, starting with user
            bool isUser = true;
            foreach (var turn in history)
            {
                if (isUser)
                {
                    chatHistory.AddUserMessage(turn);
                }
                else
                {
                    chatHistory.AddAssistantMessage(turn);
                }
                isUser = !isUser;
            }
            chatHistory.AddUserMessage(prompt);

            // Compose the full prompt for Ollama
            var fullPrompt = string.Join("\n", chatHistory.Select(m =>
                m.Role == AuthorRole.System ? $"System: {m.Content}" :
                m.Role == AuthorRole.User ? $"User: {m.Content}" :
                $"AI: {m.Content}"));

            // Call Ollama for chat completion
            var requestBody = new { model = model, prompt = fullPrompt, stream = false };
            var response = await _httpClient.PostAsync("/api/generate", new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.TryGetProperty("response", out var respProp) ? respProp.GetString() : json;
            return result ?? "[No response from Ollama]";
        }
    }
}
