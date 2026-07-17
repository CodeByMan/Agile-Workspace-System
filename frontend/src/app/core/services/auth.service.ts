import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, of, tap } from 'rxjs';
import { apiUrl } from '../config/api.config';
import { ApiResponse } from '../models/api.models';
import { AuthUser, LoginRequest, RegisterRequest } from '../models/auth.models';
import { hasSessionToken, SESSION_TOKEN_KEY } from '../security/session-policy';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storageKey = SESSION_TOKEN_KEY;
  readonly currentUser = signal<AuthUser | null>(null);
  readonly isHydrated = signal(false);

  login(payload: LoginRequest) {
    return this.http.post<ApiResponse<AuthUser>>(apiUrl('/account/login'), payload).pipe(
      map((r) => this.extractUser(r)),
      tap((u) => this.setSession(u)),
    );
  }
  register(payload: RegisterRequest) {
    return this.http.post<ApiResponse<AuthUser>>(apiUrl('/account/register'), payload).pipe(
      map((r) => this.extractUser(r)),
      tap((u) => this.setSession(u)),
    );
  }
  hydrate(): void {
    if (this.isHydrated()) return;
    const token = this.getToken();
    if (!token) {
      this.isHydrated.set(true);
      return;
    }
    this.http
      .get<ApiResponse<AuthUser>>(apiUrl('/account/me'))
      .pipe(
        map((r) => this.extractUser(r, false)),
        catchError(() => {
          this.clearSession(false);
          return of(null);
        }),
      )
      .subscribe((user) => {
        if (user) this.currentUser.set({ ...user, Token: token });
        this.isHydrated.set(true);
      });
  }
  logout(redirect = true): void {
    this.clearSession(redirect);
  }
  getToken(): string | null {
    return sessionStorage.getItem(this.storageKey);
  }
  isAuthenticated(): boolean {
    return hasSessionToken(this.getToken());
  }
  hasAnyRole(roles: string[]): boolean {
    const role = this.currentUser()?.Role;
    return !!role && roles.includes(role);
  }
  private extractUser(response: ApiResponse<AuthUser>, requireToken = true): AuthUser {
    const user = response.Data;
    if (!user) throw new Error('Authentication response did not include a user payload.');
    if (requireToken && !user.Token)
      throw new Error('Authentication response did not include a token.');
    return user;
  }
  private setSession(user: AuthUser): void {
    if (!user.Token) throw new Error('Cannot create session without a token.');
    sessionStorage.setItem(this.storageKey, user.Token);
    this.currentUser.set(user);
    this.isHydrated.set(true);
  }
  private clearSession(redirect: boolean): void {
    sessionStorage.removeItem(this.storageKey);
    this.currentUser.set(null);
    this.isHydrated.set(true);
    if (redirect) void this.router.navigate(['/signin']);
  }
}
