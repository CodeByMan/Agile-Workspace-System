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
import { HttpErrorResponse } from '@angular/common/http';
import { PageBreadcrumbComponent } from '../../shared/page-breadcrumb.component';
import { AuthService } from '../../core/services/auth.service';
import { ProjectsService } from '../../core/services/projects.service';
import { Project } from '../../core/models/project.models';

@Component({
  selector: 'app-projects-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageBreadcrumbComponent],
  templateUrl: './projects-page.component.html',
  styleUrl: './projects-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsPageComponent implements OnInit {
  private readonly projectsService = inject(ProjectsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly projects = signal<Project[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly selectedProjectId = signal<number | null>(null);
  readonly canManage = computed(() => this.auth.hasAnyRole(['Admin', 'ScrumMaster', 'Manager']));
  readonly canDelete = computed(() => this.auth.hasAnyRole(['Admin']));

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    startDate: ['', Validators.required],
    endDate: [''],
    isArchived: [false],
  });

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.loading.set(true);
    this.projectsService.getAll().subscribe({
      next: (projects) => {
        this.projects.set(projects);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load projects.');
        this.loading.set(false);
      },
    });
  }

  edit(project: Project): void {
    this.selectedProjectId.set(project.Id);
    this.form.patchValue({
      name: project.Name,
      description: project.Description,
      startDate: this.toDateInput(project.StartDate),
      endDate: this.toDateInput(project.EndDate),
      isArchived: project.IsArchived,
    });
  }

  startCreate(): void {
    this.selectedProjectId.set(null);
    this.form.reset({
      name: '',
      description: '',
      startDate: this.toDateInput(new Date().toISOString()),
      endDate: '',
      isArchived: false,
    });
  }

  submit(): void {
    if (!this.canManage() || this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');
    const raw = this.form.getRawValue();
    const payload = {
      name: raw.name,
      description: raw.description,
      startDate: raw.startDate,
      endDate: raw.endDate || null,
      isArchived: raw.isArchived,
      rowVersion: this.projects().find((project) => project.Id === this.selectedProjectId())
        ?.RowVersion,
    };

    const request = this.selectedProjectId()
      ? this.projectsService.update(this.selectedProjectId()!, payload)
      : this.projectsService.create(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.startCreate();
        this.loadProjects();
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        this.error.set(error.error?.Message ?? 'Unable to save the project.');
      },
    });
  }

  remove(project: Project): void {
    if (!this.canDelete() || !confirm(`Delete ${project.Name}? This action cannot be undone.`)) {
      return;
    }

    this.projectsService.delete(project.Id).subscribe({
      next: () => this.loadProjects(),
      error: (error: HttpErrorResponse) => {
        this.error.set(error.error?.Message ?? 'Unable to delete the project.');
      },
    });
  }

  progress(project: Project): number {
    if (!project.TotalTasks) {
      return 0;
    }
    return Math.round((project.CompletedTasks / project.TotalTasks) * 100);
  }

  private toDateInput(value: string | null): string {
    return value ? value.slice(0, 10) : '';
  }
}
