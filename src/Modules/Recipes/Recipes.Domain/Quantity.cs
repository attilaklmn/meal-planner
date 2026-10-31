namespace Recipes.Domain;

/// <summary>
/// An amount of an ingredient in a unit, as written in a recipe ("2 tbsp", "3 pieces", "250 g").
/// It keeps the unit it was given, so equality is by representation: 1 kg and 1000 g are different values.
/// </summary>
public sealed record Quantity
{
    public Quantity(decimal amount, Unit unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (!Enum.IsDefined(unit))
        {
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit.");
        }

        Amount = amount;
        Unit = unit;
    }

    // Get-only (no init): a `with` expression would skip the constructor's validation.
    public decimal Amount { get; }

    public Unit Unit { get; }

    public UnitDimension Dimension => Unit.Dimension();

    /// <summary>Whether <see cref="ToGrams"/> works with this ingredient's unit data.</summary>
    public bool CanConvertToGrams(UnitConversion conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);

        return conversion.GramsPerBaseUnit(Dimension) is not null;
    }

    /// <summary>
    /// The weight of this quantity. Volumes need the ingredient's density, pieces its piece weight;
    /// when that is missing this throws instead of returning 0, so a recipe's nutrition is never silently too low.
    /// </summary>
    public decimal ToGrams(UnitConversion conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);

        var gramsPerBaseUnit = conversion.GramsPerBaseUnit(Dimension)
            ?? throw new InvalidOperationException(Dimension == UnitDimension.Volume
                ? $"Cannot convert {Amount} {Unit} to grams: the ingredient has no density."
                : $"Cannot convert {Amount} {Unit} to grams: the ingredient has no piece weight.");

        return Amount * Unit.BaseFactor() * gramsPerBaseUnit;
    }

    /// <summary>For changing servings: 2 tbsp for 4 servings is 1 tbsp for 2.</summary>
    public Quantity Scale(decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);

        return new Quantity(Amount * factor, Unit);
    }

    /// <summary>
    /// Sums quantities of the same dimension. The same unit stays (1 cup + 1 cup = 2 cups); mixed units give
    /// the dimension's base unit (1 tbsp + 100 ml = 115 ml). Different dimensions need ingredient data, so they throw.
    /// </summary>
    public static Quantity operator +(Quantity left, Quantity right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Unit == right.Unit)
        {
            return new Quantity(left.Amount + right.Amount, left.Unit);
        }

        if (left.Dimension != right.Dimension)
        {
            throw new InvalidOperationException(
                $"Cannot add {right.Amount} {right.Unit} to {left.Amount} {left.Unit}: convert both to grams first.");
        }

        var baseAmount = (left.Amount * left.Unit.BaseFactor()) + (right.Amount * right.Unit.BaseFactor());
        return new Quantity(baseAmount, left.Dimension.BaseUnit());
    }

    // CA2225: operators should have a named alternative for languages without operator overloading.
    public static Quantity Add(Quantity left, Quantity right) => left + right;
}
