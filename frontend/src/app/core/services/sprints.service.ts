import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { Sprint, SprintPayload } from '../models/sprint.models';

@Injectable({ providedIn: 'root' })
export class SprintsService {
  private readonly http = inject(HttpClient);

  getAll(projectId?: number | null) {
    const suffix = projectId ? `?projectId=${projectId}` : '';
    return this.http
      .get<ApiResponse<Sprint[]>>(apiUrl(`/sprints${suffix}`))
      .pipe(map((response) => response.Data));
  }

  create(payload: SprintPayload) {
    return this.http.post<ApiResponse<{ id: number }>>(apiUrl('/sprints'), payload);
  }

  update(id: number, payload: SprintPayload & { isClosed: boolean }) {
    return this.http.put<ApiResponse<{ id: number }>>(apiUrl(`/sprints/${id}`), payload);
  }
}
