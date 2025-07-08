using System;
using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.Entities
{
    public class WeatherForecastEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public int MinTemperatureC { get; set; }
        public int MaxTemperatureC { get; set; }
        public double ChanceOfRain { get; set; } // 0.0 to 1.0
        public double MinRainfallMm { get; set; }
        public double MaxRainfallMm { get; set; }
        public string? Summary { get; set; }
    }
}
