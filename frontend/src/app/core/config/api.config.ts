import { isApiRequest, normalizeApiBaseUrl } from '../security/request-policy';

export { normalizeApiBaseUrl } from '../security/request-policy';

declare global {
  interface Window {
    __AGILE_WORKSPACE_CONFIG__?: { apiBaseUrl?: string };
  }
}

export const API_BASE_URL = normalizeApiBaseUrl(
  window.__AGILE_WORKSPACE_CONFIG__?.apiBaseUrl,
  window.location.origin,
);

export function apiUrl(path: string): string {
  return `${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`;
}

export function isConfiguredApiRequest(url: string, origin = window.location.origin): boolean {
  return isApiRequest(url, API_BASE_URL, origin);
}
