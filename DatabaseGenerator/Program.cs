using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Redis;
using StackExchange.Redis;
using Common.Models;
using System.Net.Http.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.IO;

namespace DatabaseGenerator
{
    internal class Program
    {
        static async Task<float[]> GetEmbeddingAsync(string text)
        {
            using var http = new HttpClient();
            var response = await http.PostAsJsonAsync("http://localhost:11434/api/embeddings", new
            {
                model = "bge-large",
                prompt = text
            });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
            return result?.embedding ?? Array.Empty<float>();
        }

        static IEnumerable<string> ChunkText(string text, int maxTokens = 512)
        {
            // Simple chunking by sentence or word count (customize as needed)
            var words = text.Split(' ');
            for (int i = 0; i < words.Length; i += maxTokens)
                yield return string.Join(' ', words.Skip(i).Take(maxTokens));
        }

        static async Task AddPolicyDocumentAsync(string id, string title, string content, string department, dynamic collection)
        {
            int chunkIndex = 0;
            foreach (var chunk in ChunkText(content, 128))
            {
                chunkIndex = await AddChunkWithRetryAsync(id, title, chunk, department, collection, chunkIndex);
            }
        }

        static async Task<int> AddChunkWithRetryAsync(string id, string title, string chunk, string department, dynamic collection, int chunkIndex)
        {
            try
            {
                var embedding = await GetEmbeddingAsync(chunk);
                var record = new PolicyDocumentEmbedding
                {
                    Id = $"{id}_chunk{chunkIndex}",
                    Title = title,
                    Content = chunk,
                    Department = department,
                    Embedding = new ReadOnlyMemory<float>(embedding)
                };
                await collection.UpsertAsync(record, System.Threading.CancellationToken.None);
                return chunkIndex + 1;
            }
            catch (HttpRequestException)
            {
                if (chunk.Length < 50) // Avoid infinite recursion on very small chunks
                {
                    Console.WriteLine($"Failed to embed chunk: too small after splitting.");
                    return chunkIndex;
                }
                int mid = chunk.Length / 2;
                var left = chunk.Substring(0, mid);
                var right = chunk.Substring(mid);
                chunkIndex = await AddChunkWithRetryAsync(id, title, left, department, collection, chunkIndex);
                chunkIndex = await AddChunkWithRetryAsync(id, title, right, department, collection, chunkIndex);
                return chunkIndex;
            }
        }

        static string ExtractTextFromPdf(string filePath)
        {
            using var pdf = PdfDocument.Open(filePath);
            var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
            return text;
        }

        static async Task Main(string[] args)
        {
            // Connect to Redis
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var db = redis.GetDatabase();
            var vectorStore = new RedisVectorStore(db);
            var collection = vectorStore.GetCollection<string, PolicyDocumentEmbedding>("policy-documents");

            // Directory containing PDF documents
            string docsDir = @"C:\Temp\ControlledDocs";
            var pdfFiles = Directory.GetFiles(docsDir, "*.pdf");

            foreach (var pdfPath in pdfFiles)
            {
                string content = ExtractTextFromPdf(pdfPath);
                string docId = Path.GetFileNameWithoutExtension(pdfPath);
                string title = Path.GetFileName(pdfPath);
                string department = "IT"; // Or extract from filename/metadata if needed
                Console.WriteLine($"Processing {title}...");
                await AddPolicyDocumentAsync(docId, title, content, department, collection);
            }
        }

        // Helper for deserializing Ollama embedding response
        private class OllamaEmbeddingResponse
        {
            public float[] embedding { get; set; } = Array.Empty<float>();
        }
    }
}
