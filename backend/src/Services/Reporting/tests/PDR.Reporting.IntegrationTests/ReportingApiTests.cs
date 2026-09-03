using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Reporting.Application.Dashboards;

namespace PDR.Reporting.IntegrationTests;

public sealed class ReportingApiTests(ReportingApiFactory factory) : IClassFixture<ReportingApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Executive_dashboard_is_captured_with_scope_and_freshness_metadata()
    {
        var client = factory.CreateClient();

        var dashboard = await GetAsync<DashboardDto>(client, "/api/v1/reporting/dashboards/executive?refresh=true");

        dashboard.Metrics.Should().Contain(metric => metric.Key == "future-readiness");
        dashboard.RulesetVersion.Should().Be("2026.1");
        dashboard.ScopeDescription.Should().Be("Whole portfolio");
        dashboard.SourceAsOfUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Repeated_requests_reuse_the_snapshot_until_a_refresh_is_asked_for()
    {
        var client = factory.CreateClient();
        const string url = "/api/v1/reporting/dashboards/scheme?schemeCodes=SEPA";

        var first = await GetAsync<DashboardDto>(client, url);
        var cached = await GetAsync<DashboardDto>(client, url);
        var refreshed = await GetAsync<DashboardDto>(client, url + "&refresh=true");

        cached.Id.Should().Be(first.Id);
        refreshed.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task Scope_filters_the_breakdown_rows()
    {
        var client = factory.CreateClient();

        var dashboard = await GetAsync<DashboardDto>(
            client,
            "/api/v1/reporting/dashboards/scheme?schemeCodes=SWIFT&refresh=true");

        dashboard.Breakdown.Should().ContainSingle().Which.Key.Should().Be("SWIFT");
    }

    [Fact]
    public async Task Drill_down_returns_the_rows_behind_a_metric()
    {
        var client = factory.CreateClient();

        var drill = await GetAsync<DrillDownDto>(
            client,
            "/api/v1/reporting/dashboards/executive/drill/Scheme");

        drill.Dimension.Should().Be("Scheme");
        drill.Rows.Should().HaveCount(2);
        drill.Rows[0].RejectedCount.Should().BeGreaterThanOrEqualTo(drill.Rows[1].RejectedCount);
    }

    [Fact]
    public async Task Drill_down_rejects_an_unknown_dimension()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/v1/reporting/dashboards/executive/drill/Nonsense", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Export_carries_the_provenance_header_rows()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/v1/reporting/dashboards/executive/export", UriKind.Relative));
        var csv = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        csv.Should().Contain("# Dashboard,Executive")
            .And.Contain("# Ruleset,2026.1")
            .And.Contain("# Reconciliation,Reconciled")
            .And.Contain("Section,Key,Label,Value,Unit");
    }

    [Fact]
    public async Task Snapshot_history_is_paged()
    {
        var client = factory.CreateClient();

        await GetAsync<DashboardDto>(client, "/api/v1/reporting/dashboards/operations?refresh=true");
        await GetAsync<DashboardDto>(client, "/api/v1/reporting/dashboards/operations?refresh=true");

        var page = await GetAsync<PagedResult<DashboardDto>>(
            client,
            "/api/v1/reporting/snapshots?audience=operations&page=1&pageSize=1");

        page.Items.Should().ContainSingle();
        page.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        page.PageSize.Should().Be(1);
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(new Uri(url, UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        value.Should().NotBeNull();
        return value!;
    }
}
