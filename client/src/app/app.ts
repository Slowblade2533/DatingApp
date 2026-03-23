import { Component, inject, OnInit, signal } from '@angular/core';
import { Nav } from '../layout/nav/nav';
import { Home } from '../features/home/home';
import { HttpClient } from '@angular/common/http';
import { AccountService } from '../core/services/account-service';
import { User } from '../types/user';
import { lastValueFrom } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [Nav, Home],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  protected readonly title = signal('Dating app');
  private http = inject(HttpClient);
  private accountService = inject(AccountService);
  protected members = signal<User[]>([]);
  httpLink = 'https://localhost:7091/api/members';

  async ngOnInit() {
    await this.getMembers();
    this.setCurrentUser();
  }

  setCurrentUser() {
    const userString = localStorage.getItem('User');
    if (!userString) return;
    const user = JSON.parse(userString);
    this.accountService.currentUser.set(user);
  }

  async getMembers() {
    try {
      const result = await lastValueFrom(this.http.get<User[]>(this.httpLink));
      this.members.set(result);
      console.log('Members loaded:', result);
    } catch (error) {
      console.log(error);
    }
  }
}
