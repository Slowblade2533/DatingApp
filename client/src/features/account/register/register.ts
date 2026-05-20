import { Component, DestroyRef, OnInit, inject, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { AccountService } from '../../../core/services/account.service';
import { Router } from '@angular/router';
import { TextInput } from '../../../shared/text-input/text-input';
import { matchValues } from '../../../shared/validator/match-values.validator';
import { minAgeValidator } from '../../../shared/validator/min-age.validator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  imports: [ReactiveFormsModule, TextInput],
  selector: 'app-register',
  styleUrl: './register.css',
  templateUrl: './register.html',
})
export class Register implements OnInit {
  private accountService = inject(AccountService);
  private destroyRef = inject(DestroyRef);
  private router = inject(Router);

  cancelRegister = output<boolean>();
  protected isLoading = signal(false);
  protected currentStep = signal(1);
  protected validationErrors = signal<string[]>([]);

  credentialsForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.email,
        Validators.minLength(6),
        Validators.maxLength(256),
      ],
    }),
    displayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(256)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), Validators.maxLength(32)],
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(32),
        matchValues('password'),
      ],
    }),
  });

  profileForm = new FormGroup({
    gender: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    dateOfBirth: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, minAgeValidator(18)],
    }),
    city: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    country: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  cancel() {
    this.credentialsForm.reset();
    this.cancelRegister.emit(false);
    this.isLoading.set(false);
  }

  getMaxDate() {
    const today = new Date();
    today.setFullYear(today.getFullYear() - 18);
    return today.toISOString().split('T')[0];
  }

  nextStep() {
    if (this.credentialsForm.valid) {
      this.currentStep.update((prevStep) => prevStep + 1);
    }
  }

  ngOnInit() {
    this.credentialsForm
      .get('password')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.credentialsForm.get('confirmPassword')?.updateValueAndValidity();
      });
  }

  prevStep() {
    this.currentStep.update((prevStep) => prevStep - 1);
  }

  register() {
    this.isLoading.set(true);

    if (this.profileForm.valid && this.credentialsForm.valid) {
      const formData = { ...this.credentialsForm.getRawValue(), ...this.profileForm.getRawValue() };

      this.accountService.register(formData).subscribe({
        next: () => {
          this.router.navigateByUrl('/members');
        },
        error: (error) => {
          console.log(error);
          this.validationErrors.set(error);
        },
      });
    }

    setTimeout(() => this.isLoading.set(false), 1000);
  }
}
