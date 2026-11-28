import { zodResolver } from '@hookform/resolvers/zod'
import { Plus } from 'lucide-react'
import { useId, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { ApiError } from '@/api/fetcher'
import { useAddMealPlanEntry } from '@/api/generated/meal-plans/meal-plans'
import { Meal, type DayOfWeek, type HttpValidationProblemDetails } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  emptyEntry,
  entryFormSchema,
  MAX_SERVINGS,
  toEntryField,
  type EntryFormInput,
  type EntryFormValues,
} from './entry-form-schema'
import { RecipePicker } from './recipe-picker'
import { useStoreWeek } from './use-store-week'

export function AddEntryDialog({ weekStart, day }: { weekStart: string; day: DayOfWeek }) {
  const [open, setOpen] = useState(false)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" aria-label={`Add to ${day}`}>
          <Plus /> Add
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add to {day}</DialogTitle>
          <DialogDescription>Pick a recipe, the meal and how many servings.</DialogDescription>
        </DialogHeader>
        {/* Rendered only while open, so every opening starts from an empty form. */}
        <EntryForm weekStart={weekStart} day={day} onAdded={() => setOpen(false)} />
      </DialogContent>
    </Dialog>
  )
}

function EntryForm({
  weekStart,
  day,
  onAdded,
}: {
  weekStart: string
  day: DayOfWeek
  onAdded: () => void
}) {
  const id = useId()
  const storeWeek = useStoreWeek()
  const addEntry = useAddMealPlanEntry({ mutation: { onSuccess: storeWeek } })
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    formState: { errors, isSubmitting, dirtyFields },
  } = useForm<EntryFormInput, unknown, EntryFormValues>({
    resolver: zodResolver(entryFormSchema),
    defaultValues: emptyEntry,
  })

  async function submit(values: EntryFormValues) {
    try {
      await addEntry.mutateAsync({
        weekStart,
        data: { recipeId: values.recipe.id, day, meal: values.meal, servings: values.servings },
      })
      onAdded()
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) {
        showServerErrors((error.body as HttpValidationProblemDetails | undefined)?.errors ?? {})
      } else {
        setError('root.server', { message: 'Couldn’t add the recipe. Try again.' })
      }
    }
  }

  // The server checks what the form can't (a recipe deleted since the list was loaded, a full week).
  function showServerErrors(serverErrors: Record<string, string[]>) {
    const unplaced: string[] = []
    for (const [key, messages] of Object.entries(serverErrors)) {
      const field = toEntryField(key)
      if (field) {
        setError(field, { type: 'server', message: messages.join(' ') })
      } else {
        unplaced.push(...messages)
      }
    }
    if (unplaced.length > 0) {
      setError('root.server', { message: unplaced.join(' ') })
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor={`${id}-recipe`}>Recipe</Label>
        <Controller
          control={control}
          name="recipe"
          render={({ field }) => (
            <RecipePicker
              id={`${id}-recipe`}
              value={field.value}
              aria-invalid={errors.recipe ? true : undefined}
              onSelect={(recipe) => {
                field.onChange(recipe)
                // Cooking the whole recipe is the usual case; servings typed by hand are kept.
                if (!dirtyFields.servings) {
                  setValue('servings', Math.min(recipe.servings, MAX_SERVINGS))
                }
              }}
            />
          )}
        />
        <FieldError message={errors.recipe?.message} />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`${id}-meal`}>Meal</Label>
          <select
            id={`${id}-meal`}
            aria-invalid={errors.meal ? true : undefined}
            className="border-input aria-invalid:border-destructive h-9 rounded-md border bg-transparent px-2 text-sm shadow-xs"
            {...register('meal')}
          >
            {Object.values(Meal).map((meal) => (
              <option key={meal} value={meal}>
                {meal}
              </option>
            ))}
          </select>
          <FieldError message={errors.meal?.message} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`${id}-servings`}>Servings</Label>
          <Input
            id={`${id}-servings`}
            type="number"
            min={1}
            max={MAX_SERVINGS}
            step={1}
            aria-invalid={errors.servings ? true : undefined}
            {...register('servings', { valueAsNumber: true })}
          />
          <FieldError message={errors.servings?.message} />
        </div>
      </div>

      {errors.root?.server && (
        <p role="alert" className="text-destructive text-sm">
          {errors.root.server.message}
        </p>
      )}

      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Cancel
          </Button>
        </DialogClose>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Adding…' : 'Add to plan'}
        </Button>
      </DialogFooter>
    </form>
  )
}

function FieldError({ message }: { message?: string }) {
  return message ? <p className="text-destructive text-sm">{message}</p> : null
}
