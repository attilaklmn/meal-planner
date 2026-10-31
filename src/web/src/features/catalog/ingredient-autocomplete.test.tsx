import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import type { IngredientSummary } from '@/api/generated/model'
import { stubFetch } from '@/test/fetch'
import { renderWithQueryClient } from '@/test/render'
import { IngredientAutocomplete } from './ingredient-autocomplete'

const egg: IngredientSummary = {
  id: '01a18bc0-9571-7cc0-922b-bc821243b457',
  name: 'Egg, whole, raw, fresh',
  hungarianName: 'Tojás',
  nutrientsPer100g: { energyKcal: 143, protein: 12.56, carbohydrate: 0.72, fat: 9.51, fiber: 0 },
}

const eggYolk: IngredientSummary = {
  id: '01a18bc0-9571-7cc0-922b-bc821243b458',
  name: 'Egg, yolk, raw, fresh',
  hungarianName: 'Tojássárgája',
  nutrientsPer100g: { energyKcal: 322, protein: 15.86, carbohydrate: 3.59, fat: 26.54, fiber: 0 },
}

const searchUrl = (term: string) => `/api/catalog/ingredients?search=${term}&limit=20`

async function openAndType(text: string) {
  const user = userEvent.setup()
  await user.click(screen.getByRole('combobox'))
  await user.type(screen.getByPlaceholderText('Search in Hungarian or English…'), text)
  return user
}

describe('IngredientAutocomplete', () => {
  it('searches once the user stops typing and lists the results with nutrition', async () => {
    const fetchMock = stubFetch({ [searchUrl('tojas')]: () => Response.json([egg, eggYolk]) })
    renderWithQueryClient(<IngredientAutocomplete value={null} onSelect={vi.fn()} />)

    await openAndType('tojas')

    expect(await screen.findByText('Tojás')).toBeInTheDocument()
    expect(screen.getByText('Tojássárgája')).toBeInTheDocument()
    expect(screen.getByText('143 kcal · P 12.6 g · C 0.7 g · F 9.5 g / 100 g')).toBeInTheDocument()
    // Debounced: one request for the whole word, not one per keystroke.
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('calls onSelect with the clicked ingredient and shows it on the button', async () => {
    stubFetch({ [searchUrl('tojas')]: () => Response.json([egg, eggYolk]) })
    const onSelect = vi.fn()
    // Holds the selection like a real parent would, so the button can show it.
    function Parent() {
      const [value, setValue] = useState<IngredientSummary | null>(null)
      return (
        <IngredientAutocomplete
          value={value}
          onSelect={(ingredient) => {
            onSelect(ingredient)
            setValue(ingredient)
          }}
        />
      )
    }
    renderWithQueryClient(<Parent />)

    const user = await openAndType('tojas')
    await user.click(await screen.findByText('Tojássárgája'))

    expect(onSelect).toHaveBeenCalledWith(eggYolk)
    expect(screen.getByRole('combobox')).toHaveTextContent('Tojássárgája')
  })

  it('can be used with the keyboard', async () => {
    stubFetch({ [searchUrl('tojas')]: () => Response.json([egg, eggYolk]) })
    const onSelect = vi.fn()
    renderWithQueryClient(<IngredientAutocomplete value={null} onSelect={onSelect} />)

    const user = await openAndType('tojas')
    await screen.findByText('Tojás')
    // The first result is highlighted; one step down is the second.
    await user.keyboard('{ArrowDown}{Enter}')

    expect(onSelect).toHaveBeenCalledWith(eggYolk)
  })

  it('does not search for a single letter', async () => {
    const fetchMock = stubFetch({})
    renderWithQueryClient(<IngredientAutocomplete value={null} onSelect={vi.fn()} />)

    await openAndType('t')

    expect(screen.getByText('Type at least 2 letters.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('says so when nothing matches', async () => {
    stubFetch({ [searchUrl('xyz')]: () => Response.json([]) })
    renderWithQueryClient(<IngredientAutocomplete value={null} onSelect={vi.fn()} />)

    await openAndType('xyz')

    expect(await screen.findByText('No ingredients found.')).toBeInTheDocument()
  })

  it('shows an error when the search fails', async () => {
    stubFetch({ [searchUrl('tojas')]: () => new Response(null, { status: 500 }) })
    renderWithQueryClient(<IngredientAutocomplete value={null} onSelect={vi.fn()} />)

    await openAndType('tojas')

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Search failed (API request failed with HTTP 500).',
    )
  })
})
