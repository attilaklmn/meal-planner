import type { NutritionResponse, Unit } from '@/api/generated/model'
import { toGrams, type ConvertibleIngredient } from './units'

export type PreviewLine = {
  ingredient: ConvertibleIngredient | null
  amount: number
  unit: Unit
}

const zero: NutritionResponse = { energyKcal: 0, protein: 0, carbohydrate: 0, fat: 0, fiber: 0 }

/**
 * Nutrition of the recipe as it's being edited: the same sum as Recipe.CalculateNutrition on the server.
 * Lines that aren't complete yet (no ingredient, no valid amount, unit without data) count as nothing,
 * so the preview never breaks while typing.
 */
export function previewNutrition(lines: PreviewLine[], servings: number) {
  const total = lines.reduce((sum, line) => {
    if (!line.ingredient || !(line.amount > 0)) return sum

    const grams = toGrams(line.amount, line.unit, line.ingredient)
    if (grams === null) return sum

    const per100g = line.ingredient.nutrientsPer100g
    const factor = grams / 100
    return {
      energyKcal: sum.energyKcal + per100g.energyKcal * factor,
      protein: sum.protein + per100g.protein * factor,
      carbohydrate: sum.carbohydrate + per100g.carbohydrate * factor,
      fat: sum.fat + per100g.fat * factor,
      fiber: sum.fiber + per100g.fiber * factor,
    }
  }, zero)

  const validServings = Number.isInteger(servings) && servings > 0 ? servings : 1
  const perServing: NutritionResponse = {
    energyKcal: total.energyKcal / validServings,
    protein: total.protein / validServings,
    carbohydrate: total.carbohydrate / validServings,
    fat: total.fat / validServings,
    fiber: total.fiber / validServings,
  }

  return { total, perServing }
}
