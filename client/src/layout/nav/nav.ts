import { HttpErrorResponse } from '@angular/common/http';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  DOCUMENT,
  inject,
  model,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountService } from '../../core/services/account.service';
import { BusyService } from '../../core/services/busy.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../environments/environment';
import { themes } from '../theme';

@Component({
  selector: 'app-nav',
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Nav {
  private accountService = inject(AccountService);
  protected busyService = inject(BusyService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  private document = inject(DOCUMENT);

  protected selectedTheme = signal<string>('light');
  protected themes = themes;
  protected showDebugRoutes = !environment.production;

  protected email = model('');
  protected password = model('');
  protected isLoading = signal(false);
  protected readonly currentUser = this.accountService.currentUser;

  constructor() {
    afterNextRender(() => {
      const savedTheme = localStorage.getItem('theme') || 'light';
      this.selectedTheme.set(savedTheme);
      this.applyThemeToDOM(savedTheme);
    });
  }

  handleSelectTheme(theme: string) {
    this.selectedTheme.set(theme);
    localStorage.setItem('theme', theme);
    this.applyThemeToDOM(theme);

    const activeElement = this.document.activeElement as HTMLElement;
    if (activeElement) {
      activeElement.blur();
    }
  }

  private applyThemeToDOM(theme: string) {
    this.document.documentElement.setAttribute('data-theme', theme);
  }

  login() {
    if (this.isLoading()) return;

    this.isLoading.set(true);
    const creds = { email: this.email(), password: this.password() };

    this.accountService
      .login(creds)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.email.set('');
          this.password.set('');
          this.router.navigateByUrl('/members');
          this.toast.success('Logged in successfully');
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.message ?? 'Login failed. Please try again.';
          this.toast.error(message);
        },
      });
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}
