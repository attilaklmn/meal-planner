import { zodResolver } from '@hookform/resolvers/zod'
import { Trash2 } from 'lucide-react'
import { useId, type ReactNode } from 'react'
import {
  Controller,
  useFieldArray,
  useForm,
  useWatch,
  type Control,
  type FieldErrors,
  type FieldPath,
  type UseFormGetValues,
  type UseFormRegister,
  type UseFormSetValue,
} from 'react-hook-form'
import { Link } from 'react-router'
import { ApiError } from '@/api/fetcher'
import type { HttpValidationProblemDetails, RecipeRequest } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { IngredientAutocomplete } from '@/features/catalog/ingredient-autocomplete'
import { NutritionTable } from './nutrition-table'
import { previewNutrition } from './nutrition-preview'
import {
  recipeFormSchema,
  toFieldPath,
  toRecipeRequest,
  type EditorIngredient,
  type RecipeFormInput,
  type RecipeFormValues,
} from './recipe-form-schema'
import { canMeasure, missingDataFor, unitLabel, units } from './units'

type RecipeFormProps = {
  defaultValues: RecipeFormInput
  submitLabel: string
  cancelTo: string
  /** Sends the request; throwing an ApiError with a ValidationProblem puts its messages on the fields. */
  onSubmit: (request: RecipeRequest) => Promise<unknown>
}

// Shared by the new and edit pages: they differ only in where the values come from and which request is sent.
export function RecipeForm({ defaultValues, submitLabel, cancelTo, onSubmit }: RecipeFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<RecipeFormInput, unknown, RecipeFormValues>({
    resolver: zodResolver(recipeFormSchema),
    defaultValues,
  })

  const ingredientRows = useFieldArray({ control, name: 'ingredients' })
  const stepRows = useFieldArray({ control, name: 'steps' })

  async function submit(values: RecipeFormValues) {
    try {
      await onSubmit(toRecipeRequest(values))
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) {
        showServerErrors((error.body as HttpValidationProblemDetails | undefined)?.errors ?? {})
      } else {
        setError('root.server', { message: 'Couldn’t save the recipe. Try again.' })
      }
    }
  }

  // The server checks what the form can't (an ingredient deleted from the catalog meanwhile) and has the final say.
  function showServerErrors(serverErrors: Record<string, string[]>) {
    const unplaced: string[] = []
    for (const [key, messages] of Object.entries(serverErrors)) {
      const field = toFieldPath(key)
      if (field) {
        setError(field as FieldPath<RecipeFormInput>, {
          type: 'server',
          message: messages.join(' '),
        })
      } else {
        unplaced.push(...messages)
      }
    }
    setError('root.server', {
      message: unplaced.length > 0 ? unplaced.join(' ') : 'Some fields need fixing.',
    })
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-6">
      <div className="grid gap-4 sm:grid-cols-[1fr_8rem]">
        <Field label="Title" error={errors.title?.message}>
          {(props) => <Input {...props} {...register('title')} />}
        </Field>
        <Field label="Servings" error={errors.servings?.message}>
          {(props) => (
            <Input
              {...props}
              type="number"
              min={1}
              step={1}
              {...register('servings', { valueAsNumber: true })}
            />
          )}
        </Field>
      </div>
      <Field
        label="Tags"
        hint="Comma separated, e.g. leves, vegetáriánus"
        error={errors.tags?.message}
      >
        {(props) => <Input {...props} {...register('tags')} />}
      </Field>

      <section className="flex flex-col gap-3">
        <h3 className="text-lg font-semibold">Ingredients</h3>
        {ingredientRows.fields.length === 0 && (
          <p className="text-muted-foreground text-sm">No ingredients yet.</p>
        )}
        <ol className="flex flex-col gap-3">
          {ingredientRows.fields.map((row, index) => (
            <IngredientRow
              key={row.id}
              index={index}
              control={control}
              setValue={setValue}
              getValues={getValues}
              register={register}
              errors={errors.ingredients?.[index]}
              onRemove={() => ingredientRows.remove(index)}
            />
          ))}
        </ol>
        <Button
          type="button"
          variant="outline"
          className="self-start"
          onClick={() =>
            ingredientRows.append({ ingredient: null, amount: Number.NaN, unit: 'Gram', note: '' })
          }
        >
          Add ingredient
        </Button>
      </section>

      <section className="flex flex-col gap-3">
        <h3 className="text-lg font-semibold">Steps</h3>
        <ol className="flex flex-col gap-3">
          {stepRows.fields.map((row, index) => (
            <li key={row.id} className="flex items-start gap-2">
              <span className="text-muted-foreground w-6 pt-2 text-right">{index + 1}.</span>
              <div className="flex grow flex-col gap-1">
                <Textarea
                  aria-label={`Step ${index + 1}`}
                  aria-invalid={errors.steps?.[index]?.text ? true : undefined}
                  {...register(`steps.${index}.text`)}
                />
                <FieldError message={errors.steps?.[index]?.text?.message} />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label={`Remove step ${index + 1}`}
                onClick={() => stepRows.remove(index)}
              >
                <Trash2 />
              </Button>
            </li>
          ))}
        </ol>
        <Button
          type="button"
          variant="outline"
          className="self-start"
          onClick={() => stepRows.append({ text: '' })}
        >
          Add step
        </Button>
      </section>

      <NutritionPreview control={control} />

      {errors.root?.server && (
        <p role="alert" className="text-destructive text-sm">
          {errors.root.server.message}
        </p>
      )}
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </Button>
        <Button variant="ghost" asChild>
          <Link to={cancelTo}>Cancel</Link>
        </Button>
      </div>
    </form>
  )
}

type IngredientRowProps = {
  index: number
  control: Control<RecipeFormInput, unknown, RecipeFormValues>
  setValue: UseFormSetValue<RecipeFormInput>
  getValues: UseFormGetValues<RecipeFormInput>
  register: UseFormRegister<RecipeFormInput>
  errors: FieldErrors<RecipeFormInput['ingredients'][number]> | undefined
  onRemove: () => void
}

function IngredientRow({
  index,
  control,
  setValue,
  getValues,
  register,
  errors,
  onRemove,
}: IngredientRowProps) {
  const ingredient = useWatch({ control, name: `ingredients.${index}.ingredient` })
  const position = index + 1

  return (
    <li className="flex flex-col gap-1 rounded-md border p-3">
      <div className="grid gap-2 sm:grid-cols-[1fr_6rem_7rem_auto]">
        <Controller
          control={control}
          name={`ingredients.${index}.ingredient`}
          render={({ field }) => (
            <IngredientAutocomplete
              value={field.value}
              aria-label={`Ingredient ${position}`}
              aria-invalid={errors?.ingredient ? true : undefined}
              onSelect={(picked) => {
                const next: EditorIngredient = {
                  id: picked.id,
                  name: picked.name,
                  hungarianName: picked.hungarianName,
                  nutrientsPer100g: picked.nutrientsPer100g,
                  densityGramsPerMl: picked.densityGramsPerMl,
                  pieceWeightGrams: picked.pieceWeightGrams,
                }
                field.onChange(next)
                // A unit the new ingredient can't be measured in falls back to grams, which always work.
                const unitField = `ingredients.${index}.unit` as const
                if (!canMeasure(next, getValues(unitField))) {
                  setValue(unitField, 'Gram', { shouldValidate: true })
                }
              }}
            />
          )}
        />
        <Input
          type="number"
          min={0}
          step="any"
          aria-label={`Amount ${position}`}
          aria-invalid={errors?.amount ? true : undefined}
          {...register(`ingredients.${index}.amount`, { valueAsNumber: true })}
        />
        <select
          aria-label={`Unit ${position}`}
          aria-invalid={errors?.unit ? true : undefined}
          className="border-input aria-invalid:border-destructive h-9 rounded-md border bg-transparent px-2 text-sm shadow-xs"
          {...register(`ingredients.${index}.unit`)}
        >
          {units.map((unit) => {
            // Disabled, not hidden: the user sees that tbsp exists, and why this ingredient can't use it.
            const usable = !ingredient || canMeasure(ingredient, unit)
            return (
              <option key={unit} value={unit} disabled={!usable}>
                {usable ? unitLabel(unit) : `${unitLabel(unit)} (${missingDataFor(unit)})`}
              </option>
            )
          })}
        </select>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Remove ingredient ${position}`}
          onClick={onRemove}
        >
          <Trash2 />
        </Button>
      </div>
      <Input
        placeholder="Note, e.g. finely chopped"
        aria-label={`Note ${position}`}
        aria-invalid={errors?.note ? true : undefined}
        {...register(`ingredients.${index}.note`)}
      />
      <FieldError
        message={[errors?.ingredient, errors?.amount, errors?.unit, errors?.note]
          .map((error) => error?.message)
          .filter(Boolean)
          .join(' ')}
      />
    </li>
  )
}

function NutritionPreview({
  control,
}: {
  control: Control<RecipeFormInput, unknown, RecipeFormValues>
}) {
  // Re-renders only this component on every change, not the whole form.
  const [lines, servings] = useWatch({ control, name: ['ingredients', 'servings'] })
  const preview = previewNutrition(lines, servings)
  const shownServings = Number.isInteger(servings) && servings > 0 ? servings : 1

  return (
    <section className="flex flex-col gap-2" aria-label="Nutrition preview">
      <h3 className="text-lg font-semibold">Nutrition</h3>
      <NutritionTable
        perServing={preview.perServing}
        total={preview.total}
        servings={shownServings}
      />
    </section>
  )
}

type FieldControlProps = { id: string; 'aria-invalid'?: boolean; 'aria-describedby'?: string }

function Field({
  label,
  hint,
  error,
  children,
}: {
  label: string
  hint?: string
  error?: string
  children: (props: FieldControlProps) => ReactNode
}) {
  const id = useId()
  const describedBy = [hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ')

  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      {children({
        id,
        'aria-invalid': error ? true : undefined,
        'aria-describedby': describedBy || undefined,
      })}
      {hint && (
        <p id={`${id}-hint`} className="text-muted-foreground text-xs">
          {hint}
        </p>
      )}
      <FieldError id={`${id}-error`} message={error} />
    </div>
  )
}

function FieldError({ id, message }: { id?: string; message?: string }) {
  return message ? (
    <p id={id} className="text-destructive text-sm">
      {message}
    </p>
  ) : null
}
