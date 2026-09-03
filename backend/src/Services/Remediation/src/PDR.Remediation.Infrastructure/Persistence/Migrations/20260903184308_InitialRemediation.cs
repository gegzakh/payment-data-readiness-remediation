using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDR.Remediation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRemediation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    audience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assignee = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    case_count = table.Column<int>(type: "integer", nullable: false),
                    remediated_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
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
                name: "remediation_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    owner_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    party_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    party_role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    original_country = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    original_town_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    original_post_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    original_street_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    original_building_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    original_address_lines = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    issue_rule_codes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    affected_schemes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    evidence_pointer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    occurrences = table.Column<int>(type: "integer", nullable: false),
                    future_exposure = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    priority_score = table.Column<int>(type: "integer", nullable: false),
                    queue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    assigned_to = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    submitted_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_rationale = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    exception_expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    remediated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_remediation_cases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulated_source_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    record_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulated_source_records", x => x.id);
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
                name: "writeback_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    applied_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    export_checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_writeback_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "writeback_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    writable_fields = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    export_format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    maintenance_window = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    max_records_per_run = table.Column<int>(type: "integer", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    rollback_method = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_writeback_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "case_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    from_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    to_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    rationale = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_events_remediation_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "remediation_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "case_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    captured_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_evidence_remediation_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "remediation_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "case_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    country = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    town_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    post_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    street_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    building_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    country_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    town_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    post_code_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    street_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    building_number_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    overall_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ambiguity = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    alternatives = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_proposals", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_proposals_remediation_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "remediation_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "writeback_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    before_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    after_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    applied_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_writeback_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_writeback_items_writeback_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "writeback_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_code",
                table: "campaigns",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_case_events_case_id_occurred_at_utc",
                table: "case_events",
                columns: new[] { "case_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_case_evidence_case_id",
                table: "case_evidence",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_case_proposals_case_id",
                table: "case_proposals",
                column: "case_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc_occurred_at_utc",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_remediation_cases_campaign_id",
                table: "remediation_cases",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_remediation_cases_case_key",
                table: "remediation_cases",
                column: "case_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_remediation_cases_source_code",
                table: "remediation_cases",
                column: "source_code");

            migrationBuilder.CreateIndex(
                name: "ix_remediation_cases_status_priority",
                table: "remediation_cases",
                columns: new[] { "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_simulated_source_records_source_code_record_reference",
                table: "simulated_source_records",
                columns: new[] { "source_code", "record_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_writeback_items_job_id_case_id",
                table: "writeback_items",
                columns: new[] { "job_id", "case_id" });

            migrationBuilder.CreateIndex(
                name: "ix_writeback_jobs_idempotency_key",
                table: "writeback_jobs",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_writeback_jobs_requested_at_utc",
                table: "writeback_jobs",
                column: "requested_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_writeback_targets_source_code",
                table: "writeback_targets",
                column: "source_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropTable(
                name: "case_events");

            migrationBuilder.DropTable(
                name: "case_evidence");

            migrationBuilder.DropTable(
                name: "case_proposals");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "simulated_source_records");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "writeback_items");

            migrationBuilder.DropTable(
                name: "writeback_targets");

            migrationBuilder.DropTable(
                name: "remediation_cases");

            migrationBuilder.DropTable(
                name: "writeback_jobs");
        }
    }
}
