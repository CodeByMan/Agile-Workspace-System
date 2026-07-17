import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import {
  assignableRoles,
  backlogCapabilities,
  canAssignRole,
  canManageRole,
  canManageWorkItemPlanning,
} from '../src/app/core/security/role-policy.ts';
import { isApiRequest, normalizeApiBaseUrl } from '../src/app/core/security/request-policy.ts';
import { hasSessionToken, SESSION_TOKEN_KEY } from '../src/app/core/security/session-policy.ts';

const source = (relativePath: string) =>
  readFileSync(new URL(`../${relativePath}`, import.meta.url), 'utf8');

test('only Admin may assign Admin', () => {
  assert.equal(canAssignRole('Admin', 'Admin'), true);
  for (const role of ['ScrumMaster', 'Manager', 'TeamLead', 'Developer'] as const) {
    assert.equal(canAssignRole(role, 'Admin'), false);
  }
});

test('role hierarchy protects peers and higher accounts', () => {
  assert.equal(canManageRole('Manager', 'TeamLead'), true);
  assert.equal(canManageRole('Manager', 'Manager'), false);
  assert.equal(canManageRole('Manager', 'ScrumMaster'), false);
  assert.deepEqual(assignableRoles('TeamLead'), ['Developer']);
});

test('backlog capabilities match API permissions', () => {
  assert.deepEqual(backlogCapabilities('Developer'), {
    create: false,
    update: true,
    delete: false,
  });
  assert.deepEqual(backlogCapabilities('TeamLead'), {
    create: true,
    update: true,
    delete: false,
  });
  assert.deepEqual(backlogCapabilities('Manager'), {
    create: true,
    update: true,
    delete: true,
  });
  assert.equal(canManageWorkItemPlanning('Developer'), false);
  assert.equal(canManageWorkItemPlanning('TeamLead'), true);
});

test('session policy rejects missing and blank bearer tokens', () => {
  assert.equal(SESSION_TOKEN_KEY, 'agile_workspace_session_token');
  assert.equal(hasSessionToken(null), false);
  assert.equal(hasSessionToken('   '), false);
  assert.equal(hasSessionToken('token-value'), true);
});

test('API configuration supports same-origin deployment and strips a trailing slash', () => {
  assert.equal(
    normalizeApiBaseUrl('/api/', 'https://workspace.example'),
    'https://workspace.example/api',
  );
  assert.equal(
    normalizeApiBaseUrl(undefined, 'https://workspace.example'),
    'https://workspace.example/api',
  );
});

test('bearer policy attaches only to the configured API origin and path', () => {
  const origin = 'https://workspace.example';
  const apiBase = 'https://workspace.example/api';
  assert.equal(isApiRequest('/api/projects', apiBase, origin), true);
  assert.equal(isApiRequest('https://workspace.example/api', apiBase, origin), true);
  assert.equal(isApiRequest('https://workspace.example/assets/app.js', apiBase, origin), false);
  assert.equal(isApiRequest('https://evil.example/api/projects', apiBase, origin), false);
  assert.equal(isApiRequest('https://workspace.example/api-evil/projects', apiBase, origin), false);
});

test('auth interceptor enforces configured API request policy', () => {
  const interceptor = source('src/app/core/interceptors/auth.interceptor.ts');
  assert.match(interceptor, /isConfiguredApiRequest\(req\.url\)/);
  assert.match(interceptor, /Authorization: `Bearer \$\{token\}`/);
});

test('route guard redirects sessions without a token', () => {
  const guard = source('src/app/core/guards/auth.guard.ts');
  assert.match(guard, /auth\.isAuthenticated\(\)/);
  assert.match(guard, /createUrlTree\(\['\/signin'\]\)/);
});

test('team editor retains phone number and protects role and status controls', () => {
  const component = source('src/app/features/team/team-page.component.ts');
  assert.match(component, /phoneNumber:\s*user\.PhoneNumber\s*\?\?\s*''/);
  assert.match(component, /canChangeRole/);
  assert.match(component, /canToggle/);
  assert.match(component, /currentUser\(\)\?\.Id\s*!==\s*user\.Id/);
});

test('sprint project switch clears old state before loading new data', () => {
  const component = source('src/app/features/sprints/sprint-page.component.ts');
  const start = component.indexOf('onProjectChange()');
  const handler = component.slice(start, component.indexOf('  selectSprint(', start));
  assert.ok(
    handler.indexOf('this.selectedSprint.set(null)') <
      handler.indexOf('this.loadSprints(projectId)'),
  );
  assert.ok(
    handler.indexOf('this.sprintItems.set([])') < handler.indexOf('this.loadSprints(projectId)'),
  );
  assert.ok(
    handler.indexOf("this.filters.patchValue({ sprintId: '' })") <
      handler.indexOf('this.loadSprints(projectId)'),
  );
});

test('frontend handles unauthorized API failures by clearing session and redirecting', () => {
  const interceptor = source('src/app/core/interceptors/error.interceptor.ts');
  assert.match(interceptor, /error\.status === 401/);
  assert.match(interceptor, /auth\.logout\(false\)/);
  assert.match(interceptor, /router\.navigate\(\['\/signin'\]\)/);
});
