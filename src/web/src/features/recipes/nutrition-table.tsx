import type { NutritionResponse } from '@/api/generated/model'

const kcal = new Intl.NumberFormat('en', { maximumFractionDigits: 0 })
const grams = new Intl.NumberFormat('en', { maximumFractionDigits: 1 })

const rows: { label: string; key: keyof NutritionResponse; format: (value: number) => string }[] = [
  { label: 'Energy', key: 'energyKcal', format: (value) => `${kcal.format(value)} kcal` },
  { label: 'Protein', key: 'protein', format: (value) => `${grams.format(value)} g` },
  { label: 'Carbohydrate', key: 'carbohydrate', format: (value) => `${grams.format(value)} g` },
  { label: 'Fat', key: 'fat', format: (value) => `${grams.format(value)} g` },
  { label: 'Fiber', key: 'fiber', format: (value) => `${grams.format(value)} g` },
]

// Per serving and total side by side; the details page and the editor's live preview share it.
export function NutritionTable({
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
