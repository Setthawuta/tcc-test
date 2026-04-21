import { inject } from '@angular/core';
import { firstValueFrom, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export function authInitializer(): Promise<unknown> {
  const auth = inject(AuthService);

  if (auth.hasValidToken()) {
    return firstValueFrom(
      auth.loadCurrentUser().pipe(catchError(() => of(null))),
    );
  }

  if (auth.hasRefreshToken()) {
    return firstValueFrom(
      auth.refresh().pipe(
        catchError(() => {
          auth.clearSession();
          return of(null);
        }),
      ),
    );
  }

  auth.clearSession();
  return Promise.resolve();
}
