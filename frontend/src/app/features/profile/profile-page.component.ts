import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { PageBreadcrumbComponent } from '../../shared/page-breadcrumb.component';
import { Profile } from '../../core/models/profile.models';
import { ProfileService } from '../../core/services/profile.service';
import {
  PASSWORD_DIGIT_PATTERN,
  PASSWORD_LOWERCASE_PATTERN,
  PASSWORD_MAX_LENGTH,
  PASSWORD_MIN_LENGTH,
  PASSWORD_REQUIREMENTS_TEXT,
  PASSWORD_SPECIAL_PATTERN,
  PASSWORD_UPPERCASE_PATTERN,
} from '../../core/security/password-policy';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageBreadcrumbComponent],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePageComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly fb = inject(FormBuilder);

  readonly profile = signal<Profile | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly passwordSaving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly passwordMessage = signal('');
  readonly passwordRequirements = PASSWORD_REQUIREMENTS_TEXT;

  readonly profileForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    jobTitle: [''],
    phoneNumber: [''],
  });

  readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: [
      '',
      [
        Validators.required,
        Validators.minLength(PASSWORD_MIN_LENGTH),
        Validators.maxLength(PASSWORD_MAX_LENGTH),
        Validators.pattern(PASSWORD_UPPERCASE_PATTERN),
        Validators.pattern(PASSWORD_LOWERCASE_PATTERN),
        Validators.pattern(PASSWORD_DIGIT_PATTERN),
        Validators.pattern(PASSWORD_SPECIAL_PATTERN),
      ],
    ],
    confirmPassword: [
      '',
      [
        Validators.required,
        Validators.minLength(PASSWORD_MIN_LENGTH),
        Validators.maxLength(PASSWORD_MAX_LENGTH),
        Validators.pattern(PASSWORD_UPPERCASE_PATTERN),
        Validators.pattern(PASSWORD_LOWERCASE_PATTERN),
        Validators.pattern(PASSWORD_DIGIT_PATTERN),
        Validators.pattern(PASSWORD_SPECIAL_PATTERN),
      ],
    ],
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.profileService.getMyProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm.patchValue({
          fullName: profile.FullName,
          email: profile.Email,
          jobTitle: profile.JobTitle ?? '',
          phoneNumber: profile.PhoneNumber ?? '',
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load your profile.');
        this.loading.set(false);
      },
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid || this.saving()) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.success.set('');

    const raw = this.profileForm.getRawValue();
    this.profileService
      .updateMyProfile({
        fullName: raw.fullName,
        email: raw.email,
        jobTitle: raw.jobTitle || null,
        phoneNumber: raw.phoneNumber || null,
      })
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.saving.set(false);
          this.success.set('Profile updated successfully.');
        },
        error: (error: HttpErrorResponse) => {
          this.saving.set(false);
          this.error.set(error.error?.Message ?? 'Unable to update your profile.');
        },
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid || this.passwordSaving()) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const raw = this.passwordForm.getRawValue();
    if (raw.newPassword !== raw.confirmPassword) {
      this.passwordMessage.set('Password confirmation does not match.');
      return;
    }

    this.passwordSaving.set(true);
    this.passwordMessage.set('');

    this.profileService
      .changePassword({
        currentPassword: raw.currentPassword,
        newPassword: raw.newPassword,
      })
      .subscribe({
        next: () => {
          this.passwordSaving.set(false);
          this.passwordMessage.set('Password updated successfully.');
          this.passwordForm.reset();
        },
        error: (error: HttpErrorResponse) => {
          this.passwordSaving.set(false);
          this.passwordMessage.set(error.error?.Message ?? 'Unable to change your password.');
        },
      });
  }
}
