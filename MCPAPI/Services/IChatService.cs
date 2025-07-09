using Common.Models;
using Microsoft.SemanticKernel;
using System.Threading.Tasks;

namespace MCPAPI.Services
{
    public interface IChatService
    {
        IAsyncEnumerable<StreamingChatMessageContent> ProcessPrompt(IList<Query> queries);
    }
}
