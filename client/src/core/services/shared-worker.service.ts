import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

// ✅ แยก Export Types เพื่อให้ Service อื่นๆ (เช่น NetworkStatusService) เรียกใช้ได้
export type WorkerMessage =
  | { type: 'STATE_UPDATE'; payload: { user: any; lastUpdatedAt: number } }
  | { type: 'FORCE_LOGOUT' }
  | { type: 'TOKEN_REFRESHED'; token: string }
  | { type: 'HEALTH_UPDATE'; reachable: boolean }; // ✅ รองรับการเช็คสุขภาพ Server

export type TabMessage =
  | { type: 'INIT_CONFIG'; apiUrl: string } // ✅ ส่ง Config เริ่มต้นให้ Worker
  | { type: 'LOGOUT' }
  | { type: 'REQUEST_TOKEN_REFRESH' }
  | { type: 'SET_USER'; user: any }
  | { type: 'CHECK_INITIAL_HEALTH' }; // ✅ ใช้ขอสถานะสุขภาพครั้งแรก

@Injectable({ providedIn: 'root' })
export class SharedWorkerService implements OnDestroy {
  // ✅ โหลด Worker ตาม Path
  private worker =
    typeof SharedWorker !== 'undefined'
      ? new SharedWorker(new URL('../workers/shared.worker.ts', import.meta.url), {
          type: 'module',
        })
      : null;

  private messageSubject = new Subject<WorkerMessage>();
  readonly messages$ = this.messageSubject.asObservable();

  constructor() {
    if (!this.worker) return;

    // 1. ผูก listener ก่อน
    this.worker.port.addEventListener('message', (e: MessageEvent<WorkerMessage>) => {
      this.messageSubject.next(e.data);
    });

    // 2. เรียก start()
    this.worker.port.start();

    // 3. ✅ ส่ง Config ทันทีเพื่อให้ Worker รู้ว่าจะต้อง Ping ไปที่ URL ไหน
    // วิธีนี้ช่วยให้ Worker ไม่ต้อง Hardcode URL และเปลี่ยนตาม Environment ได้ (Dev/Prod)
    this.send({ type: 'INIT_CONFIG', apiUrl: environment.apiUrl });
  }

  // ส่ง Command ไปหา Worker
  send(message: TabMessage): void {
    this.worker?.port.postMessage(message);
  }

  ngOnDestroy(): void {
    this.worker?.port.close();
    this.messageSubject.complete();
  }
}
