// Every generated request goes through this function (Orval "mutator").
// Plain fetch resolves on 4xx/5xx too; throwing here is what puts TanStack Query into its error state.

export class ApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(status: number, body: unknown) {
    super(`API request failed with HTTP ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

// Orval picks this up as the error type of every generated hook (TError), instead of `unknown`.
export type ErrorType<_Body> = ApiError

export async function apiFetch<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init)

  const text = [204, 205, 304].includes(response.status) ? '' : await response.text()
  const body: unknown = text ? JSON.parse(text) : undefined

  if (!response.ok) {
    throw new ApiError(response.status, body)
  }

  return body as T
}
