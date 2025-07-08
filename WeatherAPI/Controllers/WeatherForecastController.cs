using Microsoft.AspNetCore.Mvc;
using WeatherAPI.Entities;
using WeatherAPI.Services;

namespace WeatherAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IWeatherService weatherService, ILogger<WeatherForecastController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeatherForecastEntity>>> GetAll()
        {
            var forecasts = await _weatherService.GetForecastsAsync();
            return Ok(forecasts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WeatherForecastEntity>> GetById(int id)
        {
            var forecast = await _weatherService.GetForecastByIdAsync(id);
            if (forecast == null) return NotFound();
            return Ok(forecast);
        }

        [HttpPost]
        public async Task<ActionResult<WeatherForecastEntity>> Create([FromBody] WeatherForecastEntity forecast)
        {
            var created = await _weatherService.AddForecastAsync(forecast);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<WeatherForecastEntity>> Update(int id, [FromBody] WeatherForecastEntity forecast)
        {
            if (id != forecast.Id) return BadRequest();
            var updated = await _weatherService.UpdateForecastAsync(forecast);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _weatherService.DeleteForecastAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("5day")]
        public async Task<ActionResult<IEnumerable<WeatherForecastEntity>>> GetFiveDayForecast()
        {
            var fiveDay = await _weatherService.GetFiveDayForecastAsync();
            return Ok(fiveDay);
        }
    }
}
