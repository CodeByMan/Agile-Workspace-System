import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { UpdateUserPayload, UserListResult, UserSummary } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  getAll(
    options: {
      pageNumber?: number;
      pageSize?: number;
      search?: string;
      role?: string;
      isActive?: boolean | null;
    } = {},
  ) {
    let params = new HttpParams();
    Object.entries(options).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http
      .get<ApiResponse<UserListResult>>(apiUrl('/users'), { params })
      .pipe(map((response) => response.Data));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<UserSummary>>(apiUrl(`/users/${id}`))
      .pipe(map((response) => response.Data));
  }

  update(id: string, payload: UpdateUserPayload) {
    return this.http
      .put<ApiResponse<UserSummary>>(apiUrl(`/users/${id}`), payload)
      .pipe(map((response) => response.Data));
  }

  toggleStatus(id: string, isActive: boolean) {
    return this.http.patch<ApiResponse<null>>(
      apiUrl(`/users/${id}/status?isActive=${isActive}`),
      {},
    );
  }
}
