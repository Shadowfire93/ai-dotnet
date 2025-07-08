using Microsoft.EntityFrameworkCore;
using WeatherAPI.Entities;

namespace WeatherAPI.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

        public DbSet<WeatherForecastEntity> WeatherForecasts { get; set; }
    }
}
