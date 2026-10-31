import type { NutrientsResponse } from '@/api/generated/model'

const grams = new Intl.NumberFormat('en', { maximumFractionDigits: 1 })
const kcal = new Intl.NumberFormat('en', { maximumFractionDigits: 0 })

// "143 kcal · P 12.6 g · C 0.7 g · F 9.5 g"
export function formatNutrients(nutrients: NutrientsResponse): string {
  return [
    `${kcal.format(nutrients.energyKcal)} kcal`,
    `P ${grams.format(nutrients.protein)} g`,
    `C ${grams.format(nutrients.carbohydrate)} g`,
    `F ${grams.format(nutrients.fat)} g`,
  ].join(' · ')
}
