import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '@/api/fetcher'
import type { RecipeDetails } from '@/api/generated/model'
import {
  getGetRecipeQueryKey,
  getListRecipesQueryKey,
  useDeleteRecipe,
  useGetRecipe,
} from '@/api/generated/recipes/recipes'
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
import { Slider } from '@/components/ui/slider'
import { NutritionTable } from './nutrition-table'
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
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-2xl font-semibold">{recipe.title}</h2>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to={`/recipes/${recipe.id}/edit`}>Edit</Link>
            </Button>
            <DeleteRecipeButton id={recipe.id} title={recipe.title} />
          </div>
        </div>
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

function DeleteRecipeButton({ id, title }: { id: string; title: string }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const deleteRecipe = useDeleteRecipe({
    mutation: {
      onSuccess: async () => {
        // Leave first, so the details query isn't refetched for a recipe that's gone.
        await navigate('/recipes')
        queryClient.removeQueries({ queryKey: getGetRecipeQueryKey(id) })
        // Prefix match: every list query, whatever its search.
        await queryClient.invalidateQueries({ queryKey: getListRecipesQueryKey() })
      },
    },
  })

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          Delete
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Delete “{title}”?</DialogTitle>
          <DialogDescription>This can’t be undone.</DialogDescription>
        </DialogHeader>
        {deleteRecipe.isError && (
          <p role="alert" className="text-destructive text-sm">
            Couldn’t delete the recipe. Try again.
          </p>
        )}
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="outline">Keep it</Button>
          </DialogClose>
          <Button
            variant="destructive"
            disabled={deleteRecipe.isPending}
            onClick={() => deleteRecipe.mutate({ id })}
          >
            {deleteRecipe.isPending ? 'Deleting…' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
