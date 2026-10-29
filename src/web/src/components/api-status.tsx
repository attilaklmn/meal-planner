import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'

// /health is not part of the OpenAPI description, so this is the one hand-written request.
// Everything under /api goes through the generated client once it exists.
async function fetchApiHealth(): Promise<string> {
  const response = await fetch('/health')

  // 503 + "Unhealthy" means the Api answered but a dependency (e.g. the database) is down.
  if (!response.ok && response.status !== 503) {
    throw new Error(`Health check failed with HTTP ${response.status}`)
  }

  return response.text()
}

export function ApiStatus() {
  const { data, isPending, isError, isFetching, refetch } = useQuery({
    queryKey: ['api-health'],
    queryFn: fetchApiHealth,
  })

  const status = isPending ? 'Checking…' : isError ? 'Unreachable' : data

  return (
    <div className="flex items-center gap-3">
      <span>
        API: <strong>{status}</strong>
      </span>
      <Button variant="outline" size="sm" onClick={() => void refetch()} disabled={isFetching}>
        Refresh
      </Button>
    </div>
  )
}
