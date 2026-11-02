// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;

namespace AgentAI;

/// <summary>
/// Provides weather information, exposed to agents as a tool.
/// </summary>
public interface IWeatherService
{
    [Description("Gets the current weather for a given city.")]
    string GetWeather([Description("The city to get the weather for.")] string city);
}
