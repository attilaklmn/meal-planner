import { z } from 'zod'
import { Meal } from '@/api/generated/model'

// MealPlan.MaxServings in Planning.Domain, checked here too for instant feedback; the server has the final say.
export const MAX_SERVINGS = 20

const recipeSchema = z.object({ id: z.string(), title: z.string(), servings: z.number() })

export type PickedRecipe = z.infer<typeof recipeSchema>

export const entryFormSchema = z.object({
  meal: z.enum(Meal),
  recipe: recipeSchema.nullable().refine((value) => value !== null, 'Choose a recipe.'),
  // An empty number input gives NaN, which z.number() rejects with this message.
  servings: z
    .number({ error: 'Enter the number of servings.' })
    .int('Whole servings only.')
    .min(1, 'At least 1 serving.')
    .max(MAX_SERVINGS, `At most ${MAX_SERVINGS} servings.`),
})

export type EntryFormInput = z.input<typeof entryFormSchema>
export type EntryFormValues = z.output<typeof entryFormSchema>

export const emptyEntry: EntryFormInput = { meal: Meal.Lunch, recipe: null, servings: 2 }

// AddMealPlanEntryRequest field → form field; "day" comes from the card and has no field of its own.
const serverFields: Record<string, keyof EntryFormInput> = {
  recipeId: 'recipe',
  meal: 'meal',
  servings: 'servings',
}

export function toEntryField(serverKey: string): keyof EntryFormInput | null {
  return serverFields[serverKey] ?? null
}
