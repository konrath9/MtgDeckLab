using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgDeckLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "oracle_id",
                table: "cards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "card_localized_names",
                columns: table => new
                {
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    printed_type_line = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_localized_names", x => new { x.card_id, x.language });
                    table.ForeignKey(
                        name: "FK_card_localized_names_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cards_oracle_id",
                table: "cards",
                column: "oracle_id");

            migrationBuilder.CreateIndex(
                name: "IX_card_localized_names_name",
                table: "card_localized_names",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_localized_names");

            migrationBuilder.DropIndex(
                name: "IX_cards_oracle_id",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "oracle_id",
                table: "cards");
        }
    }
}
