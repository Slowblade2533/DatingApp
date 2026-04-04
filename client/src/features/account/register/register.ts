import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RegisterCreds } from '../../../types/user';
import { AccountService } from '../../../core/services/account.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-register',
  imports: [FormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private accountService = inject(AccountService);
  private destroyRef = inject(DestroyRef);
  cancelRegister = output<boolean>();
  protected creds = {} as RegisterCreds;
  protected isLoading = signal(false);

  register() {
    if (this.isLoading()) return;
    this.isLoading.set(true);

    this.accountService
      .register(this.creds)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: () => {
          this.cancel();
        },
        error: () => {},
      });
  }

  cancel() {
    this.cancelRegister.emit(false);
  }
}
