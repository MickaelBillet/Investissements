namespace InvestissementsDashboard.Client.Services;

public class DashboardPasswordHandler(ISessionService sessionService) : DelegatingHandler
{
    private const string PasswordHeader = "x-dashboard-password";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (sessionService.IsSessionExpired)
        {
            await sessionService.LogoutAsync();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        if (sessionService.Password is { } password)
            request.Headers.Add(PasswordHeader, password);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            await sessionService.ExtendSessionAsync();

        return response;
    }
}
