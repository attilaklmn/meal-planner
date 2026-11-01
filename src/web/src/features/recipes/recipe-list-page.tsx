import { keepPreviousData } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { useListRecipes } from '@/api/generated/recipes/recipes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useDebouncedValue } from '@/hooks/use-debounced-value'

const kcal = new Intl.NumberFormat('en', { maximumFractionDigits: 0 })

export function RecipeListPage() {
  // The search lives in the URL (?search=), so a filtered list can be bookmarked and survives a reload.
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('search') ?? ''
  const [input, setInput] = useState(search)
  const debouncedInput = useDebouncedValue(input.trim(), 300)

  useEffect(() => {
    if (debouncedInput !== search) {
      // replace: typing shouldn't add a history entry per keystroke.
      setSearchParams(debouncedInput ? { search: debouncedInput } : {}, { replace: true })
    }
  }, [debouncedInput, search, setSearchParams])

  const recipes = useListRecipes(search ? { search } : undefined, {
    // Keep showing the previous results while the next search loads, instead of flashing a loading state.
    query: { placeholderData: keepPreviousData },
  })

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-2xl font-semibold">Recipes</h2>
      <Input
        type="search"
        aria-label="Search recipes"
        placeholder="Search by title…"
        value={input}
        onChange={(event) => setInput(event.target.value)}
      />

      {recipes.isPending ? (
        <p className="text-muted-foreground">Loading recipes…</p>
      ) : recipes.isError ? (
        <div className="flex items-center gap-3">
          <p className="text-destructive">Couldn’t load the recipes.</p>
          <Button variant="outline" size="sm" onClick={() => void recipes.refetch()}>
            Try again
          </Button>
        </div>
      ) : recipes.data.length === 0 ? (
        <p className="text-muted-foreground">
          {search ? `No recipes match “${search}”.` : 'No recipes yet.'}
        </p>
      ) : (
        <ul className="divide-y rounded-md border">
          {recipes.data.map((recipe) => (
            <li key={recipe.id}>
              <Link
                to={`/recipes/${recipe.id}`}
                className="hover:bg-accent flex items-center justify-between gap-4 p-3"
              >
                <span className="flex flex-col">
                  <span className="font-medium">{recipe.title}</span>
                  <span className="text-muted-foreground text-sm">
                    {recipe.servings} servings
                    {recipe.tags.length > 0 && ` · ${recipe.tags.join(', ')}`}
                  </span>
                </span>
                <span className="text-sm whitespace-nowrap">
                  {kcal.format(recipe.energyKcalPerServing)} kcal / serving
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
