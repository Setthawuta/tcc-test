import {
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  filter,
  switchMap,
  take,
  throwError,
} from 'rxjs';
import { API } from '../constants/api-endpoints';
import { APP_PATHS } from '../constants/routes';
import { AuthService } from '../services/auth.service';

let refreshing = false;
const refresh$ = new BehaviorSubject<string | null>(null);

function addToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;
}

function isAuthEndpoint(url: string): boolean {
  return (
    url.includes(API.auth.login) ||
    url.includes(API.auth.refresh) ||
    url.includes(API.auth.logout)
  );
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.getToken();
  const authedReq = addToken(req, token);

  return next(authedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthEndpoint(req.url) || !token) {
        return throwError(() => error);
      }

      if (!auth.hasRefreshToken()) {
        auth.clearSession();
        router.navigate([APP_PATHS.login]);
        return throwError(() => error);
      }

      if (refreshing) {
        return refresh$.pipe(
          filter((t): t is string => t !== null),
          take(1),
          switchMap((newToken) => next(addToken(req, newToken))),
        );
      }

      refreshing = true;
      refresh$.next(null);

      return auth.refresh().pipe(
        switchMap((response) => {
          refreshing = false;
          refresh$.next(response.accessToken);
          return next(addToken(req, response.accessToken));
        }),
        catchError((refreshErr) => {
          refreshing = false;
          auth.clearSession();
          router.navigate([APP_PATHS.login]);
          return throwError(() => refreshErr);
        }),
      );
    }),
  );
};
