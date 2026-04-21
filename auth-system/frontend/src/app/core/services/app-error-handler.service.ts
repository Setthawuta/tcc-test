import { ErrorHandler, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AppErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    if (!environment.production) {
      console.error('[AppErrorHandler]', error);
    }
  }
}
