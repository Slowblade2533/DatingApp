import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { NetworkStatusService } from '../core/services/network-status.service';
import { Nav } from '../layout/nav/nav';
import { OfflineBanner } from '../shared/errors/offline-banner/offline-banner';
import { ToastContainer } from '../shared/toast-container/toast-container';

@Component({
  selector: 'app-root',
  imports: [Nav, RouterOutlet, ToastContainer, OfflineBanner],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected router = inject(Router);
  protected networkService = inject(NetworkStatusService);

  protected isSubPage = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event: NavigationEnd) => event.urlAfterRedirects !== '/'),
    ),
    { initialValue: this.router.url !== '/' },
  );
}
