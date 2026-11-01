import { Link, Navigate, Route, Routes } from 'react-router'
import { AppShell } from '@/components/app-shell'
import { IngredientSearchDemo } from '@/features/catalog/ingredient-search-demo'
import { RecipeDetailsPage } from '@/features/recipes/recipe-details-page'
import { RecipeListPage } from '@/features/recipes/recipe-list-page'

// Declarative routes (React Router's library mode): data stays in TanStack Query, the router only maps URLs to pages.
function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<Navigate to="/recipes" replace />} />
        <Route path="recipes" element={<RecipeListPage />} />
        <Route path="recipes/:id" element={<RecipeDetailsPage />} />
        <Route path="ingredients" element={<IngredientSearchDemo />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}

function NotFoundPage() {
  return (
    <div className="flex flex-col items-start gap-2">
      <p className="text-muted-foreground">There’s no page here.</p>
      <Link to="/recipes" className="underline">
        Go to the recipes
      </Link>
    </div>
  )
}

export default App
