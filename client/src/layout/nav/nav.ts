import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, model, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountService } from '../../core/services/account.service';
import { ToastService } from '../../core/services/toast.service';
import { themes } from '../theme';

@Component({
  selector: 'app-nav',
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav implements OnInit {
  private accountService = inject(AccountService);
  private router = inject(Router);
  private toast = inject(ToastService);
  protected selectedTheme = signal<string>(localStorage.getItem('theme') || 'light');
  protected themes = themes;

  ngOnInit(): void {
    document.documentElement.setAttribute('data-theme', this.selectedTheme());
  }

  handleSelectTheme(theme: string) {
    this.selectedTheme.set(theme);
    localStorage.setItem('theme', theme);
    document.documentElement.setAttribute('data-theme', theme);
    const element = document.activeElement as HTMLDivElement;
    if (element) element.blur();
  }

  // Inject DestroyRef เพื่อใช้ติดตามอายุขัยของ Component
  private destroyRef = inject(DestroyRef);

  protected email = model('');
  protected password = model('');

  // เพิ่ม Signal สำหรับจัดการสถานะ Loading (นำไป bind ใส่ [disabled]="isLoading()" ที่ปุ่ม HTML)
  protected isLoading = signal(false);

  protected readonly currentUser = this.accountService.currentUser;

  login() {
    // ป้องกันการยิง API ซ้ำหากผู้ใช้กดรัวๆ
    if (this.isLoading()) return;

    this.isLoading.set(true);
    const creds = { email: this.email(), password: this.password() };

    this.accountService
      .login(creds)
      .pipe(
        // ใช้ takeUntilDestroyed กรณี Component ถูกทำลาย Angular จะทำการ Unsubscribe และ Cancel HTTP Request ไปยัง Backend ให้อัตโนมัติ
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
