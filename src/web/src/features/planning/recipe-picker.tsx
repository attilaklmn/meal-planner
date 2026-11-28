import { keepPreviousData } from '@tanstack/react-query'
import { ChevronsUpDown } from 'lucide-react'
import { useState } from 'react'
import { useListRecipes } from '@/api/generated/recipes/recipes'
import { Button } from '@/components/ui/button'
import {
  Command,
  CommandEmpty,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import type { PickedRecipe } from './entry-form-schema'

const kcal = new Intl.NumberFormat('en', { maximumFractionDigits: 0 })

type RecipePickerProps = {
  value: PickedRecipe | null
  onSelect: (recipe: PickedRecipe) => void
  id?: string
  'aria-invalid'?: boolean
}

// Combobox over the recipe list. A home has tens of recipes, so an empty search lists them all
// and typing narrows the list on the server (same search as the recipe list page).
export function RecipePicker({
  value,
  onSelect,
  id,
  'aria-invalid': ariaInvalid,
}: RecipePickerProps) {
  const [open, setOpen] = useState(false)

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          id={id}
          variant="outline"
          role="combobox"
          aria-expanded={open}
          aria-invalid={ariaInvalid}
          className="w-full justify-between font-normal"
        >
          {value ? value.title : 'Choose a recipe…'}
          <ChevronsUpDown className="opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-(--radix-popover-trigger-width) min-w-72 p-0" align="start">
        {/* Mounted only while open, so the recipes are fetched when the picker is opened, not per day card. */}
        <RecipeResults
          onSelect={(recipe) => {
            onSelect(recipe)
            setOpen(false)
          }}
        />
      </PopoverContent>
    </Popover>
  )
}

function RecipeResults({ onSelect }: { onSelect: (recipe: PickedRecipe) => void }) {
  const [search, setSearch] = useState('')
  const term = useDebouncedValue(search.trim(), 250)
  const recipes = useListRecipes(term ? { search: term } : undefined, {
    query: { placeholderData: keepPreviousData },
  })

  return (
    // The API already filters; cmdk's own fuzzy filter would hide or reorder its results.
    <Command shouldFilter={false}>
      <CommandInput placeholder="Search recipes…" value={search} onValueChange={setSearch} />
      <CommandList aria-busy={recipes.isFetching}>
        {recipes.isError ? (
          <p className="text-destructive p-3 text-sm" role="alert">
            Couldn’t load the recipes.
          </p>
        ) : recipes.isPending ? (
          <p className="text-muted-foreground p-3 text-sm">Loading recipes…</p>
        ) : (
          <CommandEmpty>{term ? `No recipes match “${term}”.` : 'No recipes yet.'}</CommandEmpty>
        )}
        {recipes.data?.map((recipe) => (
          <CommandItem
            key={recipe.id}
            value={recipe.id}
            onSelect={() =>
              onSelect({ id: recipe.id, title: recipe.title, servings: recipe.servings })
            }
            className="flex flex-col items-start gap-0.5"
          >
            <span className="font-medium">{recipe.title}</span>
            <span className="text-muted-foreground text-xs">
              {recipe.servings} servings · {kcal.format(recipe.energyKcalPerServing)} kcal / serving
            </span>
          </CommandItem>
        ))}
      </CommandList>
    </Command>
  )
}
