import { keepPreviousData } from '@tanstack/react-query'
import { ChevronsUpDown } from 'lucide-react'
import { useState } from 'react'
import { useSearchIngredients } from '@/api/generated/catalog/catalog'
import type { IngredientSummary } from '@/api/generated/model'
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
import { formatNutrients } from './format-nutrients'

const MIN_SEARCH_LENGTH = 2
const SEARCH_DEBOUNCE_MS = 250
const RESULT_LIMIT = 20

type IngredientAutocompleteProps = {
  value: IngredientSummary | null
  onSelect: (ingredient: IngredientSummary) => void
}

// Combobox for picking a catalog ingredient; the recipe editor (M2) uses it for each ingredient row.
export function IngredientAutocomplete({ value, onSelect }: IngredientAutocompleteProps) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const term = useDebouncedValue(search.trim(), SEARCH_DEBOUNCE_MS)
  const enabled = term.length >= MIN_SEARCH_LENGTH

  const { data, isFetching, isError, error } = useSearchIngredients(
    { search: term, limit: RESULT_LIMIT },
    {
      query: {
        enabled,
        // Keeps the last results on screen while the next term loads, instead of flashing an empty list.
        placeholderData: keepPreviousData,
        // The catalog changes rarely; going back to a term within 5 minutes needs no request.
        staleTime: 5 * 60_000,
      },
    },
  )

  const results = enabled ? (data ?? []) : []

  function select(ingredient: IngredientSummary) {
    onSelect(ingredient)
    setOpen(false)
    setSearch('')
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-full justify-between font-normal"
        >
          {value ? value.hungarianName : 'Choose an ingredient…'}
          <ChevronsUpDown className="opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-(--radix-popover-trigger-width) min-w-80 p-0" align="start">
        {/* The API already filters and ranks; cmdk's own fuzzy filter would reorder the results. */}
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Search in Hungarian or English…"
            value={search}
            onValueChange={setSearch}
          />
          <CommandList aria-busy={isFetching}>
            {!enabled ? (
              <p className="text-muted-foreground p-3 text-sm">
                Type at least {MIN_SEARCH_LENGTH} letters.
              </p>
            ) : isError ? (
              <p className="text-destructive p-3 text-sm" role="alert">
                Search failed ({error.message}).
              </p>
            ) : data === undefined ? (
              <p className="text-muted-foreground p-3 text-sm">Searching…</p>
            ) : (
              <CommandEmpty>No ingredients found.</CommandEmpty>
            )}
            {results.map((ingredient) => (
              <CommandItem
                key={ingredient.id}
                value={ingredient.id}
                onSelect={() => select(ingredient)}
                className="flex flex-col items-start gap-0.5"
              >
                <span className="font-medium">{ingredient.hungarianName}</span>
                <span className="text-muted-foreground line-clamp-1 text-xs">
                  {ingredient.name}
                </span>
                <span className="text-muted-foreground text-xs">
                  {formatNutrients(ingredient.nutrientsPer100g)} / 100 g
                </span>
              </CommandItem>
            ))}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
