import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from '@/App'
import type { RecipeDetails, RecipeSummary } from '@/api/generated/model'
import { stubFetch } from '@/test/fetch'
import { renderWithQueryClient } from '@/test/render'

const nokedliSummary: RecipeSummary = {
  id: '01a193cb-612d-7053-8147-5bc4ab4e5f43',
  title: 'Nokedli',
  servings: 2,
  tags: ['köret', 'magyar'],
  energyKcalPerServing: 311.39,
}

const nokedli: RecipeDetails = {
  id: nokedliSummary.id,
  title: 'Nokedli',
  servings: 2,
  steps: ['Mix the dough.', 'Press it into boiling water.'],
  tags: ['köret', 'magyar'],
  ingredients: [
    {
      id: '01a193cb-612e-729a-bf7d-ac311cd785c0',
      ingredientId: '01a193ca-bd95-7684-8dec-6e3ddc8572cf',
      name: 'Wheat flour, white, all-purpose, enriched, bleached',
      hungarianName: 'Búzaliszt (finomliszt)',
      amount: 125,
      unit: 'Gram',
      note: null,
    },
    {
      id: '01a193cb-612f-7739-a32d-a7edc48c00de',
      ingredientId: '01a193ca-bd90-7555-9988-5770661bc687',
      name: 'Oil, olive, salad or cooking',
      hungarianName: 'Olívaolaj',
      amount: 0.5,
      unit: 'Tablespoon',
      note: 'for the pan',
    },
    {
      id: '01a193cb-612e-7ecf-bac4-818e77cb4974',
      ingredientId: '01a193ca-bd1e-752b-9ed4-cea2b6f90f11',
      name: 'Egg, whole, raw, fresh',
      hungarianName: 'Tojás',
      amount: 1.5,
      unit: 'Piece',
      note: null,
    },
  ],
  nutritionPerServing: {
    energyKcal: 311.39,
    protein: 11.17,
    carbohydrate: 47.96,
    fat: 7.6,
    fiber: 1.69,
  },
  nutritionTotal: {
    energyKcal: 622.78,
    protein: 22.34,
    carbohydrate: 95.92,
    fat: 15.2,
    fiber: 3.38,
  },
}

const health = { '/health': () => new Response('Healthy') }

function ingredientLines() {
  const list = screen.getByRole('heading', { name: 'Ingredients' })
    .nextElementSibling as HTMLElement
  return within(list)
    .getAllByRole('listitem')
    .map((item) => item.textContent)
}

function totalRow(label: string) {
  return screen.getByRole('row', { name: new RegExp(`^${label}`) })
}

describe('Recipe list', () => {
  it('lists recipes with kcal per serving and links to their page', async () => {
    stubFetch({ ...health, '/api/recipes': () => Response.json([nokedliSummary]) })

    renderWithQueryClient(<App />, { route: '/recipes' })

    const link = await screen.findByRole('link', { name: /Nokedli/ })
    expect(link).toHaveAttribute('href', `/recipes/${nokedli.id}`)
    expect(link).toHaveTextContent('2 servings · köret, magyar')
    expect(link).toHaveTextContent('311 kcal / serving')
  })

  it('searches from the URL and says when nothing matches', async () => {
    stubFetch({ ...health, '/api/recipes?search=gulyas': () => Response.json([]) })

    renderWithQueryClient(<App />, { route: '/recipes?search=gulyas' })

    expect(await screen.findByText('No recipes match “gulyas”.')).toBeInTheDocument()
    expect(screen.getByRole('searchbox', { name: 'Search recipes' })).toHaveValue('gulyas')
  })

  it('says when there are no recipes yet', async () => {
    stubFetch({ ...health, '/api/recipes': () => Response.json([]) })

    renderWithQueryClient(<App />, { route: '/recipes' })

    expect(await screen.findByText('No recipes yet.')).toBeInTheDocument()
  })

  it('offers a retry when the list fails to load', async () => {
    stubFetch({ ...health, '/api/recipes': () => new Response(null, { status: 500 }) })

    renderWithQueryClient(<App />, { route: '/recipes' })

    expect(await screen.findByText('Couldn’t load the recipes.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})

describe('Recipe details', () => {
  it('shows ingredients, steps and nutrition for the recipe’s servings', async () => {
    stubFetch({ ...health, [`/api/recipes/${nokedli.id}`]: () => Response.json(nokedli) })

    renderWithQueryClient(<App />, { route: `/recipes/${nokedli.id}` })

    expect(await screen.findByRole('heading', { name: 'Nokedli' })).toBeInTheDocument()
    expect(ingredientLines()).toEqual([
      '125 gBúzaliszt (finomliszt)',
      '0.5 tbspOlívaolaj, for the pan',
      '1.5 piecesTojás',
    ])
    expect(screen.getByText('Press it into boiling water.')).toBeInTheDocument()
    expect(totalRow('Energy')).toHaveTextContent('Energy311 kcal623 kcal')
  })

  it('moving the slider from 2 to 4 servings doubles the quantities and the total', async () => {
    stubFetch({ ...health, [`/api/recipes/${nokedli.id}`]: () => Response.json(nokedli) })
    renderWithQueryClient(<App />, { route: `/recipes/${nokedli.id}` })
    const slider = await screen.findByRole('slider', { name: 'Servings' })
    expect(slider).toHaveAttribute('aria-valuenow', '2')

    const user = userEvent.setup()
    slider.focus()
    await user.keyboard('{ArrowRight}{ArrowRight}')

    expect(slider).toHaveAttribute('aria-valuenow', '4')
    expect(ingredientLines()).toEqual([
      '250 gBúzaliszt (finomliszt)',
      '1 tbspOlívaolaj, for the pan',
      '3 piecesTojás',
    ])
    // Per serving stays, the total doubles.
    expect(totalRow('Energy')).toHaveTextContent('Energy311 kcal1,246 kcal')
    expect(screen.getByRole('columnheader', { name: 'Total (4 servings)' })).toBeInTheDocument()
  })

  it('says when the recipe doesn’t exist', async () => {
    stubFetch({ ...health, '/api/recipes/missing': () => new Response(null, { status: 404 }) })

    renderWithQueryClient(<App />, { route: '/recipes/missing' })

    expect(await screen.findByText('This recipe doesn’t exist (any more).')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to the recipes' })).toHaveAttribute(
      'href',
      '/recipes',
    )
  })
})
