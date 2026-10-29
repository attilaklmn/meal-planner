import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import { renderWithQueryClient } from './test/render'

describe('App', () => {
  it('shows the title and the API health reported by the backend', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Healthy', { status: 200 })))

    renderWithQueryClient(<App />)

    expect(screen.getByRole('heading', { name: 'Meal Planner' })).toBeInTheDocument()
    expect(await screen.findByText('Healthy')).toBeInTheDocument()
  })

  it('shows the API as unreachable when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    renderWithQueryClient(<App />)

    expect(await screen.findByText('Unreachable')).toBeInTheDocument()
  })
})
