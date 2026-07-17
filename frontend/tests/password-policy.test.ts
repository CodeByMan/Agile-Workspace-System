import test from 'node:test';
import assert from 'node:assert/strict';
import {
  PASSWORD_MAX_LENGTH,
  PASSWORD_MIN_LENGTH,
  meetsPasswordPolicy,
  passwordPolicyFailures,
} from '../src/app/core/security/password-policy.ts';

test('password policy matches the backend Identity length and complexity rules', () => {
  assert.equal(PASSWORD_MIN_LENGTH, 10);
  assert.equal(PASSWORD_MAX_LENGTH, 128);
  assert.equal(meetsPasswordPolicy('Strong!Pass123'), true);
});

test('password policy rejects missing complexity categories', () => {
  assert.deepEqual(passwordPolicyFailures('lowercase!123'), ['uppercase']);
  assert.deepEqual(passwordPolicyFailures('UPPERCASE!123'), ['lowercase']);
  assert.deepEqual(passwordPolicyFailures('NoNumber!Pass'), ['digit']);
  assert.deepEqual(passwordPolicyFailures('NoSpecial123'), ['special']);
});

test('password policy rejects values outside the configured length range', () => {
  assert.ok(passwordPolicyFailures('Aa!1').includes('minLength'));
  assert.ok(passwordPolicyFailures(`Aa!1${'x'.repeat(125)}`).includes('maxLength'));
});
