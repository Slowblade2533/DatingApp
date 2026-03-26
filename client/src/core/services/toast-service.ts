import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  message: string;
  type: 'alert-success' | 'alert-error' | 'alert-warning' | 'alert-info';
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  // ใช้ Signal เพื่อให้ UI อัปเดตทันทีแบบ Zoneless
  toasts = signal<ToastMessage[]>([]);
  private nextId = 1;

  private show(message: string, type: ToastMessage['type'], duration = 5000) {
    const id = this.nextId++;

    // ใช้ update เพื่อแทรกข้อมูลเข้าไปใน Signal Array
    this.toasts.update((current) => [...current, { id, message, type }]);

    // ตั้งเวลาให้ Toast ลบตัวเองออก
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
