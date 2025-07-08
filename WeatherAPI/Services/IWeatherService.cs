using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherAPI.Entities;

namespace WeatherAPI.Services
{
    public interface IWeatherService
    {
        Task<List<WeatherForecastEntity>> GetForecastsAsync();
        Task<WeatherForecastEntity?> GetForecastByIdAsync(int id);
        Task<WeatherForecastEntity> AddForecastAsync(WeatherForecastEntity forecast);
        Task<WeatherForecastEntity?> UpdateForecastAsync(WeatherForecastEntity forecast);
        Task<bool> DeleteForecastAsync(int id);
        Task<List<WeatherForecastEntity>> GetFiveDayForecastAsync();
    }
}
