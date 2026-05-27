using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Api.Mcp;

public sealed record JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string Jsonrpc { get; init; } = "2.0";
    [JsonPropertyName("id")]      public JsonElement? Id { get; init; }
    [JsonPropertyName("method")]  public string Method { get; init; } = "";
    [JsonPropertyName("params")]  public JsonElement? Params { get; init; }
}

public sealed record JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string Jsonrpc { get; init; } = "2.0";
    [JsonPropertyName("id")]      public JsonElement? Id { get; init; }
    [JsonPropertyName("result")]  public object? Result { get; init; }
    [JsonPropertyName("error")]   public JsonRpcError? Error { get; init; }
}

public sealed record JsonRpcError(
    [property: JsonPropertyName("code")]    int Code,
    [property: JsonPropertyName("message")] string Message);

public static class JsonRpcErrors
{
    public const int ParseError     = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams  = -32602;
    public const int InternalError  = -32603;
}

public sealed record McpInitializeResult(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("serverInfo")]       McpServerInfo ServerInfo,
    [property: JsonPropertyName("capabilities")]     McpCapabilities Capabilities);

public sealed record McpServerInfo(
    [property: JsonPropertyName("name")]    string Name,
    [property: JsonPropertyName("version")] string Version);

public sealed record McpCapabilities(
    [property: JsonPropertyName("tools")] object Tools);

public sealed record McpToolsListResult(
    [property: JsonPropertyName("tools")] IReadOnlyList<McpToolDefinition> Tools);

public sealed record McpToolDefinition(
    [property: JsonPropertyName("name")]        string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("inputSchema")] object InputSchema);

public sealed record McpToolsCallResult(
    [property: JsonPropertyName("content")] IReadOnlyList<McpContent> Content);

public sealed record McpContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);

internal static class McpJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy       = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition     = JsonIgnoreCondition.WhenWritingNull
    };
}
