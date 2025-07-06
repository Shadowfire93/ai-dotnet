using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RAGDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Test : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {

            var _httpClient = new HttpClient { BaseAddress = new Uri("http://ollama:11434/") };
            var requestBody = new { model = "mistral", prompt = "Tell me about redis", stream = false };
            var response = await _httpClient.PostAsync("/api/generate", new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.TryGetProperty("response", out var respProp) ? respProp.GetString() : json;

            return Ok(new { result });

        }
    }
}
