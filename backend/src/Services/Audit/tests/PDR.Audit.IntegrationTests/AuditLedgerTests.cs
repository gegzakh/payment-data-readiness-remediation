using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PDR.Audit.Application.Ledger;
using PDR.Audit.Infrastructure.Persistence;
using PDR.BuildingBlocks.Core.Paging;

namespace PDR.Audit.IntegrationTests;

public sealed class AuditLedgerTests(AuditApiFactory factory) : IClassFixture<AuditApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<AuditRecordDto> AppendAsync(string entityId, string action = "ruleset.activated")
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/audit",
            new
            {
                service = "rules",
                action,
                entityType = "Ruleset",
                entityId,
                outcome = "Success",
                actor = "alice",
                actorId = "user-1",
                legalEntity = "LE-01",
                details = "{\"version\":2}"
            },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var record = await response.Content.ReadFromJsonAsync<AuditRecordDto>(Json, Token);
        return record!;
    }

    [Fact]
    public async Task Appended_records_form_a_chain_that_verifies()
    {
        var first = await AppendAsync(Guid.NewGuid().ToString());
        var second = await AppendAsync(Guid.NewGuid().ToString(), "ruleset.retired");

        second.Sequence.Should().Be(first.Sequence + 1);
        second.PreviousHash.Should().Be(first.Hash);

        var verification = await _client.GetFromJsonAsync<AuditChainVerificationDto>("/api/v1/audit/verify", Json, Token);
        verification!.IsIntact.Should().BeTrue();
        verification.FirstBrokenSequence.Should().BeNull();
        verification.RecordsChecked.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Records_are_searchable_newest_first_and_by_entity()
    {
        var entityId = Guid.NewGuid().ToString();
        await AppendAsync(entityId);
        await AppendAsync(entityId, "ruleset.rolled_back");

        var page = await _client.GetFromJsonAsync<PagedResult<AuditRecordDto>>(
            $"/api/v1/audit?entityType=Ruleset&entityId={entityId}", Json, Token);

        page.Should().NotBeNull();
        page.TotalCount.Should().Be(2);
        page.Items.Select(record => record.Action).Should().Equal("ruleset.rolled_back", "ruleset.activated");
    }

    [Fact]
    public async Task A_single_record_is_retrievable_by_id()
    {
        var appended = await AppendAsync(Guid.NewGuid().ToString());

        var fetched = await _client.GetFromJsonAsync<AuditRecordDto>($"/api/v1/audit/{appended.Id}", Json, Token);

        fetched!.Hash.Should().Be(appended.Hash);
    }

    [Fact]
    public async Task An_unknown_record_returns_problem_details()
    {
        var response = await _client.GetAsync($"/api/v1/audit/{Guid.NewGuid()}", Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(Token)).Should().Contain("AUDIT.NOT_FOUND");
    }

}

/// <summary>Runs against its own ledger, because it deliberately corrupts the chain.</summary>
public sealed class AuditTamperDetectionTests(AuditApiFactory factory) : IClassFixture<AuditApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Tampering_with_a_stored_record_is_detected_by_verification()
    {
        await Append("scheme.created");
        var target = await Append("scheme.updated");

        // Simulate a DBA editing history behind the application's back.
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            await context.Database.ExecuteSqlAsync(
                $"UPDATE audit_records SET actor = 'mallory' WHERE id = {target.Id}",
                Token);
        }

        var verification = await _client.GetFromJsonAsync<AuditChainVerificationDto>("/api/v1/audit/verify", Json, Token);

        verification!.IsIntact.Should().BeFalse();
        verification.FirstBrokenSequence.Should().Be(target.Sequence);
    }

    private async Task<AuditRecordDto> Append(string action)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/audit",
            new
            {
                service = "rules",
                action,
                entityType = "Scheme",
                entityId = Guid.NewGuid().ToString(),
                outcome = "Success",
                actor = "alice"
            },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AuditRecordDto>(Json, Token))!;
    }
}
