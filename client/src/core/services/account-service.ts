import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { LoginCreds, RegisterCreds, User } from '../../types/user';
import { catchError, tap, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);

  // โหลด State ทันทีตอนเริ่มต้น ป้องกันปัญหาค่าว่างเมื่อรีเฟรชหน้าเว็บ
  currentUser = signal<User | null>(this.getUserFromStorage());

  baseUrl = 'https://localhost:7091/api/';

  private authRequest(endpoint: string, creds: RegisterCreds | LoginCreds) {
    return this.http.post<User>(this.baseUrl + endpoint, creds).pipe(
      tap((user) => {
        if (user) {
          this.setCurrentUser(user);
        }
      }),
      catchError(this.handleError),
    );
  }

  register(creds: RegisterCreds) {
    return this.authRequest('account/register', creds);
  }

  login(creds: LoginCreds) {
    return this.authRequest('account/login', creds);
  }

  setCurrentUser(user: User) {
    // หมายเหตุ: หากมีการใช้ JWT Token ควรให้ Backend ส่งเป็น HttpOnly Cookie แทนการเก็บตรงๆ
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUser.set(null);
  }

  private getUserFromStorage(): User | null {
    const userJson = localStorage.getItem('user');
    if (!userJson) return null;
    try {
      return JSON.parse(userJson);
    } catch {
      return null;
    }
  }

  private handleError(error: HttpErrorResponse) {
    return throwError(() => error);
  }
}
