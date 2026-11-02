import { z } from 'zod'
import { Unit, type RecipeDetails, type RecipeRequest } from '@/api/generated/model'
import { canMeasure, missingDataFor, unitLabel } from './units'

// Limits from Recipes.Domain, checked here too so most mistakes show before a request.
// The server stays the authority: its validation errors are mapped back onto these fields.
const TITLE_MAX = 200
const MAX_SERVINGS = 100
const NOTE_MAX = 200
const STEP_MAX = 2000

const nutrientsSchema = z.object({
  energyKcal: z.number(),
  protein: z.number(),
  carbohydrate: z.number(),
  fat: z.number(),
  fiber: z.number(),
})

// The picked ingredient with what the preview and the unit check need; from a search result or a saved line.
const ingredientSchema = z.object({
  id: z.string(),
  name: z.string(),
  hungarianName: z.string(),
  nutrientsPer100g: nutrientsSchema,
  densityGramsPerMl: z.number().nullable(),
  pieceWeightGrams: z.number().nullable(),
})

export type EditorIngredient = z.infer<typeof ingredientSchema>

const lineSchema = z
  .object({
    // Checked here rather than in superRefine: refinements are skipped while another field of the line is invalid,
    // and a blank row should show all its problems at once.
    ingredient: ingredientSchema
      .nullable()
      .refine((value) => value !== null, 'Choose an ingredient.'),
    // An empty number input gives NaN, which z.number() rejects with this message.
    amount: z.number({ error: 'Enter an amount.' }).positive('Must be more than 0.'),
    unit: z.enum(Unit),
    note: z.string().trim().max(NOTE_MAX, `At most ${NOTE_MAX} characters.`),
  })
  .superRefine((line, context) => {
    if (line.ingredient !== null && !canMeasure(line.ingredient, line.unit)) {
      context.addIssue({
        code: 'custom',
        path: ['unit'],
        message: `Can't be measured in ${unitLabel(line.unit)}: ${missingDataFor(line.unit)}.`,
      })
    }
  })

export const recipeFormSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'Give the recipe a title.')
    .max(TITLE_MAX, `At most ${TITLE_MAX} characters.`),
  servings: z
    .number({ error: 'Enter the number of servings.' })
    .int('Whole servings only.')
    .min(1, 'At least 1 serving.')
    .max(MAX_SERVINGS, `At most ${MAX_SERVINGS} servings.`),
  // Comma separated in one field; split on submit.
  tags: z.string(),
  ingredients: z.array(lineSchema),
  // Objects, not strings: useFieldArray needs an object per row. Blank steps are dropped on submit.
  steps: z.array(z.object({ text: z.string().max(STEP_MAX, `At most ${STEP_MAX} characters.`) })),
})

// What the fields hold while editing (a row may have no ingredient yet) and what a valid form produces.
export type RecipeFormInput = z.input<typeof recipeFormSchema>
export type RecipeFormValues = z.output<typeof recipeFormSchema>

export const emptyRecipe: RecipeFormInput = {
  title: '',
  servings: 4,
  tags: '',
  ingredients: [],
  steps: [{ text: '' }],
}

export function toFormValues(recipe: RecipeDetails): RecipeFormInput {
  return {
    title: recipe.title,
    servings: recipe.servings,
    tags: recipe.tags.join(', '),
    ingredients: recipe.ingredients.map((line) => ({
      ingredient: {
        id: line.ingredientId,
        name: line.name,
        hungarianName: line.hungarianName,
        nutrientsPer100g: line.nutrientsPer100g,
        densityGramsPerMl: line.densityGramsPerMl,
        pieceWeightGrams: line.pieceWeightGrams,
      },
      amount: line.amount,
      unit: line.unit,
      note: line.note ?? '',
    })),
    steps: recipe.steps.length > 0 ? recipe.steps.map((text) => ({ text })) : [{ text: '' }],
  }
}

// Takes the schema's output, so every line has an ingredient.
export function toRecipeRequest(values: RecipeFormValues): RecipeRequest {
  return {
    title: values.title,
    servings: values.servings,
    tags: values.tags
      .split(',')
      .map((tag) => tag.trim())
      .filter((tag) => tag !== ''),
    steps: values.steps.map((step) => step.text.trim()).filter((text) => text !== ''),
    ingredients: values.ingredients.map((line) => ({
      ingredientId: line.ingredient.id,
      amount: line.amount,
      unit: line.unit,
      note: line.note === '' ? null : line.note,
    })),
  }
}

/**
 * The server's ValidationProblem keys ("title", "ingredients[2].unit") as form field paths ("ingredients.2.unit").
 * Keys without a matching field (e.g. "steps", whose indexes shift once blank steps are dropped) return null.
 */
export function toFieldPath(serverKey: string): string | null {
  if (serverKey === 'title' || serverKey === 'servings' || serverKey === 'tags') return serverKey

  const line = /^ingredients\[(\d+)\]\.(ingredientId|amount|unit|note)$/.exec(serverKey)
  if (!line) return null

  const field = line[2] === 'ingredientId' ? 'ingredient' : line[2]
  return `ingredients.${line[1]}.${field}`
}
