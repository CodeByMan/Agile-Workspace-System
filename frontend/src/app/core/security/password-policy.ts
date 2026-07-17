export const PASSWORD_MIN_LENGTH = 10;
export const PASSWORD_MAX_LENGTH = 128;

export const PASSWORD_UPPERCASE_PATTERN = /[A-Z]/;
export const PASSWORD_LOWERCASE_PATTERN = /[a-z]/;
export const PASSWORD_DIGIT_PATTERN = /[0-9]/;
export const PASSWORD_SPECIAL_PATTERN = /[^A-Za-z0-9]/;

export const PASSWORD_REQUIREMENTS_TEXT =
  'Use 10–128 characters with at least one uppercase letter, one lowercase letter, one number, and one special character.';

export type PasswordPolicyFailure =
  'required' | 'minLength' | 'maxLength' | 'uppercase' | 'lowercase' | 'digit' | 'special';

export function passwordPolicyFailures(value: string): PasswordPolicyFailure[] {
  if (!value) {
    return ['required'];
  }

  const failures: PasswordPolicyFailure[] = [];
  if (value.length < PASSWORD_MIN_LENGTH) failures.push('minLength');
  if (value.length > PASSWORD_MAX_LENGTH) failures.push('maxLength');
  if (!PASSWORD_UPPERCASE_PATTERN.test(value)) failures.push('uppercase');
  if (!PASSWORD_LOWERCASE_PATTERN.test(value)) failures.push('lowercase');
  if (!PASSWORD_DIGIT_PATTERN.test(value)) failures.push('digit');
  if (!PASSWORD_SPECIAL_PATTERN.test(value)) failures.push('special');
  return failures;
}

export function meetsPasswordPolicy(value: string): boolean {
  return passwordPolicyFailures(value).length === 0;
}
