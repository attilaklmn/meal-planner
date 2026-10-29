import { QueryClient } from '@tanstack/react-query'

// Single client for the app: all server state lives in its cache (no other global state library).
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Treat data as fresh for 30s, so navigating back and forth does not refetch everything.
      staleTime: 30_000,
    },
  },
})
