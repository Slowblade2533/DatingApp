import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AccountService } from '../services/account.service';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);
  const currentUser = accountService.currentUser();

  // ✅ ตรวจสอบว่าเป็น request ไปยัง API ของเราเท่านั้น
  const isApiRequest = req.url.startsWith(environment.apiUrl);

  if (currentUser?.token && isApiRequest) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${currentUser.token}`,
      },
    });
  }

  return next(req);
};
