import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, tap, throwError, timer } from 'rxjs';
import { NetworkStatusService } from '../services/network-status.service';
import { environment } from '../../environments/environment';

const RETRYABLE_STATUSES = new Set([408, 429, 502, 503, 504]);
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS']);

function shouldRetry(req: HttpRequest<unknown>, error: HttpErrorResponse): boolean {
  if (error.status === 0) return false;
  return SAFE_METHODS.has(req.method) && RETRYABLE_STATUSES.has(error.status);
}

export const networkResilienceInterceptor: HttpInterceptorFn = (req, next) => {
  const network = inject(NetworkStatusService);
  const isApiRequest = req.url.startsWith(environment.apiUrl);

  return next(req).pipe(
    retry({
      count: 3,
      delay: (error: HttpErrorResponse, retryCount: number) => {
        if (!shouldRetry(req, error)) return throwError(() => error);
        return timer(Math.pow(2, retryCount - 1) * 1000);
      },
    }),
    tap({
      next: () => {},
    }),
    catchError((error: HttpErrorResponse) => {
      if (isApiRequest && (error.status === 0 || error.status >= 500)) {
        network.recordHttpError(error.status); // ✅ ต้องมั่นใจว่าบรรทัดนี้ทำงาน
      }
      return throwError(() => error);
    }),
  );
};
