using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "recipes");

            migrationBuilder.CreateTable(
                name: "Ingredients",
                schema: "recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HungarianName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NutrientsPer100g_Carbohydrate = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_EnergyKcal = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_Fat = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_Fiber = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    NutrientsPer100g_Protein = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    DensityGramsPerMl = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    PieceWeightGrams = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                schema: "recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Steps = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                schema: "recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity_Amount = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Quantity_Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalSchema: "recipes",
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "recipes",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                schema: "recipes",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId_Position",
                schema: "recipes",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Tags",
                schema: "recipes",
                table: "Recipes",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients",
                schema: "recipes");

            migrationBuilder.DropTable(
                name: "Ingredients",
                schema: "recipes");

            migrationBuilder.DropTable(
                name: "Recipes",
                schema: "recipes");
        }
    }
}
