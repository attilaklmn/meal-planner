using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                schema: "catalog",
                table: "Ingredients",
                type: "character varying(401)",
                maxLength: 401,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_SearchText",
                schema: "catalog",
                table: "Ingredients",
                column: "SearchText")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ingredients_SearchText",
                schema: "catalog",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "SearchText",
                schema: "catalog",
                table: "Ingredients");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
