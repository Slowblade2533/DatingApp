import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { Router } from '@angular/router';
import { AccountService } from '../services/account.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);
  const accountService = inject(AccountService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error) {
        switch (error.status) {
          case 400:
            // 1. แก้ไขการสะสม Model State Errors ให้ครบทุกตัวก่อน throw
            if (error.error.errors) {
              const modelStateErrors: any = [];
              for (const key in error.error.errors) {
                if (error.error.errors[key]) {
                  modelStateErrors.push(error.error.errors[key]);
                }
              }
              // ✅ คืนค่าเป็น Observable Error พร้อม Array ที่แบนแล้ว (Flattened)
              return throwError(() => modelStateErrors.flat());
            }
            // 2. จัดการกรณี Error 400 แบบทั่วไปที่ไม่ใช่ Validation
            {
              const errorMessage = typeof error.error === 'string' ? error.error : 'Bad Request';
              toast.error(errorMessage, 5000);
            }
            break;

          case 401:
            {
              const isLoginRequest =
                req.url.includes('account/login') || req.url.includes('account/register');
              const isSessionEndpoint =
                req.url.includes('account/me') ||
                req.url.includes('account/refresh') ||
                req.url.includes('account/logout');

              if (isLoginRequest) {
                toast.error('Email or Password incorrect.');
              } else if (!isSessionEndpoint && accountService.currentUser()) {
                // Cookie-first flow: ask SharedWorker to refresh silently before forcing logout.
                accountService.requestTokenRefresh();
              }
            }
            break;
          case 404:
            router.navigateByUrl('/not-found');
            break;
          case 500:
            toast.error('Server error, please try again later.');
            break;
          case 0:
            break;
          default:
            toast.error('Something went wrong');
            break;
        }
      }
      return throwError(() => error);
    }),
  );
};
