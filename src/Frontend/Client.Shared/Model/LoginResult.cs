namespace InvestissementsDashboard.Client.Model;

public enum LoginResult
{
    Success,
    InvalidPassword,

    /// <summary>The Api gave no verdict on the password: it failed or could not be reached.</summary>
    ServiceUnavailable
}
