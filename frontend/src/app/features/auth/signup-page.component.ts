import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  PASSWORD_DIGIT_PATTERN,
  PASSWORD_LOWERCASE_PATTERN,
  PASSWORD_MAX_LENGTH,
  PASSWORD_MIN_LENGTH,
  PASSWORD_REQUIREMENTS_TEXT,
  PASSWORD_SPECIAL_PATTERN,
  PASSWORD_UPPERCASE_PATTERN,
} from '../../core/security/password-policy';
import { AuthService } from '../../core/services/auth.service';

const passwordValidators = [
  Validators.required,
  Validators.minLength(PASSWORD_MIN_LENGTH),
  Validators.maxLength(PASSWORD_MAX_LENGTH),
  Validators.pattern(PASSWORD_UPPERCASE_PATTERN),
  Validators.pattern(PASSWORD_LOWERCASE_PATTERN),
  Validators.pattern(PASSWORD_DIGIT_PATTERN),
  Validators.pattern(PASSWORD_SPECIAL_PATTERN),
];

@Component({
  selector: 'app-signup-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './signup-page.component.html',
  styleUrls: ['./signup-page.component.scss', './auth-page.shared.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignupPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly passwordRequirements = PASSWORD_REQUIREMENTS_TEXT;

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
    fullName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(160)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', passwordValidators],
    confirmPassword: ['', passwordValidators],
  });

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    if (raw.password !== raw.confirmPassword) {
      this.error.set('Password confirmation does not match.');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.auth
      .register({
        username: raw.username.trim(),
        fullName: raw.fullName.trim(),
        email: raw.email.trim(),
        password: raw.password,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          void this.router.navigate(['/dashboard']);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(error.error?.Message ?? 'Unable to create your account right now.');
        },
      });
  }
}
