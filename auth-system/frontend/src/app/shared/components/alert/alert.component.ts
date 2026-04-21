import { Component, computed, input } from '@angular/core';

type AlertVariant = 'success' | 'error' | 'info' | 'warning';

@Component({
  selector: 'app-alert',
  standalone: true,
  template: `
    <div
      [class]="classes()"
      role="alert"
      aria-live="polite"
    >
      <ng-content />
    </div>
  `,
})
export class AlertComponent {
  readonly variant = input<AlertVariant>('info');

  readonly classes = computed(() => {
    const base = 'rounded border px-3 py-2 text-sm';
    const byVariant: Record<AlertVariant, string> = {
      success: 'border-green-300 bg-green-50 text-green-800',
      error: 'border-red-300 bg-red-50 text-red-800',
      info: 'border-blue-300 bg-blue-50 text-blue-800',
      warning: 'border-amber-300 bg-amber-50 text-amber-800',
    };
    return `${base} ${byVariant[this.variant()]}`;
  });
}
