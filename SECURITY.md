# Security Policy

## Supported version

Security fixes are applied to the current default branch. Older snapshots and forks are not maintained.

## Reporting a vulnerability

Do not open a public issue containing exploit details, credentials, tokens, personal data, or database dumps. Contact the repository owner privately with:

- affected commit or version;
- reproducible steps;
- expected and observed behavior;
- impact assessment;
- suggested mitigation, when available.

The maintainer will acknowledge a complete report, investigate it, and coordinate a fix before public disclosure.

## Secret handling

This repository must never contain deployable JWT signing keys, SQL Server passwords, GitHub tokens, exported .NET user secrets, or populated `.env` files. Rotate any credential immediately if it is accidentally committed, then remove it from Git history.

## Authentication design notes

Authentication uses short-lived JWT bearer tokens. Every authenticated request validates that the account still exists, is active, and has the same ASP.NET Core Identity security stamp as the token. Password, role, and account-status changes update that stamp and invalidate older tokens.

The browser stores the bearer token in `sessionStorage`; this reduces persistence but does not eliminate XSS risk. The frontend attaches the token only to the configured API origin. Deploy behind HTTPS and retain the supplied Content Security Policy and related response headers.

Nginx access logging is disabled for the SignalR proxy path so a WebSocket `access_token` query parameter is not written to the proxy access log.

## Operational limitations

The current portfolio implementation intentionally does not include refresh tokens, multi-factor authentication, email verification, forgot-password delivery, or server-side token revocation on ordinary logout. Browser logout removes the token from `sessionStorage`; security-sensitive account changes invalidate existing tokens by changing the Identity security stamp.

Automatic EF Core migrations are disabled by default through `Database:ApplyMigrationsOnStartup=false`. Enable them only for controlled local Development or portfolio Docker use, or apply migrations explicitly as part of deployment.

API audit logging is configurable, excludes low-value health/Swagger/static paths, redacts sensitive values, omits authentication payloads and files, and does not capture response bodies by default. Review and schedule `docs/sql/purge-old-audit-logs.sql` according to the deployment's retention requirements.
