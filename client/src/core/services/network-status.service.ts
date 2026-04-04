import { computed, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { ToastService } from './toast.service';
import { SharedWorkerService } from './shared-worker.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export type NetworkState = 'online' | 'offline' | 'degraded';

@Injectable({ providedIn: 'root' })
export class NetworkStatusService implements OnDestroy {
  private readonly toast = inject(ToastService);
  private readonly sharedWorker = inject(SharedWorkerService);

  // ─── Private Signals (ลดเหลือเท่าที่จำเป็น) ──────────────────────────
  private readonly _browserOnline = signal(navigator.onLine);
  private readonly _backendReachable = signal(true);
  private readonly _offlineNotified = signal(false);
  private readonly _recoveredAt = signal<Date | null>(null);

  // ─── Public Computed Signals ───────────────────────────────
  readonly state = computed<NetworkState>(() => {
    if (!this._browserOnline()) return 'offline'; // network ขาด
    if (!this._backendReachable()) return 'degraded'; // backend ล่ม
    return 'online';
  });

  readonly isOffline = computed(() => this.state() !== 'online');
  readonly recoveredAt = computed(() => this._recoveredAt());

  // ─── Internal State ────────────────────────────────────────
  private _offlineToastId: number | null = null;

  constructor() {
    // 1. ฟังเหตุการณ์จาก Browser (แจ้งเตือนทันทีเมื่อเน็ตหลุด)
    window.addEventListener('online', this._onOnline);
    window.addEventListener('offline', this._onOffline);

    // 2. ฟังเหตุการณ์จาก SharedWorker (ศูนย์กลางการเช็ค Health)
    this.sharedWorker.messages$
      .pipe(takeUntilDestroyed()) // ป้องกัน Memory Leak อัตโนมัติ
      .subscribe((msg) => {
        if (msg.type === 'HEALTH_UPDATE') {
          this._handleStatusChange(msg.reachable);
        }
      });

    // 3. ขอสถานะปัจจุบันทันทีเมื่อเปิด Tab ใหม่
    this.sharedWorker.send({ type: 'CHECK_INITIAL_HEALTH' });
  }

  // ─── Event Handlers ────────────────────────────────────────
  private readonly _onOnline = () => {
    this._browserOnline.set(true);
    // ไม่ต้อง Ping เองแล้ว ให้ Worker จัดการรอบถัดไป หรือส่ง message ไปกระตุ้นได้
    this.sharedWorker.send({ type: 'CHECK_INITIAL_HEALTH' });
  };

  private readonly _onOffline = () => {
    this._browserOnline.set(false);
    // แจ้ง Error สถานะ 0 ทันทีไม่ต้องรอ
    this.recordHttpError(0);
  };

  /** รับสถานะจาก SharedWorker มาอัปเดต UI */
  private _handleStatusChange(reachable: boolean) {
    if (reachable) {
      this.recordHttpSuccess();
    } else {
      // จำลองสถานะ 503 (Service Unavailable) เมื่อ Worker บอกว่าติดต่อไม่ได้
      this._backendReachable.set(false);
      this._notifyOffline('ไม่สามารถเชื่อมต่อ Server ได้ กำลังลองใหม่...');
    }
  }

  // ─── Methods สำหรับ Interceptor & Worker ─────────────────────

  recordHttpError(statusCode: number): void {
    // ✅ ตรวจสอบว่า Interceptor ส่ง Status 0 มาจริงเมื่อ Server ปิด
    if (statusCode === 0 || statusCode >= 500) {
      this._backendReachable.set(false); // เปลี่ยน Signal ฝั่ง UI ทันทีไม่ต้องรอ Worker

      // กระตุ้นให้ Worker เช็คสถานะใหม่ทันที
      this.sharedWorker.send({ type: 'CHECK_INITIAL_HEALTH' });

      const msg = !this._browserOnline()
        ? 'ขาดการเชื่อมต่ออินเทอร์เน็ต'
        : 'ไม่สามารถเชื่อมต่อ Server ได้';
      this._notifyOffline(msg);
    }
  }

  recordHttpSuccess(): void {
    const wasOffline = !this._backendReachable();
    this._backendReachable.set(true);

    if (wasOffline) {
      this._clearOfflineWarning();
      this._recoveredAt.set(new Date());
      this.toast.success('เชื่อมต่อ Server สำเร็จแล้ว ✅', 5000);
    }
  }

  // ─── Helpers ───────────────────────────────────────────────

  private _notifyOffline(message: string) {
    if (this._offlineNotified()) return;
    this._offlineNotified.set(true);
    // duration -1 คือค้างไว้จนกว่าจะสั่งลบ
    this._offlineToastId = this.toast.warning(message, -1);
  }

  private _clearOfflineWarning() {
    if (this._offlineToastId !== null) {
      this.toast.remove(this._offlineToastId);
      this._offlineToastId = null;
    }
    this._offlineNotified.set(false);
  }

  ngOnDestroy(): void {
    window.removeEventListener('online', this._onOnline);
    window.removeEventListener('offline', this._onOffline);
  }
}
