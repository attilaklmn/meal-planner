import { describe, expect, it } from 'vitest'
import { toFieldPath } from './recipe-form-schema'

describe('toFieldPath', () => {
  it('maps the server’s ValidationProblem keys onto form fields', () => {
    expect(toFieldPath('title')).toBe('title')
    expect(toFieldPath('servings')).toBe('servings')
    expect(toFieldPath('ingredients[2].unit')).toBe('ingredients.2.unit')
    expect(toFieldPath('ingredients[0].ingredientId')).toBe('ingredients.0.ingredient')
  })

  it('leaves keys without a field to the form-level message', () => {
    expect(toFieldPath('steps')).toBeNull()
    expect(toFieldPath('recipe')).toBeNull()
  })
})
