import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import { stubFetch } from './test/fetch'
import { renderWithQueryClient } from './test/render'

describe('App', () => {
  it('shows the title, the API health and the Catalog module answer', async () => {
    stubFetch({
      '/health': () => new Response('Healthy'),
      '/api/catalog/ping': () =>
        Response.json({ module: 'catalog', serverTime: '2026-10-29T08:00:00Z' }),
    })

    renderWithQueryClient(<App />)

    expect(screen.getByRole('heading', { name: 'Meal Planner' })).toBeInTheDocument()
    expect(await screen.findByText('Healthy')).toBeInTheDocument()
    expect(await screen.findByText('catalog')).toBeInTheDocument()
  })

  it('shows the API as unreachable when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    renderWithQueryClient(<App />)

    expect(await screen.findByText('Unreachable')).toBeInTheDocument()
  })
})
