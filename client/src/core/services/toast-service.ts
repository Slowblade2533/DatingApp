import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'alert-success' | 'alert-error' | 'alert-warning' | 'alert-info';
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  toasts = signal<Toast[]>([]);
  private idCounter = 0;

  private show(message: string, type: Toast['type'], duration = 5000) {
    const id = this.idCounter++;

    // อัปเดต Signal เพิ่ม Toast ตัวใหม่เข้าไป
    this.toasts.update((current) => [...current, { id, message, type }]);

    // ตั้งเวลาลบ
    setTimeout(() => this.remove(id), duration);
  }

  remove(id: number) {
    this.toasts.update((current) => current.filter((t) => t.id !== id));
  }

  success(message: string, duration?: number) {
    this.show(message, 'alert-success', duration);
  }
  error(message: string, duration?: number) {
    this.show(message, 'alert-error', duration);
  }
  warning(message: string, duration?: number) {
    this.show(message, 'alert-warning', duration);
  }
  info(message: string, duration?: number) {
    this.show(message, 'alert-info', duration);
  }
}
