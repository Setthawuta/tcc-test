import { Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let uid = 0;

@Component({
  selector: 'password-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PasswordInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="relative">
      <input
        [id]="inputId()"
        [type]="show() ? 'text' : 'password'"
        [value]="value()"
        [disabled]="disabled()"
        [attr.autocomplete]="autocomplete()"
        [attr.placeholder]="placeholder()"
        [attr.aria-invalid]="invalid() || null"
        [attr.aria-describedby]="describedBy() || null"
        (input)="onInput($event)"
        (blur)="onTouched()"
        class="w-full rounded border border-gray-300 px-3 py-2 pr-10 focus:border-green-600 focus:outline-none focus:ring-1 focus:ring-green-600 disabled:bg-gray-100"
      />
      <button
        type="button"
        (click)="toggle()"
        [attr.aria-label]="show() ? 'ซ่อนรหัสผ่าน' : 'แสดงรหัสผ่าน'"
        [attr.aria-pressed]="show()"
        class="absolute inset-y-0 right-0 flex items-center px-3 text-gray-500 hover:text-gray-700"
      >
        @if (!show()) {
          <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
            <circle cx="12" cy="12" r="3" />
          </svg>
        } @else {
          <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M9.88 9.88A3 3 0 0 0 12 15a3 3 0 0 0 2.12-.88" />
            <path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c6.5 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68" />
            <path d="M6.61 6.61A13.52 13.52 0 0 0 2 12s3.5 7 10 7a9.74 9.74 0 0 0 5.39-1.61" />
            <line x1="2" y1="2" x2="22" y2="22" />
          </svg>
        }
      </button>
    </div>
  `,
})
export class PasswordInputComponent implements ControlValueAccessor {
  readonly autocomplete = input<string>('current-password');
  readonly placeholder = input<string>('');
  readonly invalid = input<boolean>(false);
  readonly describedBy = input<string>('');
  readonly inputId = input<string>(`password-input-${++uid}`);

  readonly value = signal('');
  readonly show = signal(false);
  readonly disabled = signal(false);

  private onChangeFn: (v: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }
  registerOnChange(fn: (v: string) => void): void {
    this.onChangeFn = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  onInput(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    this.value.set(v);
    this.onChangeFn(v);
  }

  toggle(): void {
    this.show.set(!this.show());
  }
}
