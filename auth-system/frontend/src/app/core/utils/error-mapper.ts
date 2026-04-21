import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, ProblemDetails } from '../models/api-error.model';

export function toApiError(err: unknown): ApiError {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ProblemDetails | string | null;
    if (typeof body === 'object' && body !== null) {
      return {
        status: err.status,
        title: body.title ?? err.statusText ?? 'Error',
        detail: body.detail,
        errors: body.errors,
        raw: err,
      };
    }
    return {
      status: err.status,
      title: err.statusText || 'Error',
      detail: typeof body === 'string' ? body : undefined,
      raw: err,
    };
  }
  return {
    status: 0,
    title: 'Unknown error',
    detail: err instanceof Error ? err.message : String(err),
    raw: err,
  };
}

export function isNetworkError(err: ApiError): boolean {
  return err.status === 0;
}
