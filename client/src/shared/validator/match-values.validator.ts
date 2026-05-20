import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function matchValues(matchTo: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const matchValue = control.parent?.get(matchTo)?.value;
    return control.value === matchValue ? null : { passwordMismatch: true };
  };
}
