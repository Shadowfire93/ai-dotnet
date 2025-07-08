using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeatherAPI.Data;
using WeatherAPI.Entities;

namespace WeatherAPI.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherDbContext _db;
        public WeatherService(WeatherDbContext db)
        {
            _db = db;
        }

        public async Task<List<WeatherForecastEntity>> GetForecastsAsync()
        {
            return await _db.WeatherForecasts.ToListAsync();
        }

        public async Task<WeatherForecastEntity?> GetForecastByIdAsync(int id)
        {
            return await _db.WeatherForecasts.FindAsync(id);
        }

        public async Task<WeatherForecastEntity> AddForecastAsync(WeatherForecastEntity forecast)
        {
            _db.WeatherForecasts.Add(forecast);
            await _db.SaveChangesAsync();
            return forecast;
        }

        public async Task<WeatherForecastEntity?> UpdateForecastAsync(WeatherForecastEntity forecast)
        {
            var existing = await _db.WeatherForecasts.FindAsync(forecast.Id);
            if (existing == null) return null;
            _db.Entry(existing).CurrentValues.SetValues(forecast);
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteForecastAsync(int id)
        {
            var entity = await _db.WeatherForecasts.FindAsync(id);
            if (entity == null) return false;
            _db.WeatherForecasts.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<WeatherForecastEntity>> GetFiveDayForecastAsync()
        {
            var today = DateTime.Today;
            return await _db.WeatherForecasts
                .Where(f => f.Date >= today)
                .OrderBy(f => f.Date)
                .Take(5)
                .ToListAsync();
        }
    }
}
