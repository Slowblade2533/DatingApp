import { Component, inject } from '@angular/core';
import { Nav } from '../layout/nav/nav';
import { Router, RouterOutlet } from '@angular/router';
import { ToastContainer } from '../shared/toast-container/toast-container';
import { OfflineBanner } from '../shared/errors/offline-banner/offline-banner';
import { NetworkStatusService } from '../core/services/network-status.service';

@Component({
  selector: 'app-root',
  imports: [Nav, RouterOutlet, ToastContainer, OfflineBanner],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected router = inject(Router);
  protected networkService = inject(NetworkStatusService);
}
