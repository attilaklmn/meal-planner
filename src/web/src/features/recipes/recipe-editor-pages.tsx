import { useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '@/api/fetcher'
import type { RecipeDetails } from '@/api/generated/model'
import {
  getGetRecipeQueryKey,
  getListRecipesQueryKey,
  useCreateRecipe,
  useGetRecipe,
  useUpdateRecipe,
} from '@/api/generated/recipes/recipes'
import { RecipeForm } from './recipe-form'
import { emptyRecipe, toFormValues } from './recipe-form-schema'

// After a save the response already is the new details: put it in the cache instead of fetching it again,
// and mark every recipe list stale (the title, servings or kcal may have changed).
function useOnSaved() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return async (recipe: RecipeDetails) => {
    queryClient.setQueryData(getGetRecipeQueryKey(recipe.id), recipe)
    await queryClient.invalidateQueries({ queryKey: getListRecipesQueryKey() })
    await navigate(`/recipes/${recipe.id}`)
  }
}

export function NewRecipePage() {
  const onSaved = useOnSaved()
  const createRecipe = useCreateRecipe()

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-2xl font-semibold">New recipe</h2>
      <RecipeForm
        defaultValues={emptyRecipe}
        submitLabel="Create recipe"
        cancelTo="/recipes"
        onSubmit={async (data) => onSaved(await createRecipe.mutateAsync({ data }))}
      />
    </section>
  )
}

export function EditRecipePage() {
  const { id = '' } = useParams()
  const recipe = useGetRecipe(id)
  const onSaved = useOnSaved()
  const updateRecipe = useUpdateRecipe()

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
        <Link to="/recipes" className="underline">
          Back to the recipes
        </Link>
      </div>
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-2xl font-semibold">Edit “{recipe.data.title}”</h2>
      <RecipeForm
        // The form reads its defaults once; keyed by id so another recipe starts a fresh form.
        key={recipe.data.id}
        defaultValues={toFormValues(recipe.data)}
        submitLabel="Save changes"
        cancelTo={`/recipes/${id}`}
        onSubmit={async (data) => onSaved(await updateRecipe.mutateAsync({ id, data }))}
      />
    </section>
  )
}
