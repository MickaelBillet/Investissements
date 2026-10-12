using AgentAI;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.News;

public class CompanyTitleFilterTests
{
    private static NewsArticle Article(string title) => new(title, "S", DateTimeOffset.UnixEpoch, $"https://a/{title.GetHashCode()}");

    [Theory]
    [InlineData("Édenred", "EDENRED : nouveau contrat")]
    [InlineData("electricite de france", "Électricité de France en hausse")]
    [InlineData("Wolters Kluwer NV", "Wolters Kluwer gagne un contrat")]
    public void Filter_TitleNamingCompany_IsKept(string company, string title)
    {
        Assert.Single(CompanyTitleFilter.Filter([Article(title)], company));
    }

    [Fact]
    public void Filter_PartialWord_DoesNotMatch()
    {
        var result = CompanyTitleFilter.Filter([Article("Farm equipment sales rise"), Article("ARM annonce un processeur")], "ARM");

        Assert.Equal(["ARM annonce un processeur"], result.Select(a => a.Title));
    }

    [Fact]
    public void Filter_CompanyWithOnlyPunctuation_KeepsEverything()
    {
        var articles = new[] { Article("Anything") };

        Assert.Same(articles, CompanyTitleFilter.Filter(articles, "---"));
    }

    [Fact]
    public void Filter_OnlyLegalSuffixAsName_StillMatchesIt()
    {
        Assert.Single(CompanyTitleFilter.Filter([Article("SA annonce ses résultats")], "SA"));
    }
}
