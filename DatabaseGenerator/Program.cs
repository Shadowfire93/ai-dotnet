using Microsoft.SemanticKernel.Memory;
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

        static IEnumerable<string> ChunkText(string text, int maxWords = 512)
        {
            // Chunk by whole words, not by characters
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i += maxWords)
            {
                yield return string.Join(' ', words.Skip(i).Take(maxWords));
            }
        }

        static async Task AddPolicyDocumentAsync(string id, string title, string content, string department, Common.Models.RedisVectorStore vectorStore)
        {
            int chunkIndex = 0;
            foreach (var chunk in ChunkText(content, 128))
            {
                chunkIndex = await AddChunkWithRetryAsync(id, title, chunk, department, vectorStore, chunkIndex);
            }
        }

        static async Task<int> AddChunkWithRetryAsync(string id, string title, string chunk, string department, Common.Models.RedisVectorStore vectorStore, int chunkIndex)
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
                    Embedding = embedding
                };
                await vectorStore.InsertAsync(record);
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
                chunkIndex = await AddChunkWithRetryAsync(id, title, left, department, vectorStore, chunkIndex);
                chunkIndex = await AddChunkWithRetryAsync(id, title, right, department, vectorStore, chunkIndex);
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
            var vectorStore = new Common.Models.RedisVectorStore(db);

            // Directory containing PDF documents
            string docsDir = @"C:\Temp\ControlledDocs";
            var pdfFiles = Directory.GetFiles(docsDir, "*.pdf");// "Board Governance Policy.pdf");

            foreach (var pdfPath in pdfFiles)
            {
                string content = ExtractTextFromPdf(pdfPath);
                string docId = Path.GetFileNameWithoutExtension(pdfPath);
                string title = Path.GetFileName(pdfPath);
                string department = "IT"; // Or extract from filename/metadata if needed
                Console.WriteLine($"Processing {title}...");
                await AddPolicyDocumentAsync(docId, title, content, department, vectorStore);
            }
        }

        // Helper for deserializing Ollama embedding response
        private class OllamaEmbeddingResponse
        {
            public float[] embedding { get; set; } = Array.Empty<float>();
        }
    }
}
