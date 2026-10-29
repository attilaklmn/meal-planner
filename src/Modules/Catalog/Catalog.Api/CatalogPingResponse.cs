namespace Catalog.Api;

public sealed record CatalogPingResponse(string Module, DateTimeOffset ServerTime);
