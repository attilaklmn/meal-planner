import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { stubFetch } from '@/test/fetch'
import { renderWithQueryClient } from '@/test/render'
import { CatalogPing } from './catalog-ping'

describe('CatalogPing', () => {
  it('turns an HTTP error from the API into an error state', async () => {
    stubFetch({
      '/api/catalog/ping': () => new Response(null, { status: 500 }),
    })

    renderWithQueryClient(<CatalogPing />)

    expect(
      await screen.findByText('Catalog module: unavailable (API request failed with HTTP 500)'),
    ).toBeInTheDocument()
  })
})
