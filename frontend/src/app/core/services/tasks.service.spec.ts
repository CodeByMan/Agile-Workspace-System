import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { apiUrl } from '../config/api.config';
import { TaskPriority, WorkItemPayload, WorkItemType, WorkStatus } from '../models/task.models';
import { TasksService } from './tasks.service';

describe('TasksService', () => {
  let service: TasksService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TasksService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TasksService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    TestBed.resetTestingModule();
  });

  it('sends API-compatible numeric status and priority values when creating work items', () => {
    const payload: WorkItemPayload = {
      title: 'Verify enum contract',
      description: 'The request must use values accepted by the ASP.NET API.',
      acceptanceCriteria: 'Status and priority are serialized with the shared numeric contract.',
      workItemType: WorkItemType.Task,
      status: WorkStatus.ToDo,
      priority: TaskPriority.Medium,
      startDate: null,
      dueDate: null,
      storyPoints: 3,
      isRecurring: false,
      recurrenceRule: null,
      projectId: 7,
      sprintId: null,
      assignedToUserId: null,
      parentTaskId: null,
    };

    service.create(payload).subscribe();

    const request = httpTesting.expectOne(apiUrl('/tasks'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body.status).toBe(1);
    expect(request.request.body.priority).toBe(2);
    request.flush({ Success: true, Message: null, Data: {}, Errors: null });
  });

  it('serializes status and priority filters using the shared API values', () => {
    service
      .getAll({
        status: WorkStatus.Blocked,
        priority: TaskPriority.Critical,
        pageNumber: 1,
        pageSize: 20,
      })
      .subscribe();

    const request = httpTesting.expectOne((candidate) => candidate.url === apiUrl('/tasks'));
    expect(request.request.params.get('status')).toBe('4');
    expect(request.request.params.get('priority')).toBe('4');
    request.flush({
      Success: true,
      Message: null,
      Data: { totalCount: 0, pageNumber: 1, pageSize: 20, items: [] },
      Errors: null,
    });
  });
});
