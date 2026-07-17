export function normalizeApiBaseUrl(value: string | undefined, origin: string): string {
  const configured = (value || '/api').trim();
  const absolute = new URL(configured, origin);
  return absolute.toString().replace(/\/$/, '');
}

export function isApiRequest(url: string, apiBaseUrl: string, origin: string): boolean {
  const request = new URL(url, origin);
  const api = new URL(apiBaseUrl, origin);
  const apiPath = api.pathname.endsWith('/') ? api.pathname : `${api.pathname}/`;
  return (
    request.origin === api.origin &&
    (request.pathname === api.pathname || request.pathname.startsWith(apiPath))
  );
}
