// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;

namespace AgentAI;

/// <summary>
/// Simulated weather source (no external dependency).
/// </summary>
internal sealed class WeatherService : IWeatherService
{
    [Description("Gets the current weather for a given city.")]
    public string GetWeather([Description("The city to get the weather for.")] string city)
    {
        Console.WriteLine($"[TOOL CALL] GetWeather appelé avec city={city}");

        string[] conditions = ["ensoleillé", "nuageux", "pluvieux", "venteux"];
        var condition = conditions[Random.Shared.Next(conditions.Length)];
        var temperature = Random.Shared.Next(-5, 35);
        return $"À {city}, il fait {temperature}°C et le temps est {condition}.";
    }
}
