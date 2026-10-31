import { useState } from 'react'
import type { IngredientSummary } from '@/api/generated/model'
import { IngredientAutocomplete } from './ingredient-autocomplete'

// Temporary home for the picker until the recipe editor (M2) uses it.
export function IngredientSearchDemo() {
  const [selected, setSelected] = useState<IngredientSummary | null>(null)

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-xl font-semibold">Ingredient search</h2>
      <IngredientAutocomplete value={selected} onSelect={setSelected} />
      {selected && (
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
          <dt className="text-muted-foreground">English name</dt>
          <dd>{selected.name}</dd>
          <dt className="text-muted-foreground">Energy</dt>
          <dd>{selected.nutrientsPer100g.energyKcal} kcal / 100 g</dd>
          <dt className="text-muted-foreground">Protein</dt>
          <dd>{selected.nutrientsPer100g.protein} g</dd>
          <dt className="text-muted-foreground">Carbohydrate</dt>
          <dd>{selected.nutrientsPer100g.carbohydrate} g</dd>
          <dt className="text-muted-foreground">Fat</dt>
          <dd>{selected.nutrientsPer100g.fat} g</dd>
          <dt className="text-muted-foreground">Fiber</dt>
          <dd>{selected.nutrientsPer100g.fiber} g</dd>
          <dt className="text-muted-foreground">Density</dt>
          <dd>
            {selected.densityGramsPerMl === null ? '–' : `${selected.densityGramsPerMl} g/ml`}
          </dd>
          <dt className="text-muted-foreground">Piece weight</dt>
          <dd>{selected.pieceWeightGrams === null ? '–' : `${selected.pieceWeightGrams} g`}</dd>
        </dl>
      )}
    </section>
  )
}
