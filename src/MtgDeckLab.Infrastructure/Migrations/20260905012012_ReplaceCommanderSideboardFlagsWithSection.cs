using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgDeckLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCommanderSideboardFlagsWithSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add "section" alongside the old flags first so we can backfill it from them
            // before they're dropped — no existing row can become Maybeboard (that state
            // didn't exist before), so this CASE is total and unambiguous.
            migrationBuilder.AddColumn<int>(
                name: "section",
                table: "deck_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "section",
                table: "deck_version_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE deck_entries SET section = CASE
                    WHEN is_commander THEN 2
                    WHEN is_sideboard THEN 1
                    ELSE 0
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE deck_version_entries SET section = CASE
                    WHEN is_commander THEN 2
                    WHEN is_sideboard THEN 1
                    ELSE 0
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_deck_entries_deck_id_card_id_is_commander_is_sideboard",
                table: "deck_entries");

            migrationBuilder.DropColumn(
                name: "is_commander",
                table: "deck_version_entries");

            migrationBuilder.DropColumn(
                name: "is_sideboard",
                table: "deck_version_entries");

            migrationBuilder.DropColumn(
                name: "is_commander",
                table: "deck_entries");

            migrationBuilder.DropColumn(
                name: "is_sideboard",
                table: "deck_entries");

            migrationBuilder.CreateIndex(
                name: "IX_deck_entries_deck_id_card_id_section",
                table: "deck_entries",
                columns: new[] { "deck_id", "card_id", "section" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deck_entries_deck_id_card_id_section",
                table: "deck_entries");

            migrationBuilder.DropColumn(
                name: "section",
                table: "deck_version_entries");

            migrationBuilder.DropColumn(
                name: "section",
                table: "deck_entries");

            migrationBuilder.AddColumn<bool>(
                name: "is_commander",
                table: "deck_version_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sideboard",
                table: "deck_version_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_commander",
                table: "deck_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sideboard",
                table: "deck_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_deck_entries_deck_id_card_id_is_commander_is_sideboard",
                table: "deck_entries",
                columns: new[] { "deck_id", "card_id", "is_commander", "is_sideboard" },
                unique: true);
        }
    }
}
