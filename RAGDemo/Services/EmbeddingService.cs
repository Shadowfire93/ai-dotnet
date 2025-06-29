using System.Threading.Tasks;
using RAGDemo.Interfaces;

namespace RAGDemo.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        public async Task<float[]> GetEmbeddingAsync(string text, string model)
        {
            // TODO: Implement embedding logic (call Ollama or other service)
            await Task.CompletedTask;
            return new float[0];
        }
    }
}
