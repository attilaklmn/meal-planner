import { useQueryClient } from '@tanstack/react-query'
import { Minus, Plus, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import {
  getGetMealPlanQueryKey,
  useChangeMealPlanEntry,
  useGetMealPlan,
  useRemoveMealPlanEntry,
} from '@/api/generated/meal-plans/meal-plans'
import { Meal, type MealPlanDayResponse, type MealPlanEntryResponse } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { AddEntryDialog } from './add-entry-dialog'
import { MAX_SERVINGS } from './entry-form-schema'
import { useStoreWeek } from './use-store-week'
import { addWeeks, parseIsoDate, toIsoDate, weekFromParam, weekStartOf } from './week'

const kcal = new Intl.NumberFormat('en', { maximumFractionDigits: 0 })
const dayDate = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short' })
const weekRange = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})
const meals = Object.values(Meal)

// Read once per page visit: render stays pure, and "this week" doesn't jump while the page is open.
function useToday() {
  const [today] = useState(() => new Date())
  return today
}

export function WeeklyPlanPage() {
  // The week lives in the URL (?week=2026-11-16), so a week can be bookmarked and survives a reload;
  // without it the page shows this week.
  const [searchParams, setSearchParams] = useSearchParams()
  const param = searchParams.get('week')
  const requested = weekFromParam(param)
  const today = useToday()
  const thisWeek = weekStartOf(today)
  const week = requested ?? thisWeek

  useEffect(() => {
    // Any day of a week opens that week; the URL is corrected to its Monday (and junk is dropped).
    if (param !== null && param !== requested) {
      setSearchParams(requested ? { week: requested } : {}, { replace: true })
    }
  }, [param, requested, setSearchParams])

  const plan = useGetMealPlan(week)
  const monday = parseIsoDate(week)!
  const sunday = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6)

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col">
          <h2 className="text-2xl font-semibold">Plan</h2>
          <p className="text-muted-foreground">{weekRange.formatRange(monday, sunday)}</p>
        </div>
        <nav aria-label="Weeks" className="flex gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link to={`/plan?week=${addWeeks(week, -1)}`}>← Previous</Link>
          </Button>
          <Button variant="outline" size="sm" asChild>
            <Link to="/plan" aria-current={week === thisWeek ? 'page' : undefined}>
              This week
            </Link>
          </Button>
          <Button variant="outline" size="sm" asChild>
            <Link to={`/plan?week=${addWeeks(week, 1)}`}>Next →</Link>
          </Button>
        </nav>
      </div>

      {plan.isPending ? (
        <p className="text-muted-foreground">Loading the week…</p>
      ) : plan.isError ? (
        <div className="flex items-center gap-3">
          <p className="text-destructive">Couldn’t load the plan.</p>
          <Button variant="outline" size="sm" onClick={() => void plan.refetch()}>
            Try again
          </Button>
        </div>
      ) : (
        <>
          <p>
            Week total:{' '}
            <span className="font-medium">{kcal.format(plan.data.energyKcal)} kcal</span>
            {plan.data.energyKcal > 0 && (
              <span className="text-muted-foreground">
                {' '}
                · {kcal.format(plan.data.energyKcal / 7)} kcal / day on average
              </span>
            )}
          </p>
          {/* One column on every screen: a day is a short list, and a phone shows the days one under the other. */}
          <ol className="flex flex-col gap-3">
            {plan.data.days.map((day) => (
              <DayCard
                key={day.day}
                weekStart={plan.data.weekStart}
                day={day}
                isToday={day.date === toIsoDate(today)}
              />
            ))}
          </ol>
        </>
      )}
    </section>
  )
}

function DayCard({
  weekStart,
  day,
  isToday,
}: {
  weekStart: string
  day: MealPlanDayResponse
  isToday: boolean
}) {
  const headingId = `day-${day.day}`

  return (
    <li
      aria-labelledby={headingId}
      className={cn('flex flex-col gap-2 rounded-md border p-3', isToday && 'border-primary')}
    >
      <div className="flex items-center justify-between gap-3">
        <h3 id={headingId} className="font-semibold">
          {day.day}{' '}
          <span className="text-muted-foreground font-normal">
            {dayDate.format(parseIsoDate(day.date)!)}
          </span>
          {isToday && <span className="text-primary ml-2 text-sm font-normal">today</span>}
        </h3>
        <div className="flex items-center gap-3">
          <span className="text-sm tabular-nums">{kcal.format(day.energyKcal)} kcal</span>
          <AddEntryDialog weekStart={weekStart} day={day.day} />
        </div>
      </div>

      {day.entries.length === 0 ? (
        <p className="text-muted-foreground text-sm">Nothing planned.</p>
      ) : (
        <div className="flex flex-col gap-2">
          {meals.map((meal) => {
            const entries = day.entries.filter((entry) => entry.meal === meal)
            return (
              entries.length > 0 && (
                <div key={meal} className="flex flex-col gap-1">
                  <h4 className="text-muted-foreground text-xs font-medium uppercase">{meal}</h4>
                  <ul className="flex flex-col gap-1">
                    {entries.map((entry) => (
                      <EntryRow key={entry.id} weekStart={weekStart} day={day} entry={entry} />
                    ))}
                  </ul>
                </div>
              )
            )
          })}
        </div>
      )}
    </li>
  )
}

function EntryRow({
  weekStart,
  day,
  entry,
}: {
  weekStart: string
  day: MealPlanDayResponse
  entry: MealPlanEntryResponse
}) {
  const queryClient = useQueryClient()
  const storeWeek = useStoreWeek()
  // A failed change (the entry was removed in another tab: 404) reloads the week to show what is really there.
  const mutation = {
    onSuccess: storeWeek,
    onError: () => queryClient.invalidateQueries({ queryKey: getGetMealPlanQueryKey(weekStart) }),
  }
  // One hook per row: its pending state disables only this row's buttons, and a second click can't race the first.
  const change = useChangeMealPlanEntry({ mutation })
  const remove = useRemoveMealPlanEntry({ mutation })
  const busy = change.isPending || remove.isPending

  function setServings(servings: number) {
    change.mutate({
      weekStart,
      entryId: entry.id,
      data: { day: day.day, meal: entry.meal, servings },
    })
  }

  return (
    <li className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1">
      <span className="flex flex-col">
        <Link to={`/recipes/${entry.recipeId}`} className="font-medium hover:underline">
          {entry.recipeTitle}
        </Link>
        <span className="text-muted-foreground text-sm">
          {entry.servings} {entry.servings === 1 ? 'serving' : 'servings'} ·{' '}
          {kcal.format(entry.energyKcal)} kcal
        </span>
        {(change.isError || remove.isError) && (
          <span role="alert" className="text-destructive text-sm">
            Couldn’t save the change.
          </span>
        )}
      </span>
      <span className="flex items-center gap-1">
        <Button
          variant="outline"
          size="icon"
          className="size-8"
          aria-label={`Fewer servings of ${entry.recipeTitle}`}
          disabled={busy || entry.servings <= 1}
          onClick={() => setServings(entry.servings - 1)}
        >
          <Minus />
        </Button>
        <Button
          variant="outline"
          size="icon"
          className="size-8"
          aria-label={`More servings of ${entry.recipeTitle}`}
          disabled={busy || entry.servings >= MAX_SERVINGS}
          onClick={() => setServings(entry.servings + 1)}
        >
          <Plus />
        </Button>
        {/* No confirmation: an entry is put back with two clicks. */}
        <Button
          variant="ghost"
          size="icon"
          className="size-8"
          aria-label={`Remove ${entry.recipeTitle} from ${day.day}`}
          disabled={busy}
          onClick={() => remove.mutate({ weekStart, entryId: entry.id })}
        >
          <X />
        </Button>
      </span>
    </li>
  )
}
