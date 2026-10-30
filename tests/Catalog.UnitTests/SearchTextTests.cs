using Catalog.Domain;

namespace Catalog.UnitTests;

public class SearchTextTests
{
    [Theory]
    [InlineData("Tojás", "tojas")]
    [InlineData("TOJÁS", "tojas")]
    [InlineData("Őszibarack", "oszibarack")]
    [InlineData("Főtt tojás", "fott tojas")]
    [InlineData("Teljes kiőrlésű búzaliszt", "teljes kiorlesu buzaliszt")]
    [InlineData("Jalapeño", "jalapeno")]
    public void Lowercases_and_removes_accents(string text, string expected)
    {
        Assert.Equal(expected, SearchText.Normalize(text));
    }

    [Theory]
    [InlineData("Csirkemell (nyers)", "csirkemell nyers")]
    [InlineData("Egg, whole, raw, fresh", "egg whole raw fresh")]
    [InlineData("  Teljes   tej (3,5%) ", "teljes tej 3 5")]
    [InlineData("Beef, trimmed to 1/8\" fat", "beef trimmed to 1 8 fat")]
    public void Turns_punctuation_and_extra_spaces_into_single_spaces(string text, string expected)
    {
        Assert.Equal(expected, SearchText.Normalize(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("%_,()")]
    public void Leaves_nothing_of_text_without_letters_or_digits(string text)
    {
        Assert.Equal("", SearchText.Normalize(text));
    }

    [Fact]
    public void Ingredient_search_text_starts_with_the_Hungarian_name()
    {
        var egg = Ingredient.Create("Egg, whole, raw", "Tojás", NutrientProfile.Zero, "usda", "1");

        Assert.Equal("tojas egg whole raw", egg.SearchText);
    }

    [Fact]
    public void Ingredient_search_text_follows_name_changes()
    {
        var egg = Ingredient.Create("Egg", "Tojás", NutrientProfile.Zero, "usda", "1");

        egg.UpdateFromSource("Egg, whole", "Tyúktojás", NutrientProfile.Zero);

        Assert.Equal("tyuktojas egg whole", egg.SearchText);
    }

    [Fact]
    public void Ingredient_search_text_fits_its_column()
    {
        var longName = new string('a', Ingredient.NameMaxLength);

        var ingredient = Ingredient.Create(longName, longName, NutrientProfile.Zero, "usda", "1");

        Assert.True(ingredient.SearchText.Length <= Ingredient.SearchTextMaxLength);
    }
}
