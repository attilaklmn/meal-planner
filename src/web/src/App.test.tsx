import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import { stubFetch } from './test/fetch'
import { renderWithQueryClient } from './test/render'

describe('App', () => {
  it('shows the title, the API health and the ingredient search', async () => {
    stubFetch({ '/health': () => new Response('Healthy') })

    renderWithQueryClient(<App />)

    expect(screen.getByRole('heading', { name: 'Meal Planner' })).toBeInTheDocument()
    expect(await screen.findByText('Healthy')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Ingredient search' })).toBeInTheDocument()
    expect(screen.getByRole('combobox')).toHaveTextContent('Choose an ingredient…')
  })

  it('shows the API as unreachable when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    renderWithQueryClient(<App />)

    expect(await screen.findByText('Unreachable')).toBeInTheDocument()
  })
})
