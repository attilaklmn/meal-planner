import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '@/App'
import type { MealPlanEntryResponse, MealPlanResponse } from '@/api/generated/model'
import { nokedliSummary } from '@/features/recipes/test-recipes'
import { stubFetch } from '@/test/fetch'
import { renderWithQueryClient } from '@/test/render'

const health = { '/health': () => new Response('Healthy') }
const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const

// A week as the API returns it: all seven days, Monday first, with the given entries per day.
function week(
  weekStart: string,
  entries: Partial<Record<(typeof days)[number], MealPlanEntryResponse[]>> = {},
): MealPlanResponse {
  const [year, month, day] = weekStart.split('-').map(Number)
  const planDays = days.map((name, index) => {
    const date = new Date(year, month - 1, day + index)
    const dayEntries = entries[name] ?? []
    return {
      day: name,
      date: `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`,
      energyKcal: dayEntries.reduce((sum, entry) => sum + entry.energyKcal, 0),
      entries: dayEntries,
    }
  })
  return {
    weekStart,
    days: planDays,
    energyKcal: planDays.reduce((sum, planDay) => sum + planDay.energyKcal, 0),
  }
}

const nokedliDinner: MealPlanEntryResponse = {
  id: '01a19f00-0000-7000-8000-000000000001',
  meal: 'Dinner',
  recipeId: nokedliSummary.id,
  recipeTitle: 'Nokedli',
  servings: 3,
  energyKcalPerServing: 311.39,
  energyKcal: 934.17,
}

function dayCard(name: string) {
  return screen.getByRole('listitem', { name: new RegExp(`^${name}`) })
}

function requests(fetchMock: ReturnType<typeof stubFetch>) {
  return fetchMock.mock.calls.map(([url, init]) => `${init?.method ?? 'GET'} ${String(url)}`)
}

afterEach(() => {
  vi.useRealTimers()
})

describe('Weekly plan', () => {
  it('shows the seven days with their entries, kcal totals and links to the recipes', async () => {
    stubFetch({
      ...health,
      '/api/plans/2026-11-16': () =>
        Response.json(week('2026-11-16', { Wednesday: [nokedliDinner] })),
    })

    renderWithQueryClient(<App />, { route: '/plan?week=2026-11-16' })

    expect(await screen.findByText(/^Week total/)).toHaveTextContent('Week total: 934 kcal')
    expect(screen.getByText(/16\s–\s22 Nov 2026/)).toBeInTheDocument()
    const wednesday = dayCard('Wednesday')
    expect(within(wednesday).getByRole('heading', { level: 3 })).toHaveTextContent(
      'Wednesday 18 Nov',
    )
    expect(within(wednesday).getByText('Dinner')).toBeInTheDocument()
    expect(within(wednesday).getByRole('link', { name: 'Nokedli' })).toHaveAttribute(
      'href',
      `/recipes/${nokedliSummary.id}`,
    )
    expect(within(wednesday).getByText('3 servings · 934 kcal')).toBeInTheDocument()
    expect(within(dayCard('Monday')).getByText('Nothing planned.')).toBeInTheDocument()
    expect(within(dayCard('Monday')).getByText('0 kcal')).toBeInTheDocument()
  })

  it('opens this week without ?week=, and the arrows load the weeks around it', async () => {
    // Only Date is faked: timers stay real for Testing Library's waits.
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 22, 12)) // a Sunday: still the week of Monday 16 November
    const fetchMock = stubFetch({
      ...health,
      '/api/plans/2026-11-16': () => Response.json(week('2026-11-16')),
      '/api/plans/2026-11-23': () => Response.json(week('2026-11-23')),
      '/api/plans/2026-11-09': () => Response.json(week('2026-11-09')),
    })
    const user = userEvent.setup()

    renderWithQueryClient(<App />, { route: '/plan' })

    expect(await screen.findByText(/16\s–\s22 Nov 2026/)).toBeInTheDocument()
    // The week label comes from the URL; the days come with the response.
    const sunday = await screen.findByRole('listitem', { name: /^Sunday/ })
    expect(within(sunday).getByText('today')).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: 'Next →' }))
    expect(await screen.findByText(/23\s–\s29 Nov 2026/)).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: 'This week' }))
    await user.click(await screen.findByRole('link', { name: '← Previous' }))
    expect(await screen.findByText(/9\s–\s15 Nov 2026/)).toBeInTheDocument()

    const planRequests = requests(fetchMock).filter((request) => request.includes('/api/plans'))
    expect(planRequests).toContain('GET /api/plans/2026-11-23')
    expect(planRequests).toContain('GET /api/plans/2026-11-09')
  })

  it('moves any day of a week in the URL to its Monday', async () => {
    const fetchMock = stubFetch({
      ...health,
      '/api/plans/2026-11-16': () => Response.json(week('2026-11-16')),
    })

    renderWithQueryClient(<App />, { route: '/plan?week=2026-11-19' })

    expect(await screen.findByText(/16\s–\s22 Nov 2026/)).toBeInTheDocument()
    expect(requests(fetchMock)).toContain('GET /api/plans/2026-11-16')
  })

  it('adds a recipe to Wednesday’s dinner and shows the week the server sends back', async () => {
    const fetchMock = stubFetch({
      ...health,
      '/api/plans/2026-11-16': () => Response.json(week('2026-11-16')),
      '/api/recipes': () => Response.json([nokedliSummary]),
      '/api/plans/2026-11-16/entries': () =>
        Response.json(week('2026-11-16', { Wednesday: [nokedliDinner] })),
    })
    const user = userEvent.setup()
    renderWithQueryClient(<App />, { route: '/plan?week=2026-11-16' })

    await user.click(await screen.findByRole('button', { name: 'Add to Wednesday' }))
    const dialog = screen.getByRole('dialog', { name: 'Add to Wednesday' })
    await user.click(within(dialog).getByRole('combobox', { name: 'Recipe' }))
    await user.click(await screen.findByRole('option', { name: /Nokedli/ }))
    // Picking the recipe suggests its own servings (2); the user makes it 3.
    const servings = within(dialog).getByRole('spinbutton', { name: 'Servings' })
    expect(servings).toHaveValue(2)
    await user.selectOptions(within(dialog).getByRole('combobox', { name: 'Meal' }), 'Dinner')
    await user.clear(servings)
    await user.type(servings, '3')
    await user.click(within(dialog).getByRole('button', { name: 'Add to plan' }))

    expect(
      await within(dayCard('Wednesday')).findByRole('link', { name: 'Nokedli' }),
    ).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')
    expect(post?.[0]).toBe('/api/plans/2026-11-16/entries')
    expect(JSON.parse(String(post?.[1]?.body))).toEqual({
      recipeId: nokedliSummary.id,
      day: 'Wednesday',
      meal: 'Dinner',
      servings: 3,
    })
    // The response replaced the cached week: no second GET.
    expect(
      requests(fetchMock).filter((request) => request === 'GET /api/plans/2026-11-16'),
    ).toHaveLength(1)
  })

  it('asks for a recipe before sending anything', async () => {
    const fetchMock = stubFetch({
      ...health,
      '/api/plans/2026-11-16': () => Response.json(week('2026-11-16')),
    })
    const user = userEvent.setup()
    renderWithQueryClient(<App />, { route: '/plan?week=2026-11-16' })

    await user.click(await screen.findByRole('button', { name: 'Add to Monday' }))
    await user.click(screen.getByRole('button', { name: 'Add to plan' }))

    expect(await screen.findByText('Choose a recipe.')).toBeInTheDocument()
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
  })

  it('changes servings and removes an entry', async () => {
    const fetchMock = stubFetch({
      ...health,
      '/api/plans/2026-11-16': () =>
        Response.json(week('2026-11-16', { Wednesday: [nokedliDinner] })),
      [`/api/plans/2026-11-16/entries/${nokedliDinner.id}`]: (init) =>
        init?.method === 'DELETE'
          ? Response.json(week('2026-11-16'))
          : Response.json(
              week('2026-11-16', {
                Wednesday: [{ ...nokedliDinner, servings: 4, energyKcal: 1245.56 }],
              }),
            ),
    })
    const user = userEvent.setup()
    renderWithQueryClient(<App />, { route: '/plan?week=2026-11-16' })

    await user.click(await screen.findByRole('button', { name: 'More servings of Nokedli' }))
    expect(await screen.findByText('4 servings · 1,246 kcal')).toBeInTheDocument()
    const put = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(JSON.parse(String(put?.[1]?.body))).toEqual({
      day: 'Wednesday',
      meal: 'Dinner',
      servings: 4,
    })

    await user.click(screen.getByRole('button', { name: 'Remove Nokedli from Wednesday' }))
    expect(await within(dayCard('Wednesday')).findByText('Nothing planned.')).toBeInTheDocument()
  })

  it('offers a retry when the week fails to load', async () => {
    stubFetch({ ...health, '/api/plans/2026-11-16': () => new Response(null, { status: 500 }) })

    renderWithQueryClient(<App />, { route: '/plan?week=2026-11-16' })

    expect(await screen.findByText('Couldn’t load the plan.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
