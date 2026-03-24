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
  private cdr = inject(ChangeDetectorRef);

  showRegister(value: boolean) {
    if (!document.startViewTransition) {
      this.registerMode.set(value);
      return;
    }

    document.startViewTransition(() => {
      this.registerMode.set(value);
      this.cdr.detectChanges();
    });
  }
}
