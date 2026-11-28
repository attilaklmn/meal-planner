import { useQueryClient } from '@tanstack/react-query'
import { getGetMealPlanQueryKey } from '@/api/generated/meal-plans/meal-plans'
import type { MealPlanResponse } from '@/api/generated/model'

// Every add, change and remove answers with the whole week (new daily totals included): it replaces the cached week,
// so the page updates without another GET.
export function useStoreWeek() {
  const queryClient = useQueryClient()
  return (plan: MealPlanResponse) =>
    queryClient.setQueryData(getGetMealPlanQueryKey(plan.weekStart), plan)
}
