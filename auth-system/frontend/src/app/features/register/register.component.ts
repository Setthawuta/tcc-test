import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { APP_PATHS } from '../../core/constants/routes';
import { AuthService } from '../../core/services/auth.service';
import { isNetworkError, toApiError } from '../../core/utils/error-mapper';
import { AlertComponent } from '../../shared/components/alert/alert.component';
import { PasswordInputComponent } from '../../shared/components/password-input/password-input.component';
import { SubmitButtonComponent } from '../../shared/components/submit-button/submit-button.component';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const p = group.get('password')?.value;
  const c = group.get('confirmPassword')?.value;
  return p === c ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    PasswordInputComponent,
    SubmitButtonComponent,
  ],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group(
    {
      username: [
        '',
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(50),
          Validators.pattern('^[a-zA-Z0-9]+$'),
        ],
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$'),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate([APP_PATHS.login], {
          queryParams: { registered: 'true' },
        });
      },
      error: (err) => {
        this.submitting.set(false);
        const apiErr = toApiError(err);
        if (apiErr.status === 409) {
          this.errorMessage.set('ชื่อผู้ใช้นี้มีอยู่แล้ว');
        } else if (apiErr.status === 400) {
          this.errorMessage.set(apiErr.detail ?? 'ข้อมูลไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง');
        } else if (isNetworkError(apiErr)) {
          this.errorMessage.set('เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาตรวจสอบอินเทอร์เน็ต');
        } else {
          this.errorMessage.set('เกิดข้อผิดพลาด กรุณาลองใหม่อีกครั้ง');
        }
      },
    });
  }
}
