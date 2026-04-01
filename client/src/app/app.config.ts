import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withRouterConfig, withViewTransitions } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { errorInterceptor } from '../core/interceptors/error.interceptor';
import { authInterceptor } from '../core/interceptors/auth.interceptor';
import { networkResilienceInterceptor } from '../core/interceptors/network-resilience.interceptor';
import { AccountService } from '../core/services/account.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(
      routes,
      withViewTransitions(),
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
    ),
    provideHttpClient(
      withInterceptors([networkResilienceInterceptor, authInterceptor, errorInterceptor]),
    ),
    provideAppInitializer(() => {
      const accountService = inject(AccountService);
      return Promise.resolve();
    }),
  ],
};
