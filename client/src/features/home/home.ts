import { ChangeDetectorRef, Component, inject, signal } from '@angular/core';
import { Register } from '../account/register/register';

@Component({
  selector: 'app-home',
  imports: [Register],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  protected registerMode = signal(false);

  showRegister(value: boolean) {
    if (!document.startViewTransition) {
      this.registerMode.set(value);
      return;
    }

    document.startViewTransition(() => {
      this.registerMode.set(value);
      // ✅ ใน Zoneless, Signal.set() trigger change detection อัตโนมัติ
      // ถ้า view transition ต้องการ synchronous render:
      // ใช้ ApplicationRef.tick() แทนเพื่อ trigger global CD
    });
  }
}
