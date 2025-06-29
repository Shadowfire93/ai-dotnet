using Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RAGDemo.Interfaces
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string prompt, List<string> history, string model, string groundingPrompt, string embeddingsModel);
        Task<List<PolicyDocumentEmbedding>> GetRelevantParagraphsAsync(string prompt, string embeddingsModel, int topK = 3);
    }
}
