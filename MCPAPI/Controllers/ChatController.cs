using Common.Models;
using MCPAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCPAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("process")]
        public async Task ProcessPrompt(IList<Query> queries)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            await foreach (var message in _chatService.ProcessPrompt(queries))
            {
                //var json = JsonSerializer.Serialize(message.Content);
                //await Response.WriteAsync(json ?? "");
                await Response.WriteAsync(message.Content ?? "");
                await Response.Body.FlushAsync();
            }
        }
    }
}
