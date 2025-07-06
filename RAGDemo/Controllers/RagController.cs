using Common.Models;
using Microsoft.AspNetCore.Mvc;
using RAGDemo.Interfaces;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace RAGDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RagController : ControllerBase
    {
        private readonly IChatService _chatService;
        public RagController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RagRequest request)
        {
            var response = await _chatService.GetChatResponseAsync(
                request.Prompt,
                request.History,
                request.Settings.Model,
                request.Settings.GroundingPrompt,
                request.Settings.EmbeddingsModel);
            return Ok(new { response });
        }
    }
}
