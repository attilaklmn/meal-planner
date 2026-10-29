using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "Ingredients",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HungarianName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NutrientsPer100g_Carbohydrate = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_EnergyKcal = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_Fat = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_Fiber = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_Protein = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Source_SourceId",
                schema: "catalog",
                table: "Ingredients",
                columns: new[] { "Source", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ingredients",
                schema: "catalog");
        }
    }
}
