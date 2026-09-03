using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.WebApi;

namespace PDR.BuildingBlocks.UnitTests;

public sealed class ProblemDetailsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Concurrency, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unprocessable, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ErrorType.RateLimited, StatusCodes.Status429TooManyRequests)]
    [InlineData(ErrorType.Dependency, StatusCodes.Status502BadGateway)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void Every_error_type_maps_to_one_agreed_status_code(ErrorType type, int expected)
    {
        PdrProblemDetails.StatusCodeFor(type).Should().Be(expected);
    }

    [Fact]
    public void A_problem_carries_the_correlation_id_so_a_report_can_be_traced_to_its_logs()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/rules/versions";
        context.Response.Headers[CorrelationContext.HeaderName] = "corr-123";

        var problem = PdrProblemDetails.Create(
            Error.Conflict("RULES.VERSION_ACTIVE", "An active version already covers that date."),
            context);

        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Title.Should().Be("Conflicting state");
        problem.Instance.Should().Be("POST /api/v1/rules/versions");
        problem.Extensions["code"].Should().Be("RULES.VERSION_ACTIVE");
        problem.Extensions["correlationId"].Should().Be("corr-123");
        problem.Extensions.Should().NotContainKey("errors");
    }

    [Fact]
    public void Field_level_failures_survive_as_the_errors_extension()
    {
        var problem = PdrProblemDetails.Create(
            Error.Validation(
                "RULES.INVALID",
                "The ruleset is not valid.",
                new Dictionary<string, string[]> { ["townName"] = ["Town name is required."] }),
            new DefaultHttpContext());

        problem.Extensions["errors"].Should().BeAssignableTo<IReadOnlyDictionary<string, string[]>>()
            .Which.Should().ContainKey("townName");
    }
}
