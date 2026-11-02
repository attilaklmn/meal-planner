import { screen, within } from '@testing-library/react'
import userEvent, { type UserEvent } from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from '@/App'
import type { IngredientSummary, RecipeRequest } from '@/api/generated/model'
import { stubFetch } from '@/test/fetch'
import { renderWithQueryClient } from '@/test/render'
import { egg, flour, nokedli } from './test-recipes'

const health = { '/health': () => new Response('Healthy') }
const searchUrl = (term: string) => `/api/catalog/ingredients?search=${term}&limit=20`

async function pickIngredient(
  user: UserEvent,
  row: number,
  term: string,
  ingredient: IngredientSummary,
) {
  await user.click(screen.getByRole('combobox', { name: `Ingredient ${row}` }))
  await user.type(screen.getByPlaceholderText('Search in Hungarian or English…'), term)
  await user.click(
    await screen.findByRole('option', {
      name: (name) => name.startsWith(ingredient.hungarianName),
    }),
  )
}

function sentBody(fetchMock: ReturnType<typeof stubFetch>, method: string): RecipeRequest {
  // stubFetch's mock is typed with the URL only; fetch also gets the RequestInit.
  const calls = fetchMock.mock.calls as unknown as [string, RequestInit | undefined][]
  const init = calls.find(([, requestInit]) => requestInit?.method === method)?.[1]
  expect(init, `a ${method} request`).toBeDefined()
  return JSON.parse(init!.body as string) as RecipeRequest
}

function previewEnergy() {
  const preview = screen.getByRole('region', { name: 'Nutrition preview' })
  return within(preview).getByRole('row', { name: /^Energy/ })
}

describe('Recipe editor', () => {
  it('adds and removes ingredient and step rows', async () => {
    stubFetch(health)
    renderWithQueryClient(<App />, { route: '/recipes/new' })
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    expect(screen.getAllByRole('combobox', { name: /^Ingredient/ })).toHaveLength(2)

    await user.click(screen.getByRole('button', { name: 'Remove ingredient 1' }))
    expect(screen.getAllByRole('combobox', { name: /^Ingredient/ })).toHaveLength(1)

    await user.click(screen.getByRole('button', { name: 'Add step' }))
    expect(screen.getByRole('textbox', { name: 'Step 2' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Remove step 1' }))
    expect(screen.queryByRole('textbox', { name: 'Step 2' })).not.toBeInTheDocument()
  })

  it('shows validation messages and sends nothing while the form is invalid', async () => {
    const fetchMock = stubFetch(health)
    renderWithQueryClient(<App />, { route: '/recipes/new' })
    const user = userEvent.setup()

    await user.clear(screen.getByLabelText('Servings'))
    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))

    expect(await screen.findByText('Give the recipe a title.')).toBeInTheDocument()
    expect(screen.getByText('Enter the number of servings.')).toBeInTheDocument()
    expect(screen.getByText('Choose an ingredient. Enter an amount.')).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveAttribute('aria-invalid', 'true')
    expect(fetchMock.mock.calls.some(([url]) => url === '/api/recipes')).toBe(false)
  })

  it('disables units the ingredient has no data for', async () => {
    stubFetch({ ...health, [searchUrl('liszt')]: () => Response.json([flour]) })
    renderWithQueryClient(<App />, { route: '/recipes/new' })
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    await pickIngredient(user, 1, 'liszt', flour)

    const unit = screen.getByRole('combobox', { name: 'Unit 1' })
    expect(within(unit).getByRole('option', { name: 'piece (no piece weight)' })).toBeDisabled()
    expect(within(unit).getByRole('option', { name: 'tbsp' })).toBeEnabled()
  })

  it('previews nutrition while editing', async () => {
    stubFetch({ ...health, [searchUrl('tojas')]: () => Response.json([egg]) })
    renderWithQueryClient(<App />, { route: '/recipes/new' })
    const user = userEvent.setup()

    await user.clear(screen.getByLabelText('Servings'))
    await user.type(screen.getByLabelText('Servings'), '1')
    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    await pickIngredient(user, 1, 'tojas', egg)
    await user.type(screen.getByRole('spinbutton', { name: 'Amount 1' }), '2')
    await user.selectOptions(screen.getByRole('combobox', { name: 'Unit 1' }), 'Piece')

    // 2 eggs × 50 g = 100 g, at 143 kcal / 100 g.
    expect(previewEnergy()).toHaveTextContent('Energy143 kcal143 kcal')
  })

  it('creates a recipe with the entered data and opens it', async () => {
    const fetchMock = stubFetch({
      ...health,
      [searchUrl('tojas')]: () => Response.json([egg]),
      '/api/recipes': () => Response.json(nokedli, { status: 201 }),
      [`/api/recipes/${nokedli.id}`]: () => Response.json(nokedli),
    })
    renderWithQueryClient(<App />, { route: '/recipes/new' })
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Title'), '  Rántotta ')
    await user.clear(screen.getByLabelText('Servings'))
    await user.type(screen.getByLabelText('Servings'), '2')
    await user.type(screen.getByLabelText('Tags'), 'Reggeli, gyors,')
    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    await pickIngredient(user, 1, 'tojas', egg)
    await user.type(screen.getByRole('spinbutton', { name: 'Amount 1' }), '4')
    await user.selectOptions(screen.getByRole('combobox', { name: 'Unit 1' }), 'Piece')
    await user.type(screen.getByRole('textbox', { name: 'Note 1' }), 'felverve')
    await user.type(screen.getByRole('textbox', { name: 'Step 1' }), 'Süsd meg.')
    await user.click(screen.getByRole('button', { name: 'Add step' }))
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))

    // Opens the saved recipe (here the response's, Nokedli).
    expect(await screen.findByRole('heading', { name: 'Nokedli' })).toBeInTheDocument()
    expect(sentBody(fetchMock, 'POST')).toEqual({
      title: 'Rántotta',
      servings: 2,
      tags: ['Reggeli', 'gyors'],
      // The blank second step is dropped.
      steps: ['Süsd meg.'],
      ingredients: [{ ingredientId: egg.id, amount: 4, unit: 'Piece', note: 'felverve' }],
    })
  })

  it('shows the server’s validation errors on the fields', async () => {
    stubFetch({
      ...health,
      [searchUrl('tojas')]: () => Response.json([egg]),
      '/api/recipes': () =>
        Response.json(
          {
            status: 400,
            errors: {
              'ingredients[0].ingredientId': ['No ingredient with this id in the catalog.'],
            },
          },
          { status: 400 },
        ),
    })
    renderWithQueryClient(<App />, { route: '/recipes/new' })
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Title'), 'Rántotta')
    await user.click(screen.getByRole('button', { name: 'Add ingredient' }))
    await pickIngredient(user, 1, 'tojas', egg)
    await user.type(screen.getByRole('spinbutton', { name: 'Amount 1' }), '100')
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))

    expect(
      await screen.findByText('No ingredient with this id in the catalog.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Ingredient 1' })).toHaveAttribute(
      'aria-invalid',
      'true',
    )
    expect(screen.getByRole('alert')).toHaveTextContent('Some fields need fixing.')
  })

  it('edits an existing recipe, prefilled, and sends the whole recipe back', async () => {
    const fetchMock = stubFetch({
      ...health,
      [`/api/recipes/${nokedli.id}`]: () => Response.json(nokedli),
    })
    renderWithQueryClient(<App />, { route: `/recipes/${nokedli.id}/edit` })
    const user = userEvent.setup()

    expect(await screen.findByLabelText('Title')).toHaveValue('Nokedli')
    expect(screen.getByRole('combobox', { name: 'Ingredient 3' })).toHaveTextContent('Tojás')
    expect(screen.getByRole('spinbutton', { name: 'Amount 2' })).toHaveValue(0.5)
    expect(screen.getByRole('combobox', { name: 'Unit 2' })).toHaveValue('Tablespoon')
    expect(previewEnergy()).toHaveTextContent('Energy311 kcal')

    await user.click(screen.getByRole('button', { name: 'Remove ingredient 2' }))
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('heading', { name: 'Nokedli' })).toBeInTheDocument()
    expect(sentBody(fetchMock, 'PUT')).toEqual({
      title: 'Nokedli',
      servings: 2,
      tags: ['köret', 'magyar'],
      steps: ['Mix the dough.', 'Press it into boiling water.'],
      ingredients: [
        { ingredientId: flour.id, amount: 125, unit: 'Gram', note: null },
        { ingredientId: egg.id, amount: 1.5, unit: 'Piece', note: null },
      ],
    })
  })
})
