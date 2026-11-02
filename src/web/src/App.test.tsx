import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import { stubFetch } from './test/fetch'
import { renderWithQueryClient } from './test/render'

describe('App', () => {
  it('opens on the recipes, with the navigation and the API health', async () => {
    stubFetch({
      '/health': () => new Response('Healthy'),
      '/api/recipes': () => Response.json([]),
    })

    renderWithQueryClient(<App />)

    expect(screen.getByRole('heading', { name: 'Meal Planner' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Recipes' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Recipes' })).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByText('Healthy')).toBeInTheDocument()
  })

  it('links to the new-recipe form', async () => {
    stubFetch({
      '/health': () => new Response('Healthy'),
      '/api/recipes': () => Response.json([]),
    })
    renderWithQueryClient(<App />)

    await userEvent.setup().click(await screen.findByRole('link', { name: 'New recipe' }))

    expect(screen.getByRole('heading', { name: 'New recipe' })).toBeInTheDocument()
  })

  it('shows a not-found page for unknown URLs', () => {
    stubFetch({ '/health': () => new Response('Healthy') })

    renderWithQueryClient(<App />, { route: '/nope' })

    expect(screen.getByText('There’s no page here.')).toBeInTheDocument()
  })

  it('shows the API as unreachable when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    renderWithQueryClient(<App />, { route: '/nope' })

    expect(await screen.findByText('Unreachable')).toBeInTheDocument()
  })
})
