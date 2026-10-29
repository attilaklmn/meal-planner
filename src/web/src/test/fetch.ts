import { vi } from 'vitest'

type Route = () => Response | Promise<Response>

// Replaces global fetch with a URL -> response map; unexpected requests fail the test loudly.
// Enough while there are only a couple of endpoints; MSW takes over once the API grows.
export function stubFetch(routes: Record<string, Route>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
    const route = routes[url]

    if (!route) {
      throw new Error(`Unexpected request in test: ${url}`)
    }

    return route()
  })

  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}
