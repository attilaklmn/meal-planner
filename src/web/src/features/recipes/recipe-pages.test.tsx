import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from '@/App'
import { stubFetch } from '@/test/fetch'
import { renderWithQueryClient } from '@/test/render'
import { nokedli, nokedliSummary } from './test-recipes'

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
