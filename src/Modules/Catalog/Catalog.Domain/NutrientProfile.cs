namespace Catalog.Domain;

/// <summary>
/// Energy and macronutrients of some amount of food (grams, kcal).
/// It does not know which amount: an ingredient keeps one per 100 g, a recipe sums them for its total.
/// </summary>
public sealed record NutrientProfile
{
    public static readonly NutrientProfile Zero = new(0, 0, 0, 0, 0);

    public NutrientProfile(decimal energyKcal, decimal protein, decimal carbohydrate, decimal fat, decimal fiber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(energyKcal);
        ArgumentOutOfRangeException.ThrowIfNegative(protein);
        ArgumentOutOfRangeException.ThrowIfNegative(carbohydrate);
        ArgumentOutOfRangeException.ThrowIfNegative(fat);
        ArgumentOutOfRangeException.ThrowIfNegative(fiber);

        EnergyKcal = energyKcal;
        Protein = protein;
        Carbohydrate = carbohydrate;
        Fat = fat;
        Fiber = fiber;
    }

    // Get-only (no init): a `with` expression would skip the constructor's validation.
    public decimal EnergyKcal { get; }

    public decimal Protein { get; }

    public decimal Carbohydrate { get; }

    public decimal Fat { get; }

    public decimal Fiber { get; }

    /// <summary>Protein, carbohydrate and fat together; fiber is left out because sources disagree on whether carbohydrate includes it.</summary>
    public decimal Macros => Protein + Carbohydrate + Fat;

    public NutrientProfile Scale(decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factor);

        return new NutrientProfile(
            EnergyKcal * factor,
            Protein * factor,
            Carbohydrate * factor,
            Fat * factor,
            Fiber * factor);
    }

    public static NutrientProfile operator +(NutrientProfile left, NutrientProfile right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new NutrientProfile(
            left.EnergyKcal + right.EnergyKcal,
            left.Protein + right.Protein,
            left.Carbohydrate + right.Carbohydrate,
            left.Fat + right.Fat,
            left.Fiber + right.Fiber);
    }

    // CA2225: operators should have a named alternative for languages without operator overloading.
    public static NutrientProfile Add(NutrientProfile left, NutrientProfile right) => left + right;
}
