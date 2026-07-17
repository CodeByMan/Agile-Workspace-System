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
import { catchError, forkJoin, of } from 'rxjs';
import { PageBreadcrumbComponent } from '../../shared/page-breadcrumb.component';
import { AuthService } from '../../core/services/auth.service';
import { ProjectsService } from '../../core/services/projects.service';
import { SprintsService } from '../../core/services/sprints.service';
import { TasksService } from '../../core/services/tasks.service';
import { UsersService } from '../../core/services/users.service';
import { Project } from '../../core/models/project.models';
import { Sprint } from '../../core/models/sprint.models';
import {
  TaskPriority,
  WorkItem,
  WorkItemSummary,
  WorkItemType,
  WorkStatus,
} from '../../core/models/task.models';
import { UserSummary } from '../../core/models/user.models';
import {
  backlogCapabilities,
  canManageWorkItemPlanning,
  isWorkspaceRole,
} from '../../core/security/role-policy';

interface SelectOption<T> {
  value: T;
  label: string;
}

@Component({
  selector: 'app-backlog-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageBreadcrumbComponent],
  templateUrl: './backlog-page.component.html',
  styleUrl: './backlog-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BacklogPageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly projectsService = inject(ProjectsService);
  private readonly usersService = inject(UsersService);
  private readonly sprintsService = inject(SprintsService);
  private readonly tasksService = inject(TasksService);

  readonly projects = signal<Project[]>([]);
  readonly users = signal<UserSummary[]>([]);
  readonly sprints = signal<Sprint[]>([]);
  readonly items = signal<WorkItemSummary[]>([]);
  readonly selectedTask = signal<WorkItem | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly capabilities = computed(() => {
    const role = this.auth.currentUser()?.Role ?? '';
    return isWorkspaceRole(role)
      ? backlogCapabilities(role)
      : { create: false, update: false, delete: false };
  });
  readonly canCreate = computed(() => this.capabilities().create);
  readonly canUpdate = computed(() => this.capabilities().update);
  readonly canDelete = computed(() => this.capabilities().delete);
  readonly canManagePlanning = computed(() => {
    const role = this.auth.currentUser()?.Role ?? '';
    return isWorkspaceRole(role) && canManageWorkItemPlanning(role);
  });

  readonly statusOptions: SelectOption<WorkStatus>[] = [
    { value: WorkStatus.ToDo, label: 'To Do' },
    { value: WorkStatus.InProgress, label: 'In Progress' },
    { value: WorkStatus.Done, label: 'Done' },
    { value: WorkStatus.Blocked, label: 'Blocked' },
  ];
  readonly priorityOptions: SelectOption<TaskPriority>[] = [
    { value: TaskPriority.Low, label: 'Low' },
    { value: TaskPriority.Medium, label: 'Medium' },
    { value: TaskPriority.High, label: 'High' },
    { value: TaskPriority.Critical, label: 'Critical' },
  ];
  readonly typeOptions: SelectOption<WorkItemType>[] = [
    { value: WorkItemType.Epic, label: 'Epic' },
    { value: WorkItemType.UserStory, label: 'User Story' },
    { value: WorkItemType.Task, label: 'Task' },
    { value: WorkItemType.Bug, label: 'Bug' },
  ];

  readonly filters = this.fb.nonNullable.group({
    projectId: [''],
    search: [''],
    status: [''],
    priority: [''],
    workItemType: [''],
  });

  readonly editorForm = this.fb.nonNullable.group({
    id: [0],
    title: ['', Validators.required],
    description: [''],
    acceptanceCriteria: [''],
    projectId: ['', Validators.required],
    sprintId: [''],
    assignedToUserId: [''],
    status: [String(WorkStatus.ToDo), Validators.required],
    priority: [String(TaskPriority.Medium), Validators.required],
    workItemType: [String(WorkItemType.Task), Validators.required],
    storyPoints: [0, Validators.required],
    startDate: [''],
    dueDate: [''],
  });

  readonly commentForm = this.fb.nonNullable.group({
    content: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loadReferenceData();
  }

  loadReferenceData(): void {
    this.loading.set(true);
    forkJoin({
      projects: this.projectsService.getAll(),
      users: this.usersService
        .getAll({ pageNumber: 1, pageSize: 100 })
        .pipe(catchError(() => of({ TotalCount: 0, PageNumber: 1, PageSize: 100, Items: [] }))),
    }).subscribe({
      next: (result) => {
        this.projects.set(result.projects);
        this.users.set(result.users.Items);
        if (!this.filters.value.projectId && result.projects[0]) {
          this.filters.patchValue({ projectId: String(result.projects[0].Id) });
          this.editorForm.patchValue({
            projectId: String(result.projects[0].Id),
          });
          this.loadSprints(result.projects[0].Id);
        }
        this.loadTasks();
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load backlog metadata.');
        this.loading.set(false);
      },
    });
  }

  loadSprints(projectId: number): void {
    this.sprintsService.getAll(projectId).subscribe({
      next: (sprints) => this.sprints.set(sprints),
      error: () => this.sprints.set([]),
    });
  }

  loadTasks(): void {
    const raw = this.filters.getRawValue();
    const projectId = this.toNumber(raw.projectId);
    if (projectId) {
      this.loadSprints(projectId);
    }

    this.tasksService
      .getAll({
        projectId,
        backlogOnly: true,
        search: raw.search || null,
        status: this.toNumber(raw.status),
        priority: this.toNumber(raw.priority),
        workItemType: this.toNumber(raw.workItemType),
        pageNumber: 1,
        pageSize: 100,
      })
      .subscribe({
        next: (result) => this.items.set(result.items),
        error: () => this.error.set('Unable to load backlog items.'),
      });
  }

  startCreate(): void {
    const projectId =
      this.filters.getRawValue().projectId ||
      (this.projects()[0] ? String(this.projects()[0].Id) : '');
    this.selectedTask.set(null);
    this.editorForm.reset({
      id: 0,
      title: '',
      description: '',
      acceptanceCriteria: '',
      projectId,
      sprintId: '',
      assignedToUserId: '',
      status: String(WorkStatus.ToDo),
      priority: String(TaskPriority.Medium),
      workItemType: String(WorkItemType.Task),
      storyPoints: 0,
      startDate: '',
      dueDate: '',
    });
    this.applyEditorPermissions(true);
    if (projectId) {
      this.loadSprints(Number(projectId));
    }
  }

  selectTask(item: WorkItemSummary): void {
    this.tasksService.getById(item.Id).subscribe({
      next: (task) => {
        this.selectedTask.set(task);
        this.editorForm.patchValue({
          id: task.Id,
          title: task.Title,
          description: task.Description,
          acceptanceCriteria: task.AcceptanceCriteria,
          projectId: String(task.ProjectId),
          sprintId: task.SprintId ? String(task.SprintId) : '',
          assignedToUserId: task.AssignedToUserId ?? '',
          status: String(task.Status),
          priority: String(task.Priority),
          workItemType: String(task.WorkItemType),
          storyPoints: task.StoryPoints,
          startDate: this.toDateInput(task.StartDate),
          dueDate: this.toDateInput(task.DueDate),
        });
        this.applyEditorPermissions(false);
        this.loadSprints(task.ProjectId);
      },
      error: () => this.error.set('Unable to load the selected backlog item.'),
    });
  }

  submit(): void {
    if (
      this.editorForm.invalid ||
      this.saving() ||
      (this.selectedTask() ? !this.canUpdate() : !this.canCreate())
    ) {
      this.editorForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');
    const raw = this.editorForm.getRawValue();
    const payload = {
      title: raw.title,
      description: raw.description,
      acceptanceCriteria: raw.acceptanceCriteria,
      workItemType: Number(raw.workItemType),
      status: Number(raw.status),
      priority: Number(raw.priority),
      storyPoints: Number(raw.storyPoints),
      startDate: raw.startDate || null,
      dueDate: raw.dueDate || null,
      isRecurring: false,
      recurrenceRule: null,
      projectId: Number(raw.projectId),
      sprintId: this.toNumber(raw.sprintId),
      assignedToUserId: raw.assignedToUserId || null,
      parentTaskId: null,
      rowVersion: this.selectedTask()?.RowVersion ?? null,
    };

    const request = raw.id
      ? this.tasksService.update(raw.id, payload)
      : this.tasksService.create(payload);
    request.subscribe({
      next: (task) => {
        this.saving.set(false);
        this.selectedTask.set(task);
        this.loadTasks();
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        this.error.set(error.error?.Message ?? 'Unable to save the backlog item.');
      },
    });
  }

  removeSelected(): void {
    const task = this.selectedTask();
    if (
      !task ||
      !this.canDelete() ||
      !confirm(`Delete ${task.Title}? This action cannot be undone.`)
    )
      return;

    this.tasksService.delete(task.Id).subscribe({
      next: () => {
        this.startCreate();
        this.loadTasks();
      },
      error: (error: HttpErrorResponse) =>
        this.error.set(error.error?.Message ?? 'Unable to delete the backlog item.'),
    });
  }

  addComment(): void {
    const task = this.selectedTask();
    if (!task || this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.tasksService.addComment(task.Id, this.commentForm.getRawValue().content).subscribe({
      next: () => {
        this.commentForm.reset();
        this.selectTask({ ...task });
      },
      error: (error: HttpErrorResponse) =>
        this.error.set(error.error?.Message ?? 'Unable to add the comment.'),
    });
  }

  statusLabel(value: WorkStatus): string {
    return this.statusOptions.find((option) => option.value === value)?.label ?? 'Unknown';
  }

  priorityLabel(value: TaskPriority): string {
    return this.priorityOptions.find((option) => option.value === value)?.label ?? 'Unknown';
  }

  typeLabel(value: WorkItemType): string {
    return this.typeOptions.find((option) => option.value === value)?.label ?? 'Unknown';
  }

  private applyEditorPermissions(isCreating: boolean): void {
    const projectControl = this.editorForm.controls.projectId;
    if (isCreating && this.canCreate()) {
      projectControl.enable({ emitEvent: false });
    } else {
      projectControl.disable({ emitEvent: false });
    }

    const planningControls = [
      this.editorForm.controls.sprintId,
      this.editorForm.controls.assignedToUserId,
      this.editorForm.controls.workItemType,
      this.editorForm.controls.priority,
      this.editorForm.controls.storyPoints,
      this.editorForm.controls.startDate,
      this.editorForm.controls.dueDate,
    ];

    for (const control of planningControls) {
      if (this.canManagePlanning()) {
        control.enable({ emitEvent: false });
      } else {
        control.disable({ emitEvent: false });
      }
    }
  }

  private toNumber(value: string): number | null {
    return value ? Number(value) : null;
  }

  private toDateInput(value: string | null): string {
    return value ? value.slice(0, 10) : '';
  }
}
