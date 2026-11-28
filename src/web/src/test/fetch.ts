import { vi } from 'vitest'

// A route gets the request's init, so one URL can answer GET, PUT and DELETE differently.
type Route = (init?: RequestInit) => Response | Promise<Response>

// Replaces global fetch with a URL -> response map; unexpected requests fail the test loudly.
// Enough while there are only a couple of endpoints; MSW takes over once the API grows.
export function stubFetch(routes: Record<string, Route>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
    const route = routes[url]

    if (!route) {
      throw new Error(`Unexpected request in test: ${url}`)
    }

    return route(init)
  })

  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}
