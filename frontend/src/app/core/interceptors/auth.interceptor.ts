import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const isApiRequest = req.url.startsWith(environment.apiUrl);
  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

  const token = authService.accessToken;
  const authorizedReq = isApiRequest && token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorizedReq).pipe(
    catchError((error: unknown) => {
      const shouldTryRefresh =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        isApiRequest &&
        !isAuthEndpoint &&
        !!authService.refreshToken;

      if (!shouldTryRefresh) {
        return throwError(() => error);
      }

      return authService.refresh().pipe(
        switchMap(() => {
          const retriedReq = req.clone({
            setHeaders: { Authorization: `Bearer ${authService.accessToken}` },
          });
          return next(retriedReq);
        }),
        catchError((refreshError: unknown) => {
          authService.logout();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
