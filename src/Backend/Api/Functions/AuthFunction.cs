using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InvestissementsDashboard.Api.Functions;

public sealed class AuthFunction
{
    [Function(nameof(Verify))]
    public IActionResult Verify(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/verify")] HttpRequest req)
    {
        // La vérification du mot de passe est faite par DashboardAuthMiddleware pour toutes les
        // routes protégées, y compris celle-ci. Si l'exécution arrive ici, le mot de passe fourni
        // était correct.
        return new OkResult();
    }
}
