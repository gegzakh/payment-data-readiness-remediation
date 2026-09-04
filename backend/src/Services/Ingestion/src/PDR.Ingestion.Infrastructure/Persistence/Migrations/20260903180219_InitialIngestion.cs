using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDR.Ingestion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingestion_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    parser_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    submitted_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_reprocess = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quarantine_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    error_summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    parsed_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    duplicate_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_count = table.Column<int>(type: "integer", nullable: false),
                    checkpoint = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingestion_batches", x => x.id);
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
                name: "batch_payloads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batch_payloads", x => x.id);
                    table.ForeignKey(
                        name: "fk_batch_payloads_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "ingestion_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "party_address_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_duplicate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_address_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_party_address_records_ingestion_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "ingestion_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_batch_payloads_batch_id",
                table: "batch_payloads",
                column: "batch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_batches_idempotency_key",
                table: "ingestion_batches",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_batches_received_at_utc",
                table: "ingestion_batches",
                column: "received_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_batches_source_code_checksum",
                table: "ingestion_batches",
                columns: new[] { "source_code", "checksum" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc_occurred_at_utc",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_party_address_records_batch_id_sequence",
                table: "party_address_records",
                columns: new[] { "batch_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_party_address_records_content_hash",
                table: "party_address_records",
                column: "content_hash");

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
                name: "batch_payloads");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "party_address_records");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "ingestion_batches");
        }
    }
}
