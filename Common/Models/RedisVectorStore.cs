using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Common.Models
{
    #pragma warning disable CS8601, CS8604, CS8600, CS8602, CS8603

    public class RedisVectorStore
    {
        private readonly IDatabase _db;
        private readonly string _collection;

        public RedisVectorStore(IDatabase db, string collection = "policy-documents")
        {
            _db = db;
            _collection = collection;
        }

        public async Task InsertAsync(PolicyDocumentEmbedding record)
        {
            var key = $"{_collection}:{record.Id}";
            var hash = new HashEntry[]
            {
                new HashEntry("Id", record.Id),
                new HashEntry("Title", record.Title ?? string.Empty),
                new HashEntry("Content", record.Content ?? string.Empty),
                new HashEntry("Department", record.Department ?? string.Empty),
                new HashEntry("Embedding", JsonSerializer.Serialize(record.Embedding?.ToArray() ?? Array.Empty<float>()))
            };
            await _db.HashSetAsync(key, hash);
            await _db.SetAddAsync(_collection, record.Id); // For fast scan
        }

        public async Task<List<PolicyDocumentEmbedding>> SearchAsync(float[] queryEmbedding, int topK = 3, double minScore = 0.3)
        {
            var ids = (await _db.SetMembersAsync(_collection)).Select(x => x.ToString()).ToList();
            var results = new List<(PolicyDocumentEmbedding, double)>();
            foreach (var id in ids)
            {
                var key = $"{_collection}:{id}";
                var hash = await _db.HashGetAllAsync(key);
                var dict = hash.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
                if (!dict.TryGetValue("Embedding", out var embStr))
                {
                    continue;
                }
                var embedding = JsonSerializer.Deserialize<float[]>(embStr);
                if (embedding == null || embedding.Length != queryEmbedding.Length)
                {
                    continue;
                }
                var score = CosineSimilarity(queryEmbedding, embedding);
                if (score >= minScore)
                {
                    results.Add((new PolicyDocumentEmbedding
                    {
                        Id = dict.GetValueOrDefault("Id") ?? string.Empty,
                        Title = dict.GetValueOrDefault("Title") ?? string.Empty,
                        Content = dict.GetValueOrDefault("Content") ?? string.Empty,
                        Department = dict.GetValueOrDefault("Department") ?? string.Empty,
                        Embedding = embedding
                    }, score));
                }
            }
            return results.OrderByDescending(x => x.Item2).Take(topK).Select(x => x.Item1).ToList();
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return (magA == 0 || magB == 0) ? 0 : dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
    }

    #pragma warning restore CS8601, CS8604, CS8600, CS8602, CS8603
}
