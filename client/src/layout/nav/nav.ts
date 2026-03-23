import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../../core/services/account-service';

@Component({
  selector: 'app-nav',
  imports: [FormsModule],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  protected accountService = inject(AccountService);

  protected email = signal('');
  protected password = signal('');

  login() {
    const creds = { email: this.email(), password: this.password() };

    this.accountService.login(creds).subscribe({
      next: (result) => {
        console.log(result);
        this.email.set('');
        this.password.set('');
      },
      error: (error) => {
        alert(error.message);
      },
    });
  }

  logout() {
    this.accountService.logout();
  }
}
