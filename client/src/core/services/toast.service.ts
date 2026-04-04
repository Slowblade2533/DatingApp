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
  private readonly MAX_TOASTS = 5; //จำกัดจำนวน toast สูงสุด: 5

  private show(message: string, type: ToastMessage['type'], duration = 5000): number {
    const id = this.nextId++;

    // ใช้ update เพื่อแทรกข้อมูลเข้าไปใน Signal Array (จำกัดจำนวนสูงสุด)
    this.toasts.update((current) => {
      const updated = [...current, { id, message, type }];
      return updated.length > this.MAX_TOASTS
        ? updated.slice(updated.length - this.MAX_TOASTS)
        : updated;
    });

    // ✅ duration <= 0 = persistent (ไม่หายเอง ต้อง dismiss เอง)
    if (duration > 0) {
      setTimeout(() => this.remove(id), duration);
    }
    return id;
  }

  remove(id: number): void {
    this.toasts.update((current) => current.filter((t) => t.id !== id));
  }

  success(message: string, duration?: number): number {
    return this.show(message, 'alert-success', duration);
  }
  error(message: string, duration?: number): number {
    return this.show(message, 'alert-error', duration);
  }
  warning(message: string, duration?: number): number {
    return this.show(message, 'alert-warning', duration);
  }
  info(message: string, duration?: number): number {
    return this.show(message, 'alert-info', duration);
  }
}
