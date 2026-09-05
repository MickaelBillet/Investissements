namespace InvestissementsDashboard.Client.Services;

public class DashboardPasswordHandler(ISessionService sessionService) : DelegatingHandler
{
    private const string PasswordHeader = "x-dashboard-password";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (sessionService.Password is { } password)
            request.Headers.Add(PasswordHeader, password);

        return base.SendAsync(request, cancellationToken);
    }
}
