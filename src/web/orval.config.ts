import { defineConfig } from 'orval'

export default defineConfig({
  api: {
    // Written by `dotnet build` of src/Api, so generation needs no running backend.
    input: '../Api/Api.json',
    output: {
      // One file per OpenAPI tag, i.e. per backend module (Catalog, Recipes, Planning).
      mode: 'tags-split',
      target: 'src/api/generated',
      schemas: 'src/api/generated/model',
      client: 'react-query',
      // Native fetch: no axios dependency. Paths stay relative (/api/...), the Vite proxy forwards them.
      httpClient: 'fetch',
      clean: true,
      override: {
        // Our fetcher throws on non-2xx responses, so failed requests become query errors.
        mutator: { path: 'src/api/fetcher.ts', name: 'apiFetch' },
        // Hooks return the response body itself instead of a { data, status, headers } wrapper.
        fetch: { includeHttpResponseReturnType: false },
      },
    },
  },
})
