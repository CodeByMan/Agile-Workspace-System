import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { NotificationSummary } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);

  get(take = 10) {
    return this.http
      .get<ApiResponse<NotificationSummary>>(apiUrl(`/notifications?take=${take}`))
      .pipe(map((response) => response.Data));
  }

  markRead(id: number) {
    return this.http.post<ApiResponse<null>>(apiUrl(`/notifications/${id}/read`), {});
  }

  markAllRead() {
    return this.http.post<ApiResponse<null>>(apiUrl('/notifications/read-all'), {});
  }
}
