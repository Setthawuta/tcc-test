import { Component, input } from '@angular/core';

@Component({
  selector: 'app-submit-button',
  standalone: true,
  host: { class: 'block' },
  template: `
    <button
      type="submit"
      [disabled]="disabled() || loading()"
      [attr.aria-busy]="loading()"
      class="w-full bg-green-600 hover:bg-green-700 text-white font-medium py-2 rounded transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
    >
      @if (loading()) {
        <span
          class="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"
          aria-hidden="true"
        ></span>
      }
      <ng-content />
    </button>
  `,
})
export class SubmitButtonComponent {
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);
}
