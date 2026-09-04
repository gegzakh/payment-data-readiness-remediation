using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.Rules.Application.Rulesets;
using PDR.Rules.Domain.Rulesets;

namespace PDR.Rules.IntegrationTests;

public sealed class RulesetLifecycleTests(RulesApiFactory factory) : IClassFixture<RulesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static object Rule(string code, string kind = "Required", string? parameter = null, string applicability = "Both") =>
        new
        {
            code,
            field = "Town",
            kind,
            severity = "Error",
            applicability,
            message = $"{code} failed.",
            parameter
        };

    private async Task<Guid> CreateRulesetAsync(string schemeCode)
    {
        var scheme = await _client.PostAsJsonAsync(
            "/api/v1/schemes",
            new { code = schemeCode, name = $"Scheme {schemeCode}", description = (string?)null, structuredAddressMandatoryFrom = (string?)null },
            Token);
        scheme.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await _client.PostAsJsonAsync(
            "/api/v1/rulesets",
            new { schemeCode, name = $"{schemeCode} address rules", description = (string?)null },
            Token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        return await created.Content.ReadFromJsonAsync<Guid>(Token);
    }

    [Fact]
    public async Task Seeded_sepa_ruleset_is_active_and_exposes_its_effective_rules()
    {
        var effective = await _client.GetFromJsonAsync<EffectiveRulesetDto>(
            "/api/v1/rulesets/effective?schemeCode=sepa&asOf=2026-01-01&mode=Current",
            Json,
            Token);

        effective.Should().NotBeNull();
        effective.SchemeCode.Should().Be("SEPA");
        effective.Rules.Should().NotBeEmpty();
        effective.Rules.Should().OnlyContain(rule =>
            rule.Applicability == RuleApplicability.Current || rule.Applicability == RuleApplicability.Both);
    }

    [Fact]
    public async Task Future_mode_returns_the_rules_that_only_apply_after_cutover()
    {
        var current = await _client.GetFromJsonAsync<EffectiveRulesetDto>(
            "/api/v1/rulesets/effective?schemeCode=SEPA&mode=Current", Json, Token);
        var future = await _client.GetFromJsonAsync<EffectiveRulesetDto>(
            "/api/v1/rulesets/effective?schemeCode=SEPA&mode=Future", Json, Token);

        current.Should().NotBeNull();
        future.Should().NotBeNull();
        future.Rules.Select(rule => rule.Code).Should().Contain("ADDR.STRUCTURED_ONLY");
        current.Rules.Select(rule => rule.Code).Should().NotContain("ADDR.STRUCTURED_ONLY");
    }

    [Fact]
    public async Task A_version_is_authored_activated_and_then_immutable()
    {
        var rulesetId = await CreateRulesetAsync($"T{Random.Shared.Next(1000, 9999)}");

        var addRule = await _client.PostAsJsonAsync(
            $"/api/v1/rulesets/{rulesetId}/versions/1/rules",
            Rule("ADDR.TOWN_REQUIRED"),
            Token);
        addRule.StatusCode.Should().Be(HttpStatusCode.OK);

        var activate = await _client.PostAsJsonAsync(
            $"/api/v1/rulesets/{rulesetId}/versions/1/activate",
            new { effectiveFrom = "2026-01-01" },
            Token);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterActivation = await _client.PostAsJsonAsync(
            $"/api/v1/rulesets/{rulesetId}/versions/1/rules",
            Rule("ADDR.POSTCODE_REQUIRED"),
            Token);

        afterActivation.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await afterActivation.Content.ReadAsStringAsync(Token)).Should().Contain("RULESET.VERSION_IMMUTABLE");
    }

    [Fact]
    public async Task Activating_a_new_version_retires_the_previous_one_and_can_be_rolled_back()
    {
        var rulesetId = await CreateRulesetAsync($"T{Random.Shared.Next(1000, 9999)}");
        await _client.PostAsJsonAsync($"/api/v1/rulesets/{rulesetId}/versions/1/rules", Rule("ADDR.TOWN_REQUIRED"), Token);
        await _client.PostAsJsonAsync($"/api/v1/rulesets/{rulesetId}/versions/1/activate", new { effectiveFrom = "2026-01-01" }, Token);

        var newVersion = await _client.PostAsJsonAsync(
            $"/api/v1/rulesets/{rulesetId}/versions",
            new { copyFromVersionNumber = 1, notes = "cutover rules" },
            Token);
        newVersion.StatusCode.Should().Be(HttpStatusCode.OK);
        (await newVersion.Content.ReadFromJsonAsync<int>(Token)).Should().Be(2);

        await _client.PostAsJsonAsync($"/api/v1/rulesets/{rulesetId}/versions/2/rules", Rule("ADDR.STRUCTURED_ONLY", "StructuredOnly", null, "Future"), Token);
        await _client.PostAsJsonAsync($"/api/v1/rulesets/{rulesetId}/versions/2/activate", new { effectiveFrom = "2026-11-15" }, Token);

        var afterCutover = await _client.GetFromJsonAsync<RulesetDto>($"/api/v1/rulesets/{rulesetId}", Json, Token);
        afterCutover!.ActiveVersionNumber.Should().Be(2);
        afterCutover.Versions.Single(version => version.VersionNumber == 1).Status.Should().Be(RulesetStatus.Retired);

        var rollback = await _client.PostAsJsonAsync(
            $"/api/v1/rulesets/{rulesetId}/versions/1/activate",
            new { effectiveFrom = "2026-12-01" },
            Token);
        rollback.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rolledBack = await _client.GetFromJsonAsync<RulesetDto>($"/api/v1/rulesets/{rulesetId}", Json, Token);
        rolledBack!.ActiveVersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task Effective_rules_follow_the_as_of_date()
    {
        var schemeCode = $"T{Random.Shared.Next(1000, 9999)}";
        var rulesetId = await CreateRulesetAsync(schemeCode);
        await _client.PostAsJsonAsync($"/api/v1/rulesets/{rulesetId}/versions/1/rules", Rule("ADDR.TOWN_REQUIRED"), Token);
        await _client.PostAsJsonAsync($"/api/v1/rulesets/{rulesetId}/versions/1/activate", new { effectiveFrom = "2026-01-01" }, Token);

        var beforeEffectiveDate = await _client.GetAsync(
            $"/api/v1/rulesets/effective?schemeCode={schemeCode}&asOf=2025-12-31", Token);
        beforeEffectiveDate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var onEffectiveDate = await _client.GetFromJsonAsync<EffectiveRulesetDto>(
            $"/api/v1/rulesets/effective?schemeCode={schemeCode}&asOf=2026-06-01", Json, Token);
        onEffectiveDate!.VersionNumber.Should().Be(1);
        onEffectiveDate.AsOf.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Fact]
    public async Task A_duplicate_scheme_code_is_rejected()
    {
        var code = $"T{Random.Shared.Next(1000, 9999)}";
        await CreateRulesetAsync(code);

        var duplicate = await _client.PostAsJsonAsync(
            "/api/v1/schemes",
            new { code, name = "Duplicate", description = (string?)null, structuredAddressMandatoryFrom = (string?)null },
            Token);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reference_countries_are_seeded()
    {
        var countries = await _client.GetFromJsonAsync<IReadOnlyList<CountryDto>>("/api/v1/countries", Json, Token);

        countries.Should().NotBeNull();
        countries.Should().Contain(country => country.Alpha2 == "DE" && country.IsSepa);
    }
}
