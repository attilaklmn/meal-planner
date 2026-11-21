using Planning.Domain;

namespace Planning.UnitTests;

public class MealPlanTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);

    private static readonly RecipeSnapshot Gulyas = new(Guid.CreateVersion7(), "Gulyásleves", 420m);

    private static readonly RecipeSnapshot Nokedli = new(Guid.CreateVersion7(), "Nokedli", 311.5m);

    [Fact]
    public void A_week_starts_on_a_monday()
    {
        var plan = MealPlan.Create(Monday);

        Assert.Equal(Monday, plan.WeekStart);
        Assert.Empty(plan.Entries);
    }

    [Theory]
    [InlineData(2026, 11, 3)]
    [InlineData(2026, 11, 8)]
    public void Any_other_day_is_rejected_as_a_week_start(int year, int month, int day)
    {
        Assert.Throws<ArgumentException>(() => MealPlan.Create(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2026, 11, 2)]
    [InlineData(2026, 11, 4)]
    [InlineData(2026, 11, 8)]
    public void Every_day_of_a_week_belongs_to_its_monday(int year, int month, int day)
    {
        Assert.Equal(Monday, MealPlan.WeekStartOf(new DateOnly(year, month, day)));
    }

    [Fact]
    public void Sunday_is_the_last_day_of_the_week()
    {
        var plan = MealPlan.Create(Monday);

        Assert.Equal(new DateOnly(2026, 11, 8), plan.DateOf(DayOfWeek.Sunday));
        Assert.Equal(Monday, plan.DateOf(DayOfWeek.Monday));
    }

    [Fact]
    public void A_meal_can_have_more_than_one_recipe()
    {
        var plan = MealPlan.Create(Monday);

        plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Gulyas, 2);
        plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Nokedli, 2);

        Assert.Equal(["Gulyásleves", "Nokedli"], plan.Entries.Select(e => e.Recipe.Title));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MealPlan.MaxServings + 1)]
    public void Servings_out_of_range_are_rejected(int servings)
    {
        var plan = MealPlan.Create(Monday);

        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AddEntry(DayOfWeek.Monday, Meal.Dinner, Gulyas, servings));
    }

    [Fact]
    public void An_unknown_meal_is_rejected()
    {
        var plan = MealPlan.Create(Monday);

        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AddEntry(DayOfWeek.Monday, (Meal)42, Gulyas, 1));
    }

    [Fact]
    public void Daily_energy_is_the_sum_of_that_days_entries_times_servings()
    {
        var plan = MealPlan.Create(Monday);
        plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Gulyas, 2);
        plan.AddEntry(DayOfWeek.Wednesday, Meal.Dinner, Nokedli, 1);
        plan.AddEntry(DayOfWeek.Thursday, Meal.Lunch, Gulyas, 1);

        Assert.Equal(1151.5m, plan.EnergyKcalOn(DayOfWeek.Wednesday));
        Assert.Equal(420m, plan.EnergyKcalOn(DayOfWeek.Thursday));
        Assert.Equal(0m, plan.EnergyKcalOn(DayOfWeek.Friday));
        Assert.Equal(1571.5m, plan.WeeklyEnergyKcal);
    }

    [Fact]
    public void Changing_an_entry_moves_it_and_sets_its_servings()
    {
        var plan = MealPlan.Create(Monday);
        var entry = plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Gulyas, 2);

        plan.ChangeEntry(entry.Id, DayOfWeek.Friday, Meal.Dinner, 3);

        Assert.Equal((DayOfWeek.Friday, Meal.Dinner, 3), (entry.Day, entry.Meal, entry.Servings));
        Assert.Equal(1260m, plan.EnergyKcalOn(DayOfWeek.Friday));
        Assert.Equal(0m, plan.EnergyKcalOn(DayOfWeek.Wednesday));
    }

    [Fact]
    public void An_invalid_change_leaves_the_entry_untouched()
    {
        var plan = MealPlan.Create(Monday);
        var entry = plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Gulyas, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => plan.ChangeEntry(entry.Id, DayOfWeek.Friday, Meal.Dinner, 0));

        Assert.Equal((DayOfWeek.Wednesday, Meal.Lunch, 2), (entry.Day, entry.Meal, entry.Servings));
    }

    [Fact]
    public void Removing_an_entry_takes_it_off_the_plan()
    {
        var plan = MealPlan.Create(Monday);
        var soup = plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Gulyas, 2);
        plan.AddEntry(DayOfWeek.Wednesday, Meal.Lunch, Nokedli, 2);

        plan.RemoveEntry(soup.Id);

        Assert.Equal(["Nokedli"], plan.Entries.Select(e => e.Recipe.Title));
    }

    [Fact]
    public void An_unknown_entry_is_reported()
    {
        var plan = MealPlan.Create(Monday);

        Assert.Throws<ArgumentException>(() => plan.RemoveEntry(Guid.CreateVersion7()));
        Assert.Throws<ArgumentException>(() => plan.ChangeEntry(Guid.CreateVersion7(), DayOfWeek.Monday, Meal.Lunch, 1));
    }

    [Fact]
    public void Refreshing_a_recipe_updates_every_entry_that_plans_it()
    {
        var plan = MealPlan.Create(Monday);
        plan.AddEntry(DayOfWeek.Monday, Meal.Lunch, Gulyas, 1);
        plan.AddEntry(DayOfWeek.Friday, Meal.Dinner, Gulyas, 2);
        plan.AddEntry(DayOfWeek.Friday, Meal.Lunch, Nokedli, 1);

        plan.RefreshRecipe(new RecipeSnapshot(Gulyas.RecipeId, "Gulyásleves (könnyített)", 350m));

        Assert.Equal(
            ["Gulyásleves (könnyített)", "Gulyásleves (könnyített)", "Nokedli"],
            plan.Entries.Select(e => e.Recipe.Title));
        Assert.Equal(1011.5m, plan.EnergyKcalOn(DayOfWeek.Friday));
    }

    [Fact]
    public void A_recipe_snapshot_needs_an_id_and_a_title()
    {
        Assert.Throws<ArgumentException>(() => new RecipeSnapshot(Guid.Empty, "Gulyásleves", 420m));
        Assert.ThrowsAny<ArgumentException>(() => new RecipeSnapshot(Guid.CreateVersion7(), " ", 420m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecipeSnapshot(Guid.CreateVersion7(), "Gulyásleves", -1m));
    }
}
