export const SESSION_TOKEN_KEY = 'agile_workspace_session_token';

export function hasSessionToken(token: string | null | undefined): boolean {
  return typeof token === 'string' && token.trim().length > 0;
}
