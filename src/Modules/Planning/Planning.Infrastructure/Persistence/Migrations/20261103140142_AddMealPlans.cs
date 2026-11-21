using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "planning");

            migrationBuilder.CreateTable(
                name: "MealPlans",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanEntries",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Meal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipe_EnergyKcalPerServing = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Recipe_RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipe_Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanEntries_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalSchema: "planning",
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanEntries_MealPlanId",
                schema: "planning",
                table: "MealPlanEntries",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_WeekStart",
                schema: "planning",
                table: "MealPlans",
                column: "WeekStart",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealPlanEntries",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "MealPlans",
                schema: "planning");
        }
    }
}
