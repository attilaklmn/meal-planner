import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '@/api/fetcher'
import type { NutritionResponse, RecipeDetails } from '@/api/generated/model'
import { useGetRecipe } from '@/api/generated/recipes/recipes'
import { Button } from '@/components/ui/button'
import { Slider } from '@/components/ui/slider'
import { formatQuantity, scaleAmount, totalNutrition } from './scale-recipe'

export function RecipeDetailsPage() {
  const { id = '' } = useParams()
  const recipe = useGetRecipe(id)

  if (recipe.isPending) {
    return <p className="text-muted-foreground">Loading recipe…</p>
  }

  if (recipe.isError) {
    const notFound = recipe.error instanceof ApiError && recipe.error.status === 404
    return (
      <div className="flex flex-col items-start gap-3">
        <p className={notFound ? 'text-muted-foreground' : 'text-destructive'}>
          {notFound ? 'This recipe doesn’t exist (any more).' : 'Couldn’t load the recipe.'}
        </p>
        {notFound ? (
          <Link to="/recipes" className="underline">
            Back to the recipes
          </Link>
        ) : (
          <Button variant="outline" size="sm" onClick={() => void recipe.refetch()}>
            Try again
          </Button>
        )}
      </div>
    )
  }

  // Keyed by id: opening another recipe starts again from its own servings.
  return <RecipeView key={recipe.data.id} recipe={recipe.data} />
}

function RecipeView({ recipe }: { recipe: RecipeDetails }) {
  // Local UI state, not server state: the chosen servings only rescale what's already loaded.
  const [servings, setServings] = useState(recipe.servings)
  const maxServings = Math.max(12, recipe.servings * 2)

  return (
    <article className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <Link to="/recipes" className="text-muted-foreground text-sm underline">
          ← Recipes
        </Link>
        <h2 className="text-2xl font-semibold">{recipe.title}</h2>
        {recipe.tags.length > 0 && (
          <p className="text-muted-foreground text-sm">{recipe.tags.join(', ')}</p>
        )}
      </header>

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold">Servings</h3>
          <span className="tabular-nums">{servings}</span>
        </div>
        <Slider
          aria-label="Servings"
          min={1}
          max={maxServings}
          step={1}
          value={[servings]}
          onValueChange={([value]) => setServings(value)}
        />
      </section>

      <section className="flex flex-col gap-2">
        <h3 className="text-lg font-semibold">Ingredients</h3>
        <ul className="flex flex-col gap-1">
          {recipe.ingredients.map((line) => (
            <li key={line.id} className="flex gap-3">
              <span className="w-28 shrink-0 text-right tabular-nums">
                {formatQuantity(scaleAmount(line.amount, recipe.servings, servings), line.unit)}
              </span>
              <span>
                {line.hungarianName}
                {line.note && <span className="text-muted-foreground">, {line.note}</span>}
              </span>
            </li>
          ))}
        </ul>
      </section>

      <section className="flex flex-col gap-2">
        <h3 className="text-lg font-semibold">Nutrition</h3>
        <NutritionTable
          perServing={recipe.nutritionPerServing}
          total={totalNutrition(recipe.nutritionPerServing, servings)}
          servings={servings}
        />
      </section>

      {recipe.steps.length > 0 && (
        <section className="flex flex-col gap-2">
          <h3 className="text-lg font-semibold">Steps</h3>
          <ol className="flex list-decimal flex-col gap-2 pl-6">
            {recipe.steps.map((step, index) => (
              <li key={index}>{step}</li>
            ))}
          </ol>
        </section>
      )}
    </article>
  )
}

const kcal = new Intl.NumberFormat('en', { maximumFractionDigits: 0 })
const grams = new Intl.NumberFormat('en', { maximumFractionDigits: 1 })

const rows: { label: string; key: keyof NutritionResponse; format: (value: number) => string }[] = [
  { label: 'Energy', key: 'energyKcal', format: (value) => `${kcal.format(value)} kcal` },
  { label: 'Protein', key: 'protein', format: (value) => `${grams.format(value)} g` },
  { label: 'Carbohydrate', key: 'carbohydrate', format: (value) => `${grams.format(value)} g` },
  { label: 'Fat', key: 'fat', format: (value) => `${grams.format(value)} g` },
  { label: 'Fiber', key: 'fiber', format: (value) => `${grams.format(value)} g` },
]

function NutritionTable({
  perServing,
  total,
  servings,
}: {
  perServing: NutritionResponse
  total: NutritionResponse
  servings: number
}) {
  return (
    <table className="w-full max-w-md text-sm">
      <thead className="text-muted-foreground">
        <tr>
          <th className="text-left font-normal"></th>
          <th className="text-right font-normal">Per serving</th>
          <th className="text-right font-normal">
            {servings === 1 ? 'Total (1 serving)' : `Total (${servings} servings)`}
          </th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row.key}>
            <th scope="row" className="text-left font-normal">
              {row.label}
            </th>
            <td className="text-right tabular-nums">{row.format(perServing[row.key])}</td>
            <td className="text-right tabular-nums">{row.format(total[row.key])}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
