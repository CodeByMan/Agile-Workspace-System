import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { isConfiguredApiRequest } from '../config/api.config';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  if (!token || !isConfiguredApiRequest(req.url)) return next(req);
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
