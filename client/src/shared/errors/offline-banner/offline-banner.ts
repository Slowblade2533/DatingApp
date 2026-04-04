import { Component, inject } from '@angular/core';
import { NetworkStatusService } from '../../../core/services/network-status.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-offline-banner',
  imports: [DatePipe],
  template: `
    @if (network.isOffline()) {
      <div
        class="offline-banner"
        [class.degraded]="network.state() === 'degraded'"
        role="alert"
        aria-live="assertive"
      >
        @switch (network.state()) {
          @case ('offline') {
            <span>🔴 ขาดการเชื่อมต่ออินเทอร์เน็ต — กรุณาตรวจสอบ Network</span>
          }
          @case ('degraded') {
            <span> 🟡 เซิร์ฟเวอร์ไม่ตอบสนอง — กำลังพยายามเชื่อมต่อใหม่โดยอัตโนมัติ... </span>
          }
        }
      </div>
    }

    @if (network.recoveredAt()) {
      <div class="recovery-banner" role="status">
        ✅ เชื่อมต่อสำเร็จแล้ว เมื่อ
        {{ network.recoveredAt() | date: 'HH:mm:ss' }}
      </div>
    }
  `,
  styles: [
    `
      .offline-banner {
        background: #a13544;
        color: white;
        padding: 10px 16px;
        text-align: center;
        font-size: 14px;
        position: sticky;
        top: 0;
        z-index: 9999;
        transition: background 0.3s ease;
      }
      .offline-banner.degraded {
        background: #da7101;
      }
      .recovery-banner {
        background: #437a22;
        color: white;
        padding: 8px 16px;
        text-align: center;
        font-size: 13px;
        /* ใช้ animation เพื่อให้หายไปเองตาม Logic เดิมของคุณ */
        animation: fadeOut 3s forwards 3s;
      }
      @keyframes fadeOut {
        to {
          opacity: 0;
          height: 0;
          padding: 0;
        }
      }
    `,
  ],
})
export class OfflineBanner {
  // ฉีด Service เพื่อใช้งาน Computed Signals
  readonly network = inject(NetworkStatusService);
}
