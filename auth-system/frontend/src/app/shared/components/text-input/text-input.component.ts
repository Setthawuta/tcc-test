import { Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let uid = 0;

@Component({
  selector: 'app-text-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TextInputComponent),
      multi: true,
    },
  ],
  template: `
    <input
      [id]="inputId()"
      [type]="type()"
      [value]="value()"
      [disabled]="disabled()"
      [attr.autocomplete]="autocomplete()"
      [attr.placeholder]="placeholder()"
      [attr.aria-invalid]="invalid() || null"
      [attr.aria-describedby]="describedBy() || null"
      (input)="onInput($event)"
      (blur)="onTouched()"
      class="w-full rounded border border-gray-300 px-3 py-2 focus:border-green-600 focus:outline-none focus:ring-1 focus:ring-green-600 disabled:bg-gray-100"
      [class.border-red-400]="invalid()"
      [class.focus:border-red-500]="invalid()"
      [class.focus:ring-red-500]="invalid()"
    />
  `,
})
export class TextInputComponent implements ControlValueAccessor {
  readonly type = input<string>('text');
  readonly autocomplete = input<string>('off');
  readonly placeholder = input<string>('');
  readonly invalid = input<boolean>(false);
  readonly describedBy = input<string>('');
  readonly inputId = input<string>(`text-input-${++uid}`);

  readonly value = signal('');
  readonly disabled = signal(false);

  private onChangeFn: (v: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(v: string | null): void {
    this.value.set(v ?? '');
  }
  registerOnChange(fn: (v: string) => void): void {
    this.onChangeFn = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(d: boolean): void {
    this.disabled.set(d);
  }

  onInput(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    this.value.set(v);
    this.onChangeFn(v);
  }
}
