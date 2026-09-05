using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class GetRolesFunction
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GetRolesFunction> _logger;

    public GetRolesFunction(IConfiguration configuration, ILogger<GetRolesFunction> logger)
    {
        _configuration = configuration;
        _logger        = logger;
    }

    [Function(nameof(GetRoles))]
    public async Task<IActionResult> GetRoles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GetRoles")] HttpRequest req,
        CancellationToken ct)
    {
        ClientPrincipalRequest? principal;
        try
        {
            principal = await JsonSerializer.DeserializeAsync<ClientPrincipalRequest>(req.Body, JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Static Web Apps roles request body.");
            return new OkObjectResult(new RolesResponse([]));
        }

        var ownerIdentity = _configuration["OWNER_IDENTITY"];
        if (string.IsNullOrEmpty(ownerIdentity) || principal?.UserDetails is null)
        {
            _logger.LogInformation(
                "GetRoles: no role granted (identityProvider={IdentityProvider}, userDetails={UserDetails}, ownerIdentityConfigured={OwnerIdentityConfigured}).",
                principal?.IdentityProvider, principal?.UserDetails, !string.IsNullOrEmpty(ownerIdentity));
            return new OkObjectResult(new RolesResponse([]));
        }

        var isOwner = string.Equals(principal.UserDetails, ownerIdentity, StringComparison.OrdinalIgnoreCase);
        _logger.LogInformation(
            "GetRoles: identityProvider={IdentityProvider}, userDetails={UserDetails}, ownerGranted={OwnerGranted}.",
            principal.IdentityProvider, principal.UserDetails, isOwner);
        return new OkObjectResult(new RolesResponse(isOwner ? ["owner"] : []));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record ClientPrincipalRequest(
        [property: JsonPropertyName("identityProvider")] string? IdentityProvider,
        [property: JsonPropertyName("userId")] string? UserId,
        [property: JsonPropertyName("userDetails")] string? UserDetails);

    private sealed record RolesResponse([property: JsonPropertyName("roles")] string[] Roles);
}
