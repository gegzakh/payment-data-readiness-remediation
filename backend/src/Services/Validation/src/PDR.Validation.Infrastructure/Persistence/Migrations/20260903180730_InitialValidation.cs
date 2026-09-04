using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDR.Validation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "validation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scheme_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    current_ruleset_version = table.Column<int>(type: "integer", nullable: true),
                    future_ruleset_version = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error_summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    input_record_count = table.Column<int>(type: "integer", nullable: false),
                    assessed_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_count = table.Column<int>(type: "integer", nullable: false),
                    unable_to_assess_count = table.Column<int>(type: "integer", nullable: false),
                    current_compliant_count = table.Column<int>(type: "integer", nullable: false),
                    current_rejected_count = table.Column<int>(type: "integer", nullable: false),
                    current_warning_count = table.Column<int>(type: "integer", nullable: false),
                    future_compliant_count = table.Column<int>(type: "integer", nullable: false),
                    future_rejected_count = table.Column<int>(type: "integer", nullable: false),
                    future_warning_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_validation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "address_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    end_to_end_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    party_role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    party_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    country = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    town_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    post_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    street_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    building_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    address_lines = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    scheme_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_duplicate = table.Column<bool>(type: "boolean", nullable: false),
                    classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    future_outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    evidence_pointer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_address_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_address_assessments_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "validation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "validation_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    rule_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    field = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expected = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    actual = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_validation_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_validation_issues_address_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "address_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_assessments_batch_id",
                table: "address_assessments",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_address_assessments_run_id_sequence",
                table: "address_assessments",
                columns: new[] { "run_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc_occurred_at_utc",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_validation_issues_assessment_id",
                table: "validation_issues",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_validation_issues_rule_code_mode",
                table: "validation_issues",
                columns: new[] { "rule_code", "mode" });

            migrationBuilder.CreateIndex(
                name: "ix_validation_runs_batch_id",
                table: "validation_runs",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_validation_runs_started_at_utc",
                table: "validation_runs",
                column: "started_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "validation_issues");

            migrationBuilder.DropTable(
                name: "address_assessments");

            migrationBuilder.DropTable(
                name: "validation_runs");
        }
    }
}
