import { usePingCatalog } from '@/api/generated/catalog/catalog'

// Calls the Catalog module through the generated client; `data` is typed from the backend's OpenAPI document.
export function CatalogPing() {
  const { data, isPending, isError, error } = usePingCatalog()

  if (isPending) {
    return <p>Catalog module: checking…</p>
  }

  if (isError) {
    return <p>Catalog module: unavailable ({error.message})</p>
  }

  return (
    <p>
      Catalog module: <strong>{data.module}</strong> answered at{' '}
      {new Date(data.serverTime).toLocaleTimeString()}
    </p>
  )
}
