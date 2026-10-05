namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// Host-provided settings. The host (WASM or MAUI) decides where the Api lives so the shared UI never
/// relies on NavigationManager.BaseUri, which is meaningless inside a BlazorWebView.
/// </summary>
public sealed record ClientOptions(Uri ApiBaseUri)
{
    // Absolute on purpose: a relative link would resolve against the BlazorWebView origin and not open.
    public Uri DocumentationUri => new(ApiBaseUri, "docs/presentation.pdf");
}
