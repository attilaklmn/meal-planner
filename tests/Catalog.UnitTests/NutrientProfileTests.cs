using Catalog.Domain;

namespace Catalog.UnitTests;

public class NutrientProfileTests
{
    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(0, -1, 0, 0, 0)]
    [InlineData(0, 0, -1, 0, 0)]
    [InlineData(0, 0, 0, -1, 0)]
    [InlineData(0, 0, 0, 0, -1)]
    public void Rejects_negative_values(int energyKcal, int protein, int carbohydrate, int fat, int fiber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutrientProfile(energyKcal, protein, carbohydrate, fat, fiber));
    }

    [Fact]
    public void Scale_multiplies_every_value()
    {
        var chickenBreastPer100g = new NutrientProfile(120m, 22.5m, 0m, 2.6m, 0m);

        var for250g = chickenBreastPer100g.Scale(2.5m);

        Assert.Equal(new NutrientProfile(300m, 56.25m, 0m, 6.5m, 0m), for250g);
    }

    [Fact]
    public void Scale_by_zero_gives_zero()
    {
        var profile = new NutrientProfile(120m, 22.5m, 0m, 2.6m, 0m);

        Assert.Equal(NutrientProfile.Zero, profile.Scale(0));
    }

    [Fact]
    public void Scale_rejects_negative_factor()
    {
        var profile = new NutrientProfile(120m, 22.5m, 0m, 2.6m, 0m);

        Assert.Throws<ArgumentOutOfRangeException>(() => profile.Scale(-0.5m));
    }

    [Fact]
    public void Adding_sums_every_value()
    {
        var rice = new NutrientProfile(130m, 2.7m, 28.2m, 0.3m, 0.4m);
        var chicken = new NutrientProfile(120m, 22.5m, 0m, 2.6m, 0m);

        Assert.Equal(new NutrientProfile(250m, 25.2m, 28.2m, 2.9m, 0.4m), rice + chicken);
    }

    [Fact]
    public void Zero_is_the_identity_of_addition()
    {
        var rice = new NutrientProfile(130m, 2.7m, 28.2m, 0.3m, 0.4m);

        Assert.Equal(rice, rice + NutrientProfile.Zero);
    }

    [Fact]
    public void Profiles_with_the_same_values_are_equal()
    {
        Assert.Equal(new NutrientProfile(130m, 2.7m, 28.2m, 0.3m, 0.4m), new NutrientProfile(130m, 2.7m, 28.2m, 0.3m, 0.4m));
    }

    [Fact]
    public void Macros_leave_out_fiber()
    {
        var oats = new NutrientProfile(379m, 13.2m, 67.7m, 6.5m, 10.1m);

        Assert.Equal(87.4m, oats.Macros);
    }
}
