using RAGDemo.Interfaces;
using RAGDemo.Services;

namespace RAGDemo
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // ...existing code...
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IEmbeddingService, EmbeddingService>();
        }
    }
}