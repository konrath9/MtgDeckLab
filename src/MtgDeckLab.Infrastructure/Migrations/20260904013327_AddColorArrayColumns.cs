using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgDeckLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColorArrayColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dados existentes estão como JSON-as-text (ex.: "[0,3]"). Trocar colchete por chave
            // produz um literal de array do Postgres válido (ex.: "{0,3}"), permitindo o cast direto.
            migrationBuilder.Sql(
                """
                ALTER TABLE cards
                ALTER COLUMN colors TYPE integer[]
                USING replace(replace(colors, '[', '{'), ']', '}')::integer[];
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE cards
                ALTER COLUMN color_identity TYPE integer[]
                USING replace(replace(color_identity, '[', '{'), ']', '}')::integer[];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE cards
                ALTER COLUMN colors TYPE text
                USING array_to_json(colors)::text;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE cards
                ALTER COLUMN color_identity TYPE text
                USING array_to_json(color_identity)::text;
                """);
        }
    }
}
