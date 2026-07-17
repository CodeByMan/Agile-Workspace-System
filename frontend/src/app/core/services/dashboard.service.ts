import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import {
  BurndownChart,
  DashboardSummary,
  ProjectProgress,
  RecentActivity,
  UserProductivity,
} from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getSummary() {
    return this.http
      .get<ApiResponse<DashboardSummary>>(apiUrl('/reporting/dashboard'))
      .pipe(map((response) => response.Data));
  }

  getProjectProgress() {
    return this.http
      .get<ApiResponse<ProjectProgress[]>>(apiUrl('/reporting/projects'))
      .pipe(map((response) => response.Data));
  }

  getRecentActivity(take = 12) {
    return this.http
      .get<ApiResponse<RecentActivity[]>>(apiUrl(`/reporting/activity?take=${take}`))
      .pipe(map((response) => response.Data));
  }

  getUserProductivity() {
    return this.http
      .get<ApiResponse<UserProductivity[]>>(apiUrl('/reporting/users/productivity'))
      .pipe(map((response) => response.Data));
  }

  getBurndown(projectId: number) {
    return this.http
      .get<ApiResponse<BurndownChart>>(apiUrl(`/reporting/projects/${projectId}/burndown`))
      .pipe(map((response) => response.Data));
  }
}
