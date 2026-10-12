using AgentAI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.News;

public class NewsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private static NewsService Create(params INewsFeedSource[] sources) =>
        new(sources, new FixedTimeProvider(Now), NullLogger<NewsService>.Instance);

    private static NewsArticle Article(string title, string url, int daysAgo = 1) =>
        new(title, "S", Now.AddDays(-daysAgo), url);

    private static Mock<INewsFeedSource> Source(IReadOnlyList<NewsArticle>? articles)
    {
        var mock = new Mock<INewsFeedSource>();
        mock.SetupGet(s => s.Name).Returns("src");
        mock.Setup(s => s.FetchAsync(It.IsAny<NewsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(articles);
        return mock;
    }

    private static Mock<INewsFeedSource> FailingSource()
    {
        var mock = new Mock<INewsFeedSource>();
        mock.SetupGet(s => s.Name).Returns("failing");
        mock.Setup(s => s.FetchAsync(It.IsAny<NewsRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("boom"));
        return mock;
    }

    [Fact]
    public async Task GetLatestNewsAsync_TwoSources_MergesMostRecentFirst()
    {
        var service = Create(
            Source([Article("Old", "https://a/1", daysAgo: 3)]).Object,
            Source([Article("New", "https://b/1", daysAgo: 1)]).Object);

        var result = await service.GetLatestNewsAsync("Acme");

        Assert.Equal(["New", "Old"], result.Select(a => a.Title));
    }

    [Fact]
    public async Task GetLatestNewsAsync_OneSourceFails_ReturnsTheOthers()
    {
        var service = Create(FailingSource().Object, Source([Article("Kept", "https://a/1")]).Object);

        var result = await service.GetLatestNewsAsync("Acme");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLatestNewsAsync_AllApplicableSourcesFail_Throws()
    {
        var service = Create(FailingSource().Object, FailingSource().Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetLatestNewsAsync("Acme"));
    }

    [Fact]
    public async Task GetLatestNewsAsync_NotApplicableSourceAndFailingSource_Throws()
    {
        var service = Create(Source(null).Object, FailingSource().Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetLatestNewsAsync("Acme"));
    }

    [Fact]
    public async Task GetLatestNewsAsync_NotApplicableSourceAndEmptySource_ReturnsEmptyList()
    {
        var service = Create(Source(null).Object, Source([]).Object);

        var result = await service.GetLatestNewsAsync("Acme");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestNewsAsync_SourceCancelled_PropagatesCancellation()
    {
        var cancelled = new Mock<INewsFeedSource>();
        cancelled.SetupGet(s => s.Name).Returns("cancelled");
        cancelled.Setup(s => s.FetchAsync(It.IsAny<NewsRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(cancelled.Object).GetLatestNewsAsync("Acme"));
    }

    [Fact]
    public async Task GetLatestNewsAsync_ArticleOlderThanWindow_IsDropped()
    {
        var service = Create(Source([Article("Fresh", "https://a/1", daysAgo: 2), Article("Stale", "https://a/2", daysAgo: 10)]).Object);

        var result = await service.GetLatestNewsAsync("Acme", days: 7);

        Assert.Equal(["Fresh"], result.Select(a => a.Title));
    }

    [Fact]
    public async Task GetLatestNewsAsync_SameUrlOrSameTitleAcrossSources_IsDeduplicated()
    {
        var service = Create(
            Source([Article("Acme wins a deal", "https://a/1")]).Object,
            Source([Article("Other title", "https://a/1"), Article("ACME wins a deal!", "https://b/2")]).Object);

        var result = await service.GetLatestNewsAsync("Acme");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLatestNewsAsync_MoreThanThirtyArticles_IsCapped()
    {
        var many = Enumerable.Range(0, 40).Select(i => Article($"Title {i}", $"https://a/{i}")).ToList();

        var result = await Create(Source(many).Object).GetLatestNewsAsync("Acme");

        Assert.Equal(30, result.Count);
    }

    [Fact]
    public async Task GetLatestNewsAsync_Request_IsForwardedToEverySource()
    {
        var first = Source([]);
        var second = Source([]);

        await Create(first.Object, second.Object).GetLatestNewsAsync("Acme", "FR0000120073", 5);

        var expected = new NewsRequest("Acme", "FR0000120073", 5);
        first.Verify(s => s.FetchAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
        second.Verify(s => s.FetchAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestNewsAsync_BlankCompany_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Create(Source([]).Object).GetLatestNewsAsync(" "));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
