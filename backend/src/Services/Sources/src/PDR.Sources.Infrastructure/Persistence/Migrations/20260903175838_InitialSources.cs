using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDR.Sources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSources : Migration
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
                name: "source_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    @interface = table.Column<string>(name: "interface", type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    legal_entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scheme_codes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    schedule = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    estimated_party_count = table.Column<long>(type: "bigint", nullable: false),
                    recurring_instruction_count = table.Column<long>(type: "bigint", nullable: false),
                    is_authoritative = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mapping = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scan_coverage_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    last_scan_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attested_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    remediation_owner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_systems", x => x.id);
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
                name: "field_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_attribute = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_element = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    transformation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_authoritative = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    last_reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_field_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_field_mappings_source_systems_source_system_id",
                        column: x => x.source_system_id,
                        principalTable: "source_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lineage_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    from_node = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    to_node = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lineage_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_lineage_steps_source_systems_source_system_id",
                        column: x => x.source_system_id,
                        principalTable: "source_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_field_mappings_source_system_id_source_attribute_target_ele",
                table: "field_mappings",
                columns: new[] { "source_system_id", "source_attribute", "target_element" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lineage_steps_source_system_id_sequence",
                table: "lineage_steps",
                columns: new[] { "source_system_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc_occurred_at_utc",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_source_systems_code",
                table: "source_systems",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_source_systems_legal_entity",
                table: "source_systems",
                column: "legal_entity");

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "field_mappings");

            migrationBuilder.DropTable(
                name: "lineage_steps");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "source_systems");
        }
    }
}
