import { ApiStatus } from '@/components/api-status'
import { CatalogPing } from '@/features/catalog/catalog-ping'

function App() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col gap-6 p-6">
      <header>
        <h1 className="text-3xl font-semibold">Meal Planner</h1>
        <p className="text-muted-foreground">Recipes, nutrition and shopping lists.</p>
      </header>
      <ApiStatus />
      <CatalogPing />
    </main>
  )
}

export default App
