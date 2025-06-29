using System.Threading.Tasks;

namespace RAGDemo.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text, string model);
    }
}
