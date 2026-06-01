using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgDeckLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scryfall_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    mana_cost = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    cmc = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    type_line = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    oracle_text = table.Column<string>(type: "text", nullable: true),
                    power = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    toughness = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    loyalty = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    price_usd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    price_usd_foil = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    set_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    color_identity = table.Column<string>(type: "text", nullable: false),
                    colors = table.Column<string>(type: "text", nullable: false),
                    subtypes = table.Column<string>(type: "text", nullable: false),
                    supertypes = table.Column<string>(type: "text", nullable: false),
                    types = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "decks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    format = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "finance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_cost_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_finance_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deck_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    is_commander = table.Column<bool>(type: "boolean", nullable: false),
                    is_sideboard = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deck_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_deck_entries_decks_deck_id",
                        column: x => x.deck_id,
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cards_scryfall_id",
                table: "cards",
                column: "scryfall_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deck_entries_deck_id_card_id_is_commander_is_sideboard",
                table: "deck_entries",
                columns: new[] { "deck_id", "card_id", "is_commander", "is_sideboard" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_finance_snapshots_deck_id",
                table: "finance_snapshots",
                column: "deck_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "deck_entries");

            migrationBuilder.DropTable(
                name: "finance_snapshots");

            migrationBuilder.DropTable(
                name: "decks");
        }
    }
}
