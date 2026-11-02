using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentAI;

internal sealed class WeatherAgentDefinitionBuilder(IWeatherService weatherService) : IAgentDefinitionBuilder
{
    public AgentKind Kind => AgentKind.Weather;

    public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        var city = AgentInput.Require(contextInput, "une ville");

        return Task.FromResult(new AgentDefinition(
            Instructions: "You are a weather assistant. Use the GetWeather tool to answer questions about weather.",
            Tools: [AIFunctionFactory.Create(weatherService.GetWeather)],
            HistoryFileName: "chat-history-weather.json",
            AIContextProviders: new CompositeContextProvider(new AIContextProvider[] { new WeatherContextProvider(city) })));
    }
}
