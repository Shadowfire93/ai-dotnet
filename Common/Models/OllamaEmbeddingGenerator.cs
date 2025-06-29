using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Common.Models
{
    public class OllamaEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public OllamaEmbeddingGenerator(HttpClient httpClient, string model = "bge-large")
        {
            _httpClient = httpClient;
            _model = model;
        }

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> data, EmbeddingGenerationOptions? options, CancellationToken cancellationToken = default)
        {
            var results = new List<Embedding<float>>();
            foreach (var text in data)
            {
                var embedding = await GetEmbeddingAsync(text, cancellationToken);
                results.Add(new Embedding<float>(embedding));
            }
            return new GeneratedEmbeddings<Embedding<float>>(results);
        }

        public object? GetService(Type serviceType, object? context) => null;

        public void Dispose() { /* No resources to dispose */ }

        private async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            var requestBody = new { model = _model, prompt = text };
            var response = await _httpClient.PostAsync(
                "/api/embeddings",
                new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"),
                cancellationToken
            );
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var embedding = new List<float>();
            foreach (var x in doc.RootElement.GetProperty("embedding").EnumerateArray())
            {
                embedding.Add(x.GetSingle());
            }
            return embedding.ToArray();
        }
    }
}
