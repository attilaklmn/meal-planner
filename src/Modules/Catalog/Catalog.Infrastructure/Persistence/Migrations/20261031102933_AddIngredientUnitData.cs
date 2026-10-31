using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientUnitData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DensityGramsPerMl",
                schema: "catalog",
                table: "Ingredients",
                type: "numeric(8,3)",
                precision: 8,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PieceWeightGrams",
                schema: "catalog",
                table: "Ingredients",
                type: "numeric(8,3)",
                precision: 8,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DensityGramsPerMl",
                schema: "catalog",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "PieceWeightGrams",
                schema: "catalog",
                table: "Ingredients");
        }
    }
}
