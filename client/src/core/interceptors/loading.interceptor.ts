import { HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize, of, tap } from 'rxjs';
import { BusyService } from '../services/busy.service';

const cache = new Map<string, HttpResponse<unknown>>();

function shouldUseCache(req: HttpRequest<unknown>): boolean {
  // Do not cache authenticated requests to avoid cross-session data leakage.
  return req.method === 'GET' && !req.headers.has('Authorization');
}

function getCacheKey(req: HttpRequest<unknown>): string {
  // Include query params to avoid serving the wrong page/filter response.
  return req.urlWithParams;
}

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const busyService = inject(BusyService);

  // Any mutation may make previous GET cache stale.
  if (req.method !== 'GET') {
    cache.clear();
  }

  if (shouldUseCache(req)) {
    const cachedResponse = cache.get(getCacheKey(req));
    if (cachedResponse) {
      return of(cachedResponse.clone());
    }
  }

  busyService.busy();

  return next(req).pipe(
    tap((event) => {
      if (shouldUseCache(req) && event instanceof HttpResponse) {
        cache.set(getCacheKey(req), event.clone());
      }
    }),
    finalize(() => {
      busyService.idle();
    }),
  );
};
