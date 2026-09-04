using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgDeckLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deck_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    grade = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deck_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_deck_versions_decks_deck_id",
                        column: x => x.deck_id,
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deck_version_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    is_commander = table.Column<bool>(type: "boolean", nullable: false),
                    is_sideboard = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deck_version_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_deck_version_entries_deck_versions_deck_version_id",
                        column: x => x.deck_version_id,
                        principalTable: "deck_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deck_version_entries_deck_version_id",
                table: "deck_version_entries",
                column: "deck_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_deck_versions_deck_id_version_number",
                table: "deck_versions",
                columns: new[] { "deck_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deck_version_entries");

            migrationBuilder.DropTable(
                name: "deck_versions");
        }
    }
}
