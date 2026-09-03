using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDR.Simulation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cutover_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    cutover_date = table.Column<DateOnly>(type: "date", nullable: false),
                    owner = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    freeze_from = table.Column<DateOnly>(type: "date", nullable: true),
                    freeze_to = table.Column<DateOnly>(type: "date", nullable: true),
                    fallback_plan = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    support_model = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cutover_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    scheme_codes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    source_codes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    countries = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    party_roles = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    exclusions = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ruleset_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    run_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requested_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    population_count = table.Column<int>(type: "integer", nullable: false),
                    assessed_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_count = table.Column<int>(type: "integer", nullable: false),
                    unable_to_assess_count = table.Column<int>(type: "integer", nullable: false),
                    rejected_count = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    payments_at_risk = table.Column<int>(type: "integer", nullable: false),
                    readiness_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ruleset_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    value_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    owner = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    scope = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cutover_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    approver = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    decision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    rationale = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    recommendation_at_sign_off = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cutover_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_cutover_approvals_cutover_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "cutover_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cutover_criteria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    owner = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    rationale = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    recorded_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cutover_criteria", x => x.id);
                    table.ForeignKey(
                        name: "fk_cutover_criteria_cutover_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "cutover_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_breakdown",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dimension = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    key = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    rejected_count = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    payments_at_risk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_breakdown", x => x.id);
                    table.ForeignKey(
                        name: "fk_simulation_breakdown_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    risk = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scenario_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    sample_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    expected_result = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actual_result = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    evidence_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    defect_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    executed_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    executed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    execution_count = table.Column<int>(type: "integer", nullable: false),
                    uat_outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    engine_outcome = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    platform_outcome = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    uat_explanation = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    reconciled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_cases_test_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "test_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cutover_approvals_plan_id_role",
                table: "cutover_approvals",
                columns: new[] { "plan_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cutover_criteria_plan_id_reference",
                table: "cutover_criteria",
                columns: new[] { "plan_id", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cutover_plans_code",
                table: "cutover_plans",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc_occurred_at_utc",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_scenarios_code",
                table: "scenarios",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_simulation_breakdown_run_id_dimension",
                table: "simulation_breakdown",
                columns: new[] { "run_id", "dimension" });

            migrationBuilder.CreateIndex(
                name: "ix_simulation_runs_run_key",
                table: "simulation_runs",
                column: "run_key");

            migrationBuilder.CreateIndex(
                name: "ix_simulation_runs_scenario_id",
                table: "simulation_runs",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_cases_plan_id_reference",
                table: "test_cases",
                columns: new[] { "plan_id", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_plans_code",
                table: "test_plans",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cutover_approvals");

            migrationBuilder.DropTable(
                name: "cutover_criteria");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "scenarios");

            migrationBuilder.DropTable(
                name: "simulation_breakdown");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "test_cases");

            migrationBuilder.DropTable(
                name: "cutover_plans");

            migrationBuilder.DropTable(
                name: "simulation_runs");

            migrationBuilder.DropTable(
                name: "test_plans");
        }
    }
}
