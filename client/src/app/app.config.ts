import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withViewTransitions,
} from '@angular/router';

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';

import { authInterceptor } from '../core/interceptors/auth.interceptor';
import { errorInterceptor } from '../core/interceptors/error.interceptor';
import { loadingInterceptor } from '../core/interceptors/loading.interceptor';
import { networkResilienceInterceptor } from '../core/interceptors/network-resilience.interceptor';
import { AccountService } from '../core/services/account.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(
      routes,
      withViewTransitions(),
      withComponentInputBinding(),
    ),
    provideHttpClient(
      withInterceptors([
        networkResilienceInterceptor,
        authInterceptor,
        errorInterceptor,
        loadingInterceptor,
      ]),
    ),
    provideAppInitializer(() => {
      inject(AccountService);
    }),
  ],
};
