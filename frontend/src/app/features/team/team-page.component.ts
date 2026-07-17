import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { UserSummary } from '../../core/models/user.models';
import {
  ROLES,
  WorkspaceRole,
  assignableRoles,
  canManageRole,
  isWorkspaceRole,
} from '../../core/security/role-policy';
import { UsersService } from '../../core/services/users.service';
import { PageBreadcrumbComponent } from '../../shared/page-breadcrumb.component';

@Component({
  selector: 'app-team-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageBreadcrumbComponent],
  templateUrl: './team-page.component.html',
  styleUrl: './team-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPageComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  readonly users = signal<UserSummary[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly selectedUser = signal<UserSummary | null>(null);
  readonly roles = ROLES;
  readonly availableRoles = computed(() => {
    const role = this.actorRole();
    const selected = this.selectedUser();
    if (!role) return [];
    const allowed = [...assignableRoles(role)];
    if (selected && isWorkspaceRole(selected.Role) && !allowed.includes(selected.Role))
      allowed.push(selected.Role);
    return allowed;
  });
  readonly filters = this.fb.nonNullable.group({ search: [''], role: [''] });
  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(160)]],
    email: ['', [Validators.required, Validators.email]],
    role: ['Developer', Validators.required],
    jobTitle: [''],
    phoneNumber: [''],
  });
  ngOnInit(): void {
    this.loadUsers();
  }
  loadUsers(): void {
    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.usersService
      .getAll({ pageNumber: 1, pageSize: 100, search: raw.search, role: raw.role || '' })
      .subscribe({
        next: (r) => {
          this.users.set(r.Items);
          this.loading.set(false);
        },
        error: (e: HttpErrorResponse) => {
          this.error.set(e.error?.Message ?? 'Unable to load team members.');
          this.loading.set(false);
        },
      });
  }
  select(user: UserSummary): void {
    if (!this.canEdit(user)) return;
    this.selectedUser.set(user);
    this.form.patchValue({
      fullName: user.FullName,
      email: user.Email,
      role: user.Role,
      jobTitle: user.JobTitle ?? '',
      phoneNumber: user.PhoneNumber ?? '',
    });
  }
  canEdit(user: UserSummary): boolean {
    const actor = this.actorRole();
    const me = this.auth.currentUser();
    if (!actor || !isWorkspaceRole(user.Role)) return false;
    return me?.Id === user.Id || canManageRole(actor, user.Role);
  }
  canChangeRole(user: UserSummary | null): boolean {
    return !!user && this.auth.currentUser()?.Id !== user.Id && this.canEdit(user);
  }
  canToggle(user: UserSummary): boolean {
    return this.auth.currentUser()?.Id !== user.Id && this.canEdit(user);
  }
  submit(): void {
    const selected = this.selectedUser();
    if (!selected || this.form.invalid || this.saving() || !this.canEdit(selected)) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    if (!this.canChangeRole(selected) && raw.role !== selected.Role) {
      this.error.set('You cannot change your own role.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    this.usersService
      .update(selected.Id, {
        fullName: raw.fullName.trim(),
        email: raw.email.trim(),
        role: raw.role,
        jobTitle: raw.jobTitle || null,
        phoneNumber: raw.phoneNumber || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.selectedUser.set(null);
          this.loadUsers();
        },
        error: (e: HttpErrorResponse) => {
          this.saving.set(false);
          this.error.set(e.error?.Message ?? e.error?.message ?? 'Unable to update the user.');
        },
      });
  }
  toggleActive(user: UserSummary): void {
    if (
      !this.canToggle(user) ||
      !confirm(`${user.IsActive ? 'Deactivate' : 'Activate'} ${user.FullName}?`)
    )
      return;
    this.usersService.toggleStatus(user.Id, !user.IsActive).subscribe({
      next: () => this.loadUsers(),
      error: (e: HttpErrorResponse) =>
        this.error.set(e.error?.Message ?? 'Unable to update user status.'),
    });
  }
  private actorRole(): WorkspaceRole | null {
    const role = this.auth.currentUser()?.Role ?? '';
    return isWorkspaceRole(role) ? role : null;
  }
}
