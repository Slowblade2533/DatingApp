import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { LoginCreds, RegisterCreds, User } from '../../types/user';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  currentUser = signal<User | null>(null);

  baseUrl = 'https://localhost:7091/api/';

  private authRequest(endpoint: string, creds: RegisterCreds | LoginCreds) {
    return this.http.post<User>(this.baseUrl + endpoint, creds).pipe(
      tap((user) => {
        if (user) {
          this.setCurrentUser(user);
        }
      }),
    );
  }

  register(creds: RegisterCreds) {
    return this.authRequest('account/register', creds);
  }

  login(creds: LoginCreds) {
    return this.authRequest('account/login', creds);
  }

  setCurrentUser(user: User) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUser.set(null);
  }

  /*
  private loadUserFromStorage(): User | null {
    const userString = localStorage.getItem('user');
    if (!userString) return null;
    try {
      return JSON.parse(userString) as User;
    } catch {
      return null;
    }
  }
  */
}
