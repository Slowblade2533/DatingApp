import { Component, inject, input } from '@angular/core';
import { ControlValueAccessor, FormControl, NgControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-text-input',
  imports: [ReactiveFormsModule],
  templateUrl: './text-input.html',
  styleUrl: './text-input.css',
})
export class TextInput implements ControlValueAccessor {
  label = input<string>('');
  type = input<string>('text');
  maxDate = input<string>('');

  public ngControl = inject(NgControl, { self: true });

  constructor() {
    this.ngControl.valueAccessor = this;
  }

  get control(): FormControl {
    return this.ngControl.control as FormControl;
  }

  preventManualInput(event: Event) {
    if (this.type() === 'date') {
      if (event instanceof KeyboardEvent && event.key === 'Tab') {
        return;
      }
      event.preventDefault();
    }
  }

  writeValue(obj: any): void {}
  registerOnChange(fn: any): void {}
  registerOnTouched(fn: any): void {}
}
