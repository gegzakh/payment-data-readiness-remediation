using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Rules.Application.Rulesets;
using PDR.Rules.Domain.Reference;
using PDR.Rules.Domain.Rulesets;
using PDR.Rules.Domain.Schemes;

namespace PDR.Rules.Infrastructure.Persistence;

/// <summary>
/// Seeds the reference data the rest of the platform depends on: the schemes in scope, ISO country
/// reference data, and an activated SEPA ruleset expressing today's checks plus the post-cutover
/// structured-address requirements.
/// </summary>
public sealed class RulesSeeder(RulesDbContext context, IClock clock) : IDataSeeder
{
    /// <summary>EPC date after which unstructured addresses are rejected for SEPA payments.</summary>
    private static readonly DateOnly SepaCutover = new(2026, 11, 15);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedSettingsAsync(cancellationToken);
        await SeedSchemesAsync(cancellationToken);
        await SeedCountriesAsync(cancellationToken);
        await SeedSepaRulesetAsync(cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (RulesSettingKeys.DefaultSchemeCode, "SEPA", "string", "Scheme assumed when a caller does not specify one."),
            (RulesSettingKeys.StructuredAddressCutoverDate, SepaCutover.ToString("yyyy-MM-dd"), "string",
                "Date the structured address requirement becomes mandatory for the default scheme.")
        };

        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSchemesAsync(CancellationToken cancellationToken)
    {
        if (await context.Schemes.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Schemes.AddRange(
            Scheme.Create("SEPA", "SEPA Credit Transfer / Direct Debit", "EPC schemes governed by the SEPA rulebooks.", SepaCutover),
            Scheme.Create("CBPR", "SWIFT CBPR+ cross-border payments", "ISO 20022 cross-border payments over SWIFT.", new DateOnly(2025, 11, 22)),
            Scheme.Create("DOMESTIC", "Domestic ACH", "Local clearing schemes without a structured-address mandate.", null));

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCountriesAsync(CancellationToken cancellationToken)
    {
        if (await context.Countries.AnyAsync(cancellationToken))
        {
            return;
        }

        var countries = new (string Alpha2, string Name, bool RequiresPostCode, bool IsSepa)[]
        {
            ("AT", "Austria", true, true),
            ("BE", "Belgium", true, true),
            ("CH", "Switzerland", true, true),
            ("DE", "Germany", true, true),
            ("DK", "Denmark", true, true),
            ("ES", "Spain", true, true),
            ("FI", "Finland", true, true),
            ("FR", "France", true, true),
            ("GB", "United Kingdom", true, true),
            ("IE", "Ireland", false, true),
            ("IT", "Italy", true, true),
            ("LU", "Luxembourg", true, true),
            ("NL", "Netherlands", true, true),
            ("NO", "Norway", true, true),
            ("PL", "Poland", true, true),
            ("PT", "Portugal", true, true),
            ("SE", "Sweden", true, true),
            ("US", "United States", true, false),
            ("CA", "Canada", true, false),
            ("AE", "United Arab Emirates", false, false),
            ("HK", "Hong Kong", false, false),
            ("SG", "Singapore", true, false)
        };

        foreach (var (alpha2, name, requiresPostCode, isSepa) in countries)
        {
            context.Countries.Add(Country.Create(alpha2, name, requiresPostCode, isSepa));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSepaRulesetAsync(CancellationToken cancellationToken)
    {
        if (await context.Rulesets.AnyAsync(ruleset => ruleset.SchemeCode == "SEPA", cancellationToken))
        {
            return;
        }

        var ruleset = Ruleset.Create("SEPA", "SEPA address validation", "Address rules applied to SEPA payment parties.");

        foreach (var rule in SepaRules())
        {
            ruleset.AddRule(
                1,
                rule.Code,
                rule.Field,
                rule.Kind,
                rule.Severity,
                rule.Applicability,
                rule.Message,
                rule.Parameter);
        }

        ruleset.Activate(1, new DateOnly(2024, 1, 1), "system", clock.UtcNow);

        context.Rulesets.Add(ruleset);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<RuleInput> SepaRules() =>
    [
        new("ADDR.COUNTRY_REQUIRED", "Country", RuleKind.Required, RuleSeverity.Error, RuleApplicability.Both,
            "Country is mandatory for every payment party address.", null),
        new("ADDR.COUNTRY_ISO", "Country", RuleKind.Pattern, RuleSeverity.Error, RuleApplicability.Both,
            "Country must be an ISO 3166-1 alpha-2 code.", "^[A-Z]{2}$"),
        new("ADDR.ADDRESS_LINE_LENGTH", "AddressLine", RuleKind.MaxLength, RuleSeverity.Error, RuleApplicability.Current,
            "An address line must not exceed 70 characters.", "70"),
        new("ADDR.TOWN_LENGTH", "TownName", RuleKind.MaxLength, RuleSeverity.Error, RuleApplicability.Both,
            "Town name must not exceed 35 characters.", "35"),
        new("ADDR.STREET_LENGTH", "StreetName", RuleKind.MaxLength, RuleSeverity.Error, RuleApplicability.Both,
            "Street name must not exceed 70 characters.", "70"),
        new("ADDR.POSTCODE_LENGTH", "PostCode", RuleKind.MaxLength, RuleSeverity.Error, RuleApplicability.Both,
            "Post code must not exceed 16 characters.", "16"),
        new("ADDR.STRUCTURED_ONLY", "AddressLine", RuleKind.StructuredOnly, RuleSeverity.Error, RuleApplicability.Future,
            "Unstructured address lines are rejected after the structured address cutover.", null),
        new("ADDR.TOWN_REQUIRED", "TownName", RuleKind.Required, RuleSeverity.Error, RuleApplicability.Future,
            "Town name is mandatory once structured addresses become mandatory.", null),
        new("ADDR.POSTCODE_REQUIRED", "PostCode", RuleKind.Required, RuleSeverity.Warning, RuleApplicability.Future,
            "Post code is expected for countries that operate a postal code system.", null),
        new("ADDR.NO_PO_BOX", "StreetName", RuleKind.Prohibited, RuleSeverity.Warning, RuleApplicability.Future,
            "PO box references are not accepted as a structured street name.", "PO BOX,P.O. BOX,POBOX,POSTBUS")
    ];
}
