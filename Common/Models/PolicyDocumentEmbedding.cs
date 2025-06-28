using Microsoft.Extensions.VectorData;
using System;

namespace Common.Models
{
    // Model for storing policy document embeddings in the vector store
    public class PolicyDocumentEmbedding
    {
        [VectorStoreKey]
        public string Id { get; set; } = string.Empty;

        [VectorStoreData(IsIndexed = true)]
        public string Title { get; set; } = string.Empty;

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Content { get; set; } = string.Empty;

        [VectorStoreData(IsIndexed = true)]
        public string Department { get; set; } = string.Empty;

        [VectorStoreVector(Dimensions: 1536)] // Adjust to your embedding size
        public ReadOnlyMemory<float>? Embedding { get; set; }
    }
}
