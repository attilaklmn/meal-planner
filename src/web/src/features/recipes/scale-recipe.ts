import type { NutritionResponse, Unit } from '@/api/generated/model'

// Changing servings is linear: 2 tbsp for 2 servings is 4 tbsp for 4. Done in the browser, no request needed.
export function scaleAmount(amount: number, recipeServings: number, servings: number): number {
  return (amount * servings) / recipeServings
}

// Nutrition per serving doesn't change with servings; the total for the chosen servings does.
export function totalNutrition(perServing: NutritionResponse, servings: number): NutritionResponse {
  return {
    energyKcal: perServing.energyKcal * servings,
    protein: perServing.protein * servings,
    carbohydrate: perServing.carbohydrate * servings,
    fat: perServing.fat * servings,
    fiber: perServing.fiber * servings,
  }
}

const unitLabels: Record<Unit, { one: string; many: string }> = {
  Gram: { one: 'g', many: 'g' },
  Kilogram: { one: 'kg', many: 'kg' },
  Milliliter: { one: 'ml', many: 'ml' },
  Liter: { one: 'l', many: 'l' },
  Teaspoon: { one: 'tsp', many: 'tsp' },
  Tablespoon: { one: 'tbsp', many: 'tbsp' },
  Cup: { one: 'cup', many: 'cups' },
  Piece: { one: 'piece', many: 'pieces' },
}

// Two decimals at most: scaling 3 eggs from 4 to 3 servings gives 2.25, not 2.2499999.
const amountFormat = new Intl.NumberFormat('en', { maximumFractionDigits: 2 })

// "250 g", "1.5 tbsp", "1 cup", "3 pieces"
export function formatQuantity(amount: number, unit: Unit): string {
  const label = unitLabels[unit]
  return `${amountFormat.format(amount)} ${amount === 1 ? label.one : label.many}`
}
