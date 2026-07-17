import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { ChangePasswordPayload, Profile, UpdateProfilePayload } from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);

  getMyProfile() {
    return this.http
      .get<ApiResponse<Profile>>(apiUrl('/profile/me'))
      .pipe(map((response) => response.Data));
  }

  updateMyProfile(payload: UpdateProfilePayload) {
    return this.http
      .put<ApiResponse<Profile>>(apiUrl('/profile/me'), payload)
      .pipe(map((response) => response.Data));
  }

  changePassword(payload: ChangePasswordPayload) {
    return this.http.put<ApiResponse<null>>(apiUrl('/profile/me/password'), payload);
  }
}
