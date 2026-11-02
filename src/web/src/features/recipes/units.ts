import { Unit, type NutritionResponse } from '@/api/generated/model'

// The browser-side copy of Recipes.Domain's unit table (Unit.cs), for the editor's live preview and unit checks.
// The server recalculates on save and its numbers are the ones stored; keep the two in step when units change.

type Dimension = 'mass' | 'volume' | 'count'

const unitInfo: Record<Unit, { dimension: Dimension; baseFactor: number; label: string }> = {
  Gram: { dimension: 'mass', baseFactor: 1, label: 'g' },
  Kilogram: { dimension: 'mass', baseFactor: 1000, label: 'kg' },
  Milliliter: { dimension: 'volume', baseFactor: 1, label: 'ml' },
  Liter: { dimension: 'volume', baseFactor: 1000, label: 'l' },
  // Metric kitchen measures, as on the server: 5 ml, 15 ml, 250 ml.
  Teaspoon: { dimension: 'volume', baseFactor: 5, label: 'tsp' },
  Tablespoon: { dimension: 'volume', baseFactor: 15, label: 'tbsp' },
  Cup: { dimension: 'volume', baseFactor: 250, label: 'cup' },
  Piece: { dimension: 'count', baseFactor: 1, label: 'piece' },
}

export const units = Object.values(Unit)

export function unitLabel(unit: Unit): string {
  return unitInfo[unit].label
}

/** What an ingredient needs for unit conversion; both a catalog search result and a recipe line have it. */
export type ConvertibleIngredient = {
  nutrientsPer100g: NutritionResponse
  densityGramsPerMl: number | null
  pieceWeightGrams: number | null
}

function gramsPerBaseUnit(ingredient: ConvertibleIngredient, unit: Unit): number | null {
  switch (unitInfo[unit].dimension) {
    case 'mass':
      return 1
    case 'volume':
      return ingredient.densityGramsPerMl
    case 'count':
      return ingredient.pieceWeightGrams
  }
}

/** Volumes need a density, pieces a piece weight; mass always works. */
export function canMeasure(ingredient: ConvertibleIngredient, unit: Unit): boolean {
  return gramsPerBaseUnit(ingredient, unit) !== null
}

/** Why a unit can't be used for an ingredient, for the disabled option's label. */
export function missingDataFor(unit: Unit): string {
  return unitInfo[unit].dimension === 'volume' ? 'no density' : 'no piece weight'
}

export function toGrams(
  amount: number,
  unit: Unit,
  ingredient: ConvertibleIngredient,
): number | null {
  const perBaseUnit = gramsPerBaseUnit(ingredient, unit)
  return perBaseUnit === null ? null : amount * unitInfo[unit].baseFactor * perBaseUnit
}
