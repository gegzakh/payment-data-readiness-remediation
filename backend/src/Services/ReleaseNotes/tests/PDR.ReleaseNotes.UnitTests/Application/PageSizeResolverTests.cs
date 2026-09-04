using AwesomeAssertions;
using NSubstitute;
using PDR.BuildingBlocks.Core.Settings;
using PDR.ReleaseNotes.Application.Releases;

namespace PDR.ReleaseNotes.UnitTests.Application;

public sealed class PageSizeResolverTests
{
    private readonly ISettingsReader _settings = Substitute.For<ISettingsReader>();

    public PageSizeResolverTests()
    {
        _settings.GetAsync(ReleaseNotesSettingKeys.DefaultPageSize, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(20);
        _settings.GetAsync(ReleaseNotesSettingKeys.MaxPageSize, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(100);
        _settings.GetAsync(ReleaseNotesSettingKeys.AllowedPageSizes, Arg.Any<CancellationToken>())
            .Returns("10,20,50");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Missing_or_invalid_page_size_falls_back_to_the_configured_default(int? requested)
    {
        var resolver = new PageSizeResolver(_settings);

        (await resolver.ResolveAsync(requested, TestContext.Current.CancellationToken)).Should().Be(20);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public async Task Allowed_page_sizes_are_honoured(int requested)
    {
        var resolver = new PageSizeResolver(_settings);

        (await resolver.ResolveAsync(requested, TestContext.Current.CancellationToken)).Should().Be(requested);
    }

    [Fact]
    public async Task Page_size_above_the_maximum_is_capped()
    {
        var resolver = new PageSizeResolver(_settings);

        (await resolver.ResolveAsync(5_000, TestContext.Current.CancellationToken)).Should().Be(100);
    }

    [Fact]
    public async Task Allowed_page_sizes_come_from_settings()
    {
        _settings.GetAsync(ReleaseNotesSettingKeys.AllowedPageSizes, Arg.Any<CancellationToken>())
            .Returns("25, 5,25");
        var resolver = new PageSizeResolver(_settings);

        (await resolver.GetAllowedPageSizesAsync(TestContext.Current.CancellationToken)).Should().Equal(5, 25);
    }

    [Fact]
    public async Task Malformed_allowed_page_sizes_fall_back_to_the_built_in_list()
    {
        _settings.GetAsync(ReleaseNotesSettingKeys.AllowedPageSizes, Arg.Any<CancellationToken>())
            .Returns("abc, ,-1");
        var resolver = new PageSizeResolver(_settings);

        (await resolver.GetAllowedPageSizesAsync(TestContext.Current.CancellationToken)).Should().Equal(PageSizeResolver.FallbackAllowedPageSizes);
    }
}
