import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, OnDestroy, signal } from '@angular/core';
import { LoginCreds, RegisterCreds, User } from '../../types/user';
import { catchError, Subscription, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { Router } from '@angular/router';
import { SharedWorkerService } from './shared-worker.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private sharedWorker = inject(SharedWorkerService);
  private baseUrl = environment.apiUrl;

  // 1. รวม Logic การโหลดจาก localStorage ไว้ที่นี่ที่เดียว
  currentUser = signal<User | null>(this.initializeUser());

  constructor() {
    // ✅ ฟัง Message จาก SharedWorker → อัปเดต Signal → Angular re-render อัตโนมัติ
    this.sharedWorker.messages$.pipe(takeUntilDestroyed()).subscribe((msg) => {
      switch (msg.type) {
        case 'STATE_UPDATE':
          if (msg.payload.user) {
            this.currentUser.set(msg.payload.user as User);
          }
          break;
        case 'FORCE_LOGOUT':
          this.handleLocalLogout(); // แยกฟังก์ชันเพื่อความเป็นระเบียบ
          break;
        case 'TOKEN_REFRESHED':
          this.updateUserToken(msg.token);
          break;
      }
    });
  }

  private initializeUser(): User | null {
    const raw = localStorage.getItem('user');
    if (!raw) return null;
    try {
      const user = JSON.parse(raw);

      if (user && typeof user.token === 'string') {
        // ✅ ตรวจสอบวันหมดอายุของ Token เบื้องต้น
        if (this.isTokenExpired(user.token)) {
          this.handleLocalLogout(); // ล้างทิ้งทันทีถ้าหมดอายุแล้ว
          return null;
        }
        return user;
      }
      return null;
    } catch {
      return null;
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      // ✅ JWT ใช้ Base64URL ไม่ใช่ Base64 ปกติ — ต้องแปลง - → + และ _ → / ก่อน decode
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(atob(base64));
      if (!payload.exp) return false;
      const expiryTime = payload.exp * 1000; // แปลงเป็นมิลลิวินาที
      return Date.now() >= expiryTime; // ถ้าเวลาปัจจุบันมากกว่าเวลาหมดอายุ = true
    } catch {
      return true; // ถ้า decode ไม่ได้ถือว่าเสีย/หมดอายุ
    }
  }

  private updateUserToken(token: string) {
    const current = this.currentUser();
    if (!current) return;
    const updated = { ...current, token };
    this.currentUser.set(updated);
    localStorage.setItem('user', JSON.stringify(updated));
  }

  setCurrentUser(user: User) {
    // หมายเหตุ: หากมีการใช้ JWT Token ควรให้ Backend ส่งเป็น HttpOnly Cookie แทนการเก็บตรงๆ
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
    // ✅ แจ้ง Worker ว่ามี User ใหม่ เพื่อ broadcast ไปยัง Tab อื่นๆ
    this.sharedWorker.send({ type: 'SET_USER', user });
  }

  logout() {
    this.handleLocalLogout();
    this.sharedWorker.send({ type: 'LOGOUT' });
  }

  private handleLocalLogout() {
    localStorage.removeItem('user');
    this.currentUser.set(null);
    this.router.navigate(['/']);
  }

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

  requestTokenRefresh() {
    // ✅ Worker มี Lock ป้องกัน — ทุก Tab เรียกได้ Worker จะ Refresh แค่ครั้งเดียว
    this.sharedWorker.send({ type: 'REQUEST_TOKEN_REFRESH' });
  }

  private handleError(error: HttpErrorResponse) {
    return throwError(() => error);
  }
}
