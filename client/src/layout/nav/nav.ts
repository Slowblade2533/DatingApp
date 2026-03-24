import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../../core/services/account-service';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastService } from '../../core/services/toast-service';

@Component({
  selector: 'app-nav',
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  protected accountService = inject(AccountService);
  private router = inject(Router);
  private toast = inject(ToastService);

  // Inject DestroyRef เพื่อใช้ติดตามอายุขัยของ Component
  private destroyRef = inject(DestroyRef);

  protected email = signal('');
  protected password = signal('');

  // เพิ่ม Signal สำหรับจัดการสถานะ Loading (นำไป bind ใส่ [disabled]="isLoading()" ที่ปุ่ม HTML)
  protected isLoading = signal(false);

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
          this.toast.error(error.message);
        },
      });
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}
