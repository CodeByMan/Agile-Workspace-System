import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { Project, ProjectPayload } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);

  getAll() {
    return this.http
      .get<ApiResponse<Project[]>>(apiUrl('/projects'))
      .pipe(map((response) => response.Data));
  }

  create(payload: ProjectPayload) {
    return this.http
      .post<ApiResponse<Project>>(apiUrl('/projects'), payload)
      .pipe(map((response) => response.Data));
  }

  update(id: number, payload: ProjectPayload) {
    return this.http
      .put<ApiResponse<Project>>(apiUrl(`/projects/${id}`), payload)
      .pipe(map((response) => response.Data));
  }

  delete(id: number) {
    return this.http.delete<ApiResponse<null>>(apiUrl(`/projects/${id}`));
  }
}
