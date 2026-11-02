import type { IngredientSummary, RecipeDetails, RecipeSummary } from '@/api/generated/model'

// Shared test data for the recipe pages and the editor; values as the API returns them.

export const flour: IngredientSummary = {
  id: '01a193ca-bd95-7684-8dec-6e3ddc8572cf',
  name: 'Wheat flour, white, all-purpose, enriched, bleached',
  hungarianName: 'Búzaliszt (finomliszt)',
  nutrientsPer100g: { energyKcal: 364, protein: 10.33, carbohydrate: 76.31, fat: 0.98, fiber: 2.7 },
  densityGramsPerMl: 0.528,
  pieceWeightGrams: null,
}

export const oliveOil: IngredientSummary = {
  id: '01a193ca-bd90-7555-9988-5770661bc687',
  name: 'Oil, olive, salad or cooking',
  hungarianName: 'Olívaolaj',
  nutrientsPer100g: { energyKcal: 884, protein: 0, carbohydrate: 0, fat: 100, fiber: 0 },
  densityGramsPerMl: 0.913,
  pieceWeightGrams: null,
}

export const egg: IngredientSummary = {
  id: '01a193ca-bd1e-752b-9ed4-cea2b6f90f11',
  name: 'Egg, whole, raw, fresh',
  hungarianName: 'Tojás',
  nutrientsPer100g: { energyKcal: 143, protein: 12.56, carbohydrate: 0.72, fat: 9.51, fiber: 0 },
  densityGramsPerMl: 1.027,
  pieceWeightGrams: 50,
}

export const nokedliSummary: RecipeSummary = {
  id: '01a193cb-612d-7053-8147-5bc4ab4e5f43',
  title: 'Nokedli',
  servings: 2,
  tags: ['köret', 'magyar'],
  energyKcalPerServing: 311.39,
}

function line(
  id: string,
  ingredient: IngredientSummary,
  amount: number,
  unit: RecipeDetails['ingredients'][number]['unit'],
  note: string | null,
) {
  return {
    id,
    ingredientId: ingredient.id,
    name: ingredient.name,
    hungarianName: ingredient.hungarianName,
    amount,
    unit,
    note,
    nutrientsPer100g: ingredient.nutrientsPer100g,
    densityGramsPerMl: ingredient.densityGramsPerMl,
    pieceWeightGrams: ingredient.pieceWeightGrams,
  }
}

export const nokedli: RecipeDetails = {
  id: nokedliSummary.id,
  title: 'Nokedli',
  servings: 2,
  steps: ['Mix the dough.', 'Press it into boiling water.'],
  tags: ['köret', 'magyar'],
  ingredients: [
    line('01a193cb-612e-729a-bf7d-ac311cd785c0', flour, 125, 'Gram', null),
    line('01a193cb-612f-7739-a32d-a7edc48c00de', oliveOil, 0.5, 'Tablespoon', 'for the pan'),
    line('01a193cb-612e-7ecf-bac4-818e77cb4974', egg, 1.5, 'Piece', null),
  ],
  nutritionPerServing: {
    energyKcal: 311.39,
    protein: 11.17,
    carbohydrate: 47.96,
    fat: 7.6,
    fiber: 1.69,
  },
  nutritionTotal: {
    energyKcal: 622.78,
    protein: 22.34,
    carbohydrate: 95.92,
    fat: 15.2,
    fiber: 3.38,
  },
}
