using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;

namespace PDR.BuildingBlocks.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void A_failed_result_refuses_to_hand_out_a_value()
    {
        var result = Result.Failure<string>(Error.NotFound("X.MISSING", "Nothing here."));

        result.IsFailure.Should().BeTrue();
        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void A_result_cannot_be_successful_and_carry_an_error()
    {
        var construct = () => Result.Failure(Error.None);

        construct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Match_picks_the_branch_that_matches_the_outcome()
    {
        Result.Success(3).Match(value => value * 2, _ => -1).Should().Be(6);
        Result.Failure<int>(Error.Conflict("X", "y")).Match(value => value * 2, _ => -1).Should().Be(-1);
    }
}

public sealed class PagedResultTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_partly_filled_last_page_still_counts_as_a_page()
    {
        var page = new PagedResult<int>([1, 2, 3], Page: 3, PageSize: 20, TotalCount: 41, AsOf);

        page.TotalPages.Should().Be(3);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void An_empty_page_reports_no_navigation_in_either_direction()
    {
        var page = PagedResult<int>.Empty(page: 1, pageSize: 20, AsOf);

        page.TotalPages.Should().Be(0);
        page.HasPreviousPage.Should().BeFalse();
        page.HasNextPage.Should().BeFalse();
        page.AsOfUtc.Should().Be(AsOf);
    }

    [Fact]
    public void A_zero_page_size_cannot_produce_a_division_by_zero()
    {
        new PagedResult<int>([], Page: 1, PageSize: 0, TotalCount: 10, AsOf).TotalPages.Should().Be(0);
    }
}
