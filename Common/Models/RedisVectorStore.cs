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
            var embedding = record.Embedding?.ToArray() ?? Array.Empty<float>();
            var embeddingBytes = new byte[embedding.Length * 4];
            Buffer.BlockCopy(embedding, 0, embeddingBytes, 0, embeddingBytes.Length);
            var hash = new HashEntry[]
            {
                new HashEntry("Id", record.Id),
                new HashEntry("Title", record.Title ?? string.Empty),
                new HashEntry("Content", record.Content ?? string.Empty),
                new HashEntry("Department", record.Department ?? string.Empty),
                new HashEntry("Embedding", JsonSerializer.Serialize(record.Embedding?.ToArray() ?? Array.Empty<float>())),
                new HashEntry("EmbeddingBytes", embeddingBytes)
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

        public async Task RemoveAsync(string id)
        {
            var key = $"{_collection}:{id}";
            await _db.KeyDeleteAsync(key);
            await _db.SetRemoveAsync(_collection, id);
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

        public async Task<List<PolicyDocumentEmbedding>> SearchVectorAsync(float[] queryEmbedding, int topK = 3)
        {
            // Convert float[] to byte[] (float32 LE)
            var bytes = new byte[queryEmbedding.Length * 4];
            Buffer.BlockCopy(queryEmbedding, 0, bytes, 0, bytes.Length);

            // FT.SEARCH policy-documents-idx "*=>[KNN $K @Embedding $vec as score]" PARAMS 4 K 3 vec <BINARY> SORTBY score RETURN 4 Id Title Content score DIALECT 2
            var args = new List<RedisValue>
            {
                "policy-documents-idx",
                "*=>[KNN $K @EmbeddingBytes $vec as score]",
                "PARAMS", 4, "K", topK, "vec", bytes,
                "SORTBY", "score",
                "RETURN", 4, "Id", "Title", "Content", "score",
                "DIALECT", 2
            };
            var result = await _db.ExecuteAsync("FT.SEARCH", args.ToArray());
            var list = new List<PolicyDocumentEmbedding>();
            if (result.Resp2Type == ResultType.Array)
            {
                var arr = (RedisResult[])result;
                for (int i = 2; i < arr.Length; i += 2)
                {
                    var fields = (RedisResult[])arr[i];
                    var dict = new Dictionary<string, string>();
                    for (int j = 0; j < fields.Length; j += 2)
                    {
                        dict[fields[j].ToString()] = fields[j + 1].ToString();
                    }
                    list.Add(new PolicyDocumentEmbedding
                    {
                        Id = dict.GetValueOrDefault("Id") ?? string.Empty,
                        Title = dict.GetValueOrDefault("Title") ?? string.Empty,
                        Content = dict.GetValueOrDefault("Content") ?? string.Empty,
                        // Optionally parse score if needed
                    });
                }
            }
            return list;
        }

        public async Task EnsureIndexAsync(int embeddingDim = 768)
        {
            // Check if index exists
            var idxName = $"{_collection}-idx";
            try
            {
                var res = await _db.ExecuteAsync("FT.INFO", idxName);
                // If no exception, index exists
                return;
            }
            catch (RedisServerException ex) when (ex.Message.ToUpper().Contains(("no such index").ToUpper()))
            {
                // Index does not exist, create it
            }
            // Create index
            await _db.ExecuteAsync("FT.CREATE", idxName, "ON", "HASH", "PREFIX", 1, $"{_collection}:",
                "SCHEMA",
                "Id", "TEXT",
                "Title", "TEXT",
                "Content", "TEXT",
                "Department", "TEXT",
                "Embedding", "VECTOR", "FLAT", 6,
                    "TYPE", "FLOAT32",
                    "DIM", embeddingDim,
                    "DISTANCE_METRIC", "COSINE"
            );
        }
    }

    #pragma warning restore CS8601, CS8604, CS8600, CS8602, CS8603
}
