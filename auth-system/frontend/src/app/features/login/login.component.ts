import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { APP_PATHS } from '../../core/constants/routes';
import { AuthService } from '../../core/services/auth.service';
import { isNetworkError, toApiError } from '../../core/utils/error-mapper';
import { AlertComponent } from '../../shared/components/alert/alert.component';
import { PasswordInputComponent } from '../../shared/components/password-input/password-input.component';
import { SubmitButtonComponent } from '../../shared/components/submit-button/submit-button.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    PasswordInputComponent,
    SubmitButtonComponent,
  ],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly infoMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  @Input() registered?: string;

  ngOnInit(): void {
    if (this.registered === 'true') {
      this.infoMessage.set('สมัครสมาชิกสำเร็จ กรุณาลงชื่อเข้าใช้งาน');
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) return;

    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate([APP_PATHS.welcome]);
      },
      error: (err) => {
        this.submitting.set(false);
        const apiErr = toApiError(err);
        if (apiErr.status === 401) {
          this.errorMessage.set('ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง');
        } else if (isNetworkError(apiErr)) {
          this.errorMessage.set('เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาตรวจสอบอินเทอร์เน็ต');
        } else {
          this.errorMessage.set('เกิดข้อผิดพลาด กรุณาลองใหม่อีกครั้ง');
        }
      },
    });
  }
}
