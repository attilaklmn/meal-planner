import { describe, expect, it } from 'vitest'
import { formatQuantity, scaleAmount, totalNutrition } from './scale-recipe'

describe('scaleAmount', () => {
  it('scales linearly with servings', () => {
    expect(scaleAmount(2, 2, 4)).toBe(4)
    expect(scaleAmount(3, 4, 1)).toBe(0.75)
  })
})

describe('formatQuantity', () => {
  it('shows short units and at most two decimals', () => {
    expect(formatQuantity(250, 'Gram')).toBe('250 g')
    expect(formatQuantity(1.5, 'Tablespoon')).toBe('1.5 tbsp')
    expect(formatQuantity(2 / 3, 'Cup')).toBe('0.67 cups')
  })

  it('uses the singular for exactly one', () => {
    expect(formatQuantity(1, 'Cup')).toBe('1 cup')
    expect(formatQuantity(1, 'Piece')).toBe('1 piece')
    expect(formatQuantity(3, 'Piece')).toBe('3 pieces')
  })
})

describe('totalNutrition', () => {
  it('multiplies the per-serving values by the servings', () => {
    expect(
      totalNutrition({ energyKcal: 300, protein: 10, carbohydrate: 40, fat: 8, fiber: 2 }, 3),
    ).toEqual({ energyKcal: 900, protein: 30, carbohydrate: 120, fat: 24, fiber: 6 })
  })
})
