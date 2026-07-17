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
import { catchError, of } from 'rxjs';
import { PageBreadcrumbComponent } from '../../shared/page-breadcrumb.component';
import { ApexChartComponent, ApexChartOptions } from '../../shared/apex-chart.component';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { ProjectsService } from '../../core/services/projects.service';
import { SprintsService } from '../../core/services/sprints.service';
import { TasksService } from '../../core/services/tasks.service';
import { BurndownChart } from '../../core/models/dashboard.models';
import { Project } from '../../core/models/project.models';
import { Sprint } from '../../core/models/sprint.models';
import { WorkItemSummary, WorkStatus } from '../../core/models/task.models';

@Component({
  selector: 'app-sprint-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageBreadcrumbComponent, ApexChartComponent],
  templateUrl: './sprint-page.component.html',
  styleUrl: './sprint-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SprintPageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly projectsService = inject(ProjectsService);
  private readonly sprintsService = inject(SprintsService);
  private readonly tasksService = inject(TasksService);
  private readonly dashboardService = inject(DashboardService);
  private readonly fb = inject(FormBuilder);

  readonly projects = signal<Project[]>([]);
  readonly sprints = signal<Sprint[]>([]);
  readonly selectedSprint = signal<Sprint | null>(null);
  readonly sprintItems = signal<WorkItemSummary[]>([]);
  readonly burndown = signal<BurndownChart | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly canManage = computed(() => this.auth.hasAnyRole(['Admin', 'ScrumMaster', 'Manager']));
  readonly burndownChart = signal<ApexChartOptions>({
    series: [{ name: 'Remaining Tasks', data: [] }],
    chart: { type: 'area', height: 300 },
  });

  readonly filters = this.fb.nonNullable.group({
    projectId: [''],
    sprintId: [''],
  });

  readonly sprintForm = this.fb.nonNullable.group({
    id: [0],
    name: ['', Validators.required],
    goal: [''],
    projectId: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    isClosed: [false],
  });

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.loading.set(true);
    this.projectsService.getAll().subscribe({
      next: (projects) => {
        this.projects.set(projects);
        const projectId = projects[0] ? String(projects[0].Id) : '';
        this.filters.patchValue({ projectId });
        this.sprintForm.patchValue({ projectId });
        if (projectId) {
          this.loadSprints(Number(projectId));
          this.loadBurndown(Number(projectId));
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load sprint metadata.');
        this.loading.set(false);
      },
    });
  }

  loadSprints(projectId: number): void {
    this.selectedSprint.set(null);
    this.sprintItems.set([]);
    this.sprints.set([]);
    this.filters.patchValue({ sprintId: '' });
    this.sprintsService.getAll(projectId).subscribe({
      next: (sprints) => {
        this.sprints.set(sprints);
        const active = sprints.find((sprint) => !sprint.IsClosed) ?? sprints[0] ?? null;
        if (active) {
          this.filters.patchValue({ sprintId: String(active.Id) });
          this.selectSprint(active);
        } else {
          this.startCreate();
        }
      },
      error: () => this.error.set('Unable to load sprints.'),
    });
  }

  onProjectChange(): void {
    const projectId = Number(this.filters.getRawValue().projectId || 0);
    this.selectedSprint.set(null);
    this.sprintItems.set([]);
    this.sprints.set([]);
    this.filters.patchValue({ sprintId: '' });
    if (!projectId) {
      this.burndown.set(null);
      return;
    }
    this.sprintForm.patchValue({ projectId: String(projectId) });
    this.loadSprints(projectId);
    this.loadBurndown(projectId);
  }

  selectSprint(sprint: Sprint): void {
    this.selectedSprint.set(sprint);
    this.filters.patchValue({ sprintId: String(sprint.Id) });
    this.sprintForm.patchValue({
      id: sprint.Id,
      name: sprint.Name,
      goal: sprint.Goal,
      projectId: String(sprint.ProjectId),
      startDate: sprint.StartDate.slice(0, 10),
      endDate: sprint.EndDate.slice(0, 10),
      isClosed: sprint.IsClosed,
    });

    this.tasksService.getAll({ sprintId: sprint.Id, pageNumber: 1, pageSize: 100 }).subscribe({
      next: (result) => this.sprintItems.set(result.items),
      error: () => this.sprintItems.set([]),
    });
  }

  loadBurndown(projectId: number): void {
    this.dashboardService
      .getBurndown(projectId)
      .pipe(catchError(() => of(null)))
      .subscribe((chart) => {
        this.burndown.set(chart);
        if (!chart) {
          return;
        }

        this.burndownChart.set({
          series: [
            { name: 'Remaining Tasks', data: chart.Points.map((point) => point.RemainingTasks) },
            { name: 'Completed Tasks', data: chart.Points.map((point) => point.CompletedTasks) },
          ],
          chart: { type: 'area', height: 300 },
          stroke: { curve: 'smooth', width: 3 },
          dataLabels: { enabled: false },
          xaxis: { categories: chart.Points.map((point) => point.Date.slice(5, 10)) },
          fill: { opacity: 0.16 },
          legend: { position: 'top', horizontalAlign: 'left' },
          colors: ['#1A73E8', '#34A853'],
        });
      });
  }

  startCreate(): void {
    const projectId = this.filters.getRawValue().projectId;
    this.selectedSprint.set(null);
    this.sprintForm.reset({
      id: 0,
      name: '',
      goal: '',
      projectId,
      startDate: new Date().toISOString().slice(0, 10),
      endDate: new Date().toISOString().slice(0, 10),
      isClosed: false,
    });
  }

  submit(): void {
    if (!this.canManage() || this.sprintForm.invalid || this.saving()) {
      this.sprintForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');
    const raw = this.sprintForm.getRawValue();
    const payload = {
      name: raw.name,
      goal: raw.goal,
      projectId: Number(raw.projectId),
      startDate: raw.startDate,
      endDate: raw.endDate,
      isClosed: raw.isClosed,
      rowVersion:
        this.selectedSprint()?.Id === raw.id ? this.selectedSprint()?.RowVersion : undefined,
    };

    const request = raw.id
      ? this.sprintsService.update(raw.id, payload)
      : this.sprintsService.create(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.loadSprints(Number(raw.projectId));
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        this.error.set(error.error?.Message ?? 'Unable to save sprint details.');
      },
    });
  }

  todoCount(): number {
    return this.sprintItems().filter((item) => item.Status === WorkStatus.ToDo).length;
  }

  inProgressCount(): number {
    return this.sprintItems().filter((item) => item.Status === WorkStatus.InProgress).length;
  }

  doneCount(): number {
    return this.sprintItems().filter((item) => item.Status === WorkStatus.Done).length;
  }
}
