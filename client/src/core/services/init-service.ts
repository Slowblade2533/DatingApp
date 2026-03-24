import { inject, Injectable } from '@angular/core';
import { AccountService } from './account-service';

@Injectable({
  providedIn: 'root',
})
export class InitService {
  private accountService = inject(AccountService);

  init(): void {
    try {
      const raw = localStorage.getItem('User');
      if (!raw) return;

      const user = JSON.parse(raw);
      if (!user || typeof user.token !== 'string') {
        localStorage.removeItem('User');
        return;
      }

      // อย่า trust role/claims จาก localStorage ตรง ๆ
      this.accountService.currentUser.set(user);
    } catch {
      localStorage.removeItem('User');
    }
  }
}
