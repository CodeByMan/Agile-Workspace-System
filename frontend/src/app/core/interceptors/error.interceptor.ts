import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAnonymousAccountRequest =
        req.url.endsWith('/account/login') || req.url.endsWith('/account/register');

      if (error.status === 401 && !isAnonymousAccountRequest) {
        auth.logout(false);
        void router.navigate(['/signin']);
      }

      return throwError(() => error);
    }),
  );
};
