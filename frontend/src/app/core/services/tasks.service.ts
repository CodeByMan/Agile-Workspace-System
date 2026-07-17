import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { WorkItem, WorkItemPayload, WorkItemsResponse } from '../models/task.models';

export interface TaskFilter {
  projectId?: number | null;
  sprintId?: number | null;
  backlogOnly?: boolean;
  assignedToUserId?: string | null;
  status?: number | null;
  priority?: number | null;
  workItemType?: number | null;
  search?: string | null;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);

  getAll(filter: TaskFilter) {
    let params = new HttpParams();
    Object.entries(filter).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return this.http
      .get<ApiResponse<WorkItemsResponse>>(apiUrl('/tasks'), { params })
      .pipe(map((response) => response.Data));
  }

  getById(id: number) {
    return this.http
      .get<ApiResponse<WorkItem>>(apiUrl(`/tasks/${id}`))
      .pipe(map((response) => response.Data));
  }

  create(payload: WorkItemPayload) {
    return this.http
      .post<ApiResponse<WorkItem>>(apiUrl('/tasks'), payload)
      .pipe(map((response) => response.Data));
  }

  update(id: number, payload: WorkItemPayload) {
    return this.http
      .put<ApiResponse<WorkItem>>(apiUrl(`/tasks/${id}`), payload)
      .pipe(map((response) => response.Data));
  }

  delete(id: number) {
    return this.http.delete<ApiResponse<null>>(apiUrl(`/tasks/${id}`));
  }

  addComment(id: number, content: string) {
    return this.http.post<ApiResponse<unknown>>(apiUrl(`/tasks/${id}/comments`), { content });
  }
}
